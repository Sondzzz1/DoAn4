import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { postService } from '../../services/postService';
import { favoriteService } from '../../services/favoriteService';
import { Post } from '../../types/post.types';
import { ROUTES, ROLES } from '../../utils/constants';
import { toast } from 'react-toastify';
import { FiChevronRight, FiAlertCircle, FiRefreshCw, FiArrowLeft } from 'react-icons/fi';

// Modular Room Components
import RoomImageGallery from '../../components/room/RoomImageGallery';
import RoomInformation from '../../components/room/RoomInformation';
import RoomAmenities from '../../components/room/RoomAmenities';
import RoomLocation from '../../components/room/RoomLocation';
import LandlordContactCard from '../../components/room/LandlordContactCard';
import BookingModal from '../../components/room/BookingModal';
import RentalRequestModal from '../../components/room/RentalRequestModal';
import RoomReviews from '../../components/room/RoomReviews';
import SimilarRooms from '../../components/room/SimilarRooms';
import { openDirectChat } from '../../components/chat/chatEvents';

// Import CSS
import './RoomDetailPage.css';

const RoomDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();

  const [post, setPost] = useState<Post | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [isFavorite, setIsFavorite] = useState<boolean>(false);
  const [isBookingModalOpen, setIsBookingModalOpen] = useState<boolean>(false);
  const [isRentalModalOpen, setIsRentalModalOpen] = useState<boolean>(false);

  useEffect(() => {
    if (id) {
      const postId = parseInt(id, 10);
      void fetchPostDetail(postId);
      if (isAuthenticated && user?.role === ROLES.TENANT) {
        void checkFavoriteStatus(postId);
      }
    }
  }, [id, isAuthenticated, user]);

  async function checkFavoriteStatus(postId: number) {
    try {
      const res = await favoriteService.checkFavorite(postId);
      setIsFavorite(!!res.data);
    } catch {
      // Ignore if offline
    }
  }

  async function fetchPostDetail(postId: number) {
    if (isNaN(postId)) {
      setError('ID phòng không hợp lệ');
      setLoading(false);
      return;
    }

    setLoading(true);
    setError(null);

    try {
      const response = await postService.getPostById(postId);
      if (response && response.data) {
        setPost(response.data);
      } else {
        setPost(null);
        setError('Không tìm thấy phòng trọ.');
      }
    } catch {
      setPost(null);
      setError('Không thể tải thông tin phòng.');
    } finally {
      setLoading(false);
    }
  }

  // Toggle favorite handler with Real API
  const handleToggleFavorite = async () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để lưu tin phòng trọ.');
      navigate(ROUTES.LOGIN);
      return;
    }

    if (user?.role !== ROLES.TENANT) {
      toast.info('Chức năng lưu tin dành cho tài khoản người thuê phòng.');
      return;
    }

    if (!post?.id) return;

    try {
      if (isFavorite) {
        await favoriteService.removeFavorite(post.id);
        setIsFavorite(false);
        toast.info('Đã bỏ lưu tin phòng trọ.');
      } else {
        await favoriteService.addFavorite(post.id);
        setIsFavorite(true);
        toast.success('Đã lưu tin vào danh sách yêu thích!');
      }
    } catch {
      toast.error('Thao tác yêu thích thất bại.');
    }
  };

  // Share handler
  const handleShare = async () => {
    const currentUrl = window.location.href;
    const shareData = {
      title: post?.title || 'Phòng trọ cho thuê - Timnhatro.vn',
      text: 'Xem thông tin phòng trọ: ' + (post?.title || ''),
      url: currentUrl,
    };

    if (navigator.share && navigator.canShare && navigator.canShare(shareData)) {
      try {
        await navigator.share(shareData);
        return;
      } catch {
        // Fallback to clipboard
      }
    }

    try {
      await navigator.clipboard.writeText(currentUrl);
      toast.success('Đã sao chép liên kết phòng.');
    } catch {
      toast.error('Không thể sao chép liên kết.');
    }
  };

  // Booking viewing handler
  const handleBookViewing = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để đặt lịch xem phòng.');
      navigate(ROUTES.LOGIN);
      return;
    }

    if (user?.role !== ROLES.TENANT) {
      toast.warning('Chỉ tài khoản Người thuê (Tenant) mới có thể đặt lịch xem phòng.');
      return;
    }

    setIsBookingModalOpen(true);
  };

  // Request rental handler
  const handleRequestRental = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để gửi yêu cầu thuê phòng.');
      navigate(ROUTES.LOGIN);
      return;
    }

    if (user?.role !== ROLES.TENANT) {
      toast.warning('Chỉ tài khoản Người thuê (Tenant) mới có thể gửi yêu cầu thuê phòng.');
      return;
    }

    setIsRentalModalOpen(true);
  };

  const handleSendMessage = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để nhắn tin với chủ trọ.');
      navigate(ROUTES.LOGIN);
      return;
    }
    
    if (!post) return;

    const partnerAccountId = post.landlordAccountId || post.landlordId;
    if (!partnerAccountId) {
      toast.error('Không tìm thấy thông tin liên hệ của chủ trọ.');
      return;
    }

    openDirectChat({
      partnerId: partnerAccountId,
      partnerName: post.landlordName || 'Chủ trọ',
      postId: post.id,
      postTitle: post.title,
      postPrice: post.price,
      postImage: post.imageUrls && post.imageUrls.length > 0 ? post.imageUrls[0] : undefined,
    });
  };

  if (loading) {
    return (
      <div className="room-skeleton-container">
        <div className="room-skeleton-wrapper">
          <div className="room-skeleton-breadcrumb" />
          <div className="room-skeleton-layout">
            <div className="room-skeleton-main">
              <div className="room-skeleton-box room-skeleton-gallery" />
              <div className="room-skeleton-box room-skeleton-info" />
              <div className="room-skeleton-box room-skeleton-amenities" />
            </div>
            <div>
              <div className="room-skeleton-sidebar-box" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  if (error || !post) {
    return (
      <div className="room-error-container">
        <div className="room-error-card">
          <div className="room-error-icon">
            <FiAlertCircle />
          </div>
          <h2 className="room-error-title">
            {error === 'Không tìm thấy phòng trọ' ? 'Không tìm thấy phòng trọ' : 'Có lỗi xảy ra'}
          </h2>
          <p className="room-error-message">
            {error || 'Không thể tải thông tin chi tiết phòng trọ. Vui lòng kiểm tra lại liên kết.'}
          </p>
          <div className="room-error-actions">
            <button
              type="button"
              onClick={() => id && fetchPostDetail(parseInt(id, 10))}
              className="room-error-button room-error-button-retry"
            >
              <FiRefreshCw /> Thử lại
            </button>
            <button
              type="button"
              onClick={() => navigate(ROUTES.ROOM_LIST || '/rooms')}
              className="room-error-button room-error-button-back"
            >
              <FiArrowLeft /> Quay lại danh sách phòng
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="room-detail-page">
      {/* BREADCRUMB */}
      <div className="room-breadcrumb">
        <div className="room-breadcrumb-inner">
          <Link to={ROUTES.HOME}>Trang chủ</Link>
          <FiChevronRight className="room-breadcrumb-separator" />
          <Link to={ROUTES.ROOM_LIST || '/rooms'}>Phòng trọ</Link>
          {post.province && (
            <>
              <FiChevronRight className="room-breadcrumb-separator" />
              <span>{post.province}</span>
            </>
          )}
          {post.district && (
            <>
              <FiChevronRight className="room-breadcrumb-separator" />
              <span>{post.district}</span>
            </>
          )}
          <FiChevronRight className="room-breadcrumb-separator" />
          <span className="room-breadcrumb-current">{post.title}</span>
        </div>
      </div>

      {/* MAIN */}
      <main className="room-detail-container">
        <div className="room-detail-layout">
          {/* LEFT */}
          <section className="room-detail-main">
            {/* Gallery */}
            <div className="room-gallery-wrapper">
              <RoomImageGallery
                images={post.imageUrls || []}
                title={post.title}
                isFavorite={isFavorite}
                onToggleFavorite={handleToggleFavorite}
                onShare={handleShare}
              />
            </div>

            {/* Room information */}
            <section className="room-content-section">
              <RoomInformation post={post} />
            </section>

            {/* Amenities */}
            <section className="room-content-section">
              <RoomAmenities amenities={post.amenities || []} />
            </section>

            {/* Location */}
            <section className="room-content-section room-location-section">
              <RoomLocation
                address={post.address}
                ward={post.ward}
                district={post.district}
                province={post.province}
                latitude={post.latitude}
                longitude={post.longitude}
                title={post.title}
              />
            </section>

            {/* Reviews */}
            <RoomReviews postId={post.id} />
          </section>

          {/* RIGHT SIDEBAR */}
          <aside className="room-detail-sidebar">
            <div className="room-sidebar-sticky">
              <div className="room-contact-wrapper">
                <LandlordContactCard
                  post={post}
                  isFavorite={isFavorite}
                  onBookViewing={handleBookViewing}
                  onSendMessage={handleSendMessage}
                  onRequestRental={handleRequestRental}
                  onToggleFavorite={handleToggleFavorite}
                />
              </div>
            </div>
          </aside>
        </div>

        {/* SIMILAR ROOMS */}
        <section className="room-similar-section">
          <div className="room-section-heading">
            <div>
              <h2>Phòng trọ tương tự</h2>
              <p>Một số phòng trọ khác có thể phù hợp với bạn</p>
            </div>
            <Link to={ROUTES.ROOM_LIST || '/rooms'}>
              Xem tất cả <FiChevronRight />
            </Link>
          </div>
          <SimilarRooms currentPostId={post.id} categoryId={post.categoryId} />
        </section>
      </main>

      {/* BOOKING MODAL */}
      <BookingModal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
        post={post}
      />

      {/* RENTAL REQUEST MODAL */}
      <RentalRequestModal
        isOpen={isRentalModalOpen}
        onClose={() => setIsRentalModalOpen(false)}
        postId={post.id}
        postTitle={post.title}
        price={post.price}
        address={`${post.address}, ${post.ward || ''}, ${post.district || ''}, ${post.province || ''}`}
      />
    </div>
  );
};

export default RoomDetailPage;
