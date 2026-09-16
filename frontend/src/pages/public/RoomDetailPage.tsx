import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { postService } from '../../services/postService';
import { Post, RoomStatus, PostStatus } from '../../types/post.types';
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
import SimilarRooms from '../../components/room/SimilarRooms';

// Realistic fallback data for frontend preview when backend server is offline
const FALLBACK_POSTS: Record<number, Post> = {
  1: {
    id: 1,
    title: 'Cho nữ thuê phòng trong nhà riêng 4 tầng, ngõ 254D Minh Khai, giá 2,4 tr',
    description: `Phòng trọ nằm trong nhà riêng 4 tầng cao cấp, khu vực yên tĩnh, dân trí cao, an ninh tốt.
- Phòng rộng rãi 25m², có ban công thoáng mát và cửa sổ đón ánh sáng tự nhiên.
- Đã trang bị đầy đủ: Điều hòa nhiệt độ, bình nóng lạnh, giường nệm cao cấp, tủ quần áo lớn, bàn học/làm việc.
- Không chung chủ, giờ giấc hoàn toàn tự do, chìa khóa trao tay.
- Gần các trường đại học lớn: ĐH Bách Khoa, ĐH Kinh Tế Quốc Dân, ĐH Xây Dựng, ĐH Kinh Doanh & Công Nghệ.
- Điện nước giá dân công tơ riêng, internet cáp quang tốc độ cao.`,
    price: 2400000,
    status: PostStatus.Approved,
    rejectionReason: null,
    landlordId: 101,
    landlordName: 'Phương',
    landlordPhone: '0914362888',
    roomId: 201,
    area: 25,
    maxOccupants: 2,
    roomStatus: RoomStatus.Available,
    province: 'Hà Nội',
    district: 'Hoàng Mai',
    ward: 'Bạch Mai',
    address: 'Số 254D Ngõ Minh Khai',
    amenities: [
      { id: 1, name: 'WiFi tốc độ cao', icon: '', description: '' },
      { id: 2, name: 'Điều hòa nhiệt độ', icon: '', description: '' },
      { id: 3, name: 'Bình nóng lạnh', icon: '', description: '' },
      { id: 4, name: 'Giường ngủ & nệm', icon: '', description: '' },
      { id: 5, name: 'Tủ quần áo lớn', icon: '', description: '' },
      { id: 6, name: 'Camera an ninh 24/7', icon: '', description: '' },
      { id: 7, name: 'Chỗ để xe tầng 1 an toàn', icon: '', description: '' },
      { id: 8, name: 'Giờ giấc tự do, không chung chủ', icon: '', description: '' },
    ],
    imageUrls: [
      'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=1200&h=800&fit=crop',
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200&h=800&fit=crop',
      'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=1200&h=800&fit=crop',
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=1200&h=800&fit=crop',
    ],
    createdAt: '2026-09-15T08:00:00Z',
    updatedAt: '2026-09-15T08:00:00Z',
  },
};

const RoomDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { isAuthenticated, user } = useAuth();

  const [post, setPost] = useState<Post | null>(null);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);
  const [isFavorite, setIsFavorite] = useState<boolean>(false);
  const [isBookingModalOpen, setIsBookingModalOpen] = useState<boolean>(false);

  useEffect(() => {
    if (id) {
      fetchPostDetail(parseInt(id, 10));
    }
  }, [id]);

  const fetchPostDetail = async (postId: number) => {
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
        // Use fallback if exists
        if (FALLBACK_POSTS[postId] || FALLBACK_POSTS[1]) {
          setPost(FALLBACK_POSTS[postId] || { ...FALLBACK_POSTS[1], id: postId });
        } else {
          setError('Không tìm thấy phòng trọ');
        }
      }
    } catch (err: unknown) {
      console.warn('Backend API connection offline, using fallback room preview data:', err);
      // Gracefully load demo fallback room data
      if (FALLBACK_POSTS[postId] || FALLBACK_POSTS[1]) {
        setPost(FALLBACK_POSTS[postId] || { ...FALLBACK_POSTS[1], id: postId });
      } else {
        setError('Không thể tải thông tin phòng.');
      }
    } finally {
      setLoading(false);
    }
  };

  // Toggle favorite handler
  const handleToggleFavorite = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để lưu tin phòng trọ.');
      navigate(ROUTES.LOGIN);
      return;
    }

    if (user?.role !== ROLES.TENANT) {
      toast.info('Chức năng lưu tin dành cho tài khoản người thuê phòng.');
      return;
    }

    setIsFavorite((prev) => {
      const nextState = !prev;
      if (nextState) {
        toast.success('Đã lưu tin vào danh sách yêu thích!');
      } else {
        toast.info('Đã bỏ lưu tin phòng trọ.');
      }
      return nextState;
    });
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
      } catch (err) {
        // Fallback to clipboard if share was cancelled or failed
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

  // Send message handler (mock / prepare)
  const handleSendMessage = () => {
    if (!isAuthenticated) {
      toast.info('Vui lòng đăng nhập để nhắn tin với chủ trọ.');
      navigate(ROUTES.LOGIN);
      return;
    }
    toast.info('Chức năng nhắn tin trực tiếp đang được cập nhật.');
  };

  // =========================================================================
  // 1. SKELETON LOADING STATE
  // =========================================================================
  if (loading) {
    return (
      <div className="bg-[#f6f7f9] min-h-screen py-6">
        <div className="max-w-[1200px] mx-auto px-4">
          {/* Breadcrumb Skeleton */}
          <div className="h-4 w-72 bg-gray-200 rounded animate-pulse mb-6" />

          <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
            {/* Left Skeleton */}
            <div className="lg:col-span-8 flex flex-col gap-6">
              <div className="w-full h-[480px] bg-gray-200 rounded-2xl animate-pulse" />
              <div className="w-full h-44 bg-gray-200 rounded-2xl animate-pulse" />
              <div className="w-full h-64 bg-gray-200 rounded-2xl animate-pulse" />
            </div>

            {/* Right Skeleton */}
            <div className="lg:col-span-4 flex flex-col gap-6">
              <div className="w-full h-80 bg-gray-200 rounded-2xl animate-pulse" />
            </div>
          </div>
        </div>
      </div>
    );
  }

  // =========================================================================
  // 2. ERROR STATE
  // =========================================================================
  if (error || !post) {
    return (
      <div className="bg-[#f6f7f9] min-h-[70vh] flex items-center justify-center p-4">
        <div className="max-w-md w-full bg-white rounded-2xl p-8 border border-gray-200 shadow-sm text-center">
          <div className="w-16 h-16 rounded-full bg-red-50 text-red-500 flex items-center justify-center mx-auto mb-4">
            <FiAlertCircle className="w-8 h-8" />
          </div>
          <h2 className="text-xl font-bold text-gray-900 mb-2">
            {error === 'Không tìm thấy phòng trọ' ? 'Không tìm thấy phòng trọ' : 'Có lỗi xảy ra'}
          </h2>
          <p className="text-sm text-gray-500 mb-6">
            {error || 'Không thể tải thông tin chi tiết phòng trọ. Vui lòng kiểm tra lại liên kết.'}
          </p>

          <div className="flex items-center justify-center gap-3">
            <button
              type="button"
              onClick={() => id && fetchPostDetail(parseInt(id, 10))}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl border border-gray-300 text-sm font-bold text-gray-700 hover:bg-gray-50 transition-colors cursor-pointer bg-white"
            >
              <FiRefreshCw className="w-4 h-4" />
              Thử lại
            </button>
            <button
              type="button"
              onClick={() => navigate(ROUTES.ROOM_LIST || '/rooms')}
              className="flex items-center gap-2 px-5 py-2.5 rounded-xl bg-[#0084ff] text-white text-sm font-bold hover:bg-[#0073df] transition-colors shadow-sm cursor-pointer border-none"
            >
              <FiArrowLeft className="w-4 h-4" />
              Quay lại danh sách phòng
            </button>
          </div>
        </div>
      </div>
    );
  }

  // =========================================================================
  // 3. MAIN DETAIL PAGE LAYOUT
  // =========================================================================
  return (
    <div className="bg-[#f6f7f9] min-h-screen pb-16">
      {/* Breadcrumb Navigation */}
      <div className="bg-white border-b border-[#e5e7eb]">
        <div className="max-w-[1200px] mx-auto px-4 py-3">
          <nav aria-label="Breadcrumb" className="flex items-center flex-wrap gap-1.5 text-[13px] text-[#6b7280]">
            <Link to={ROUTES.HOME} className="hover:text-[#0084ff] transition-colors">
              Trang chủ
            </Link>
            <FiChevronRight className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
            <Link to={ROUTES.ROOM_LIST || '/rooms'} className="hover:text-[#0084ff] transition-colors">
              Phòng trọ
            </Link>
            {post.province && (
              <>
                <FiChevronRight className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
                <span className="text-[#6b7280]">{post.province}</span>
              </>
            )}
            {post.district && (
              <>
                <FiChevronRight className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
                <span className="text-[#6b7280]">{post.district}</span>
              </>
            )}
            <FiChevronRight className="w-3.5 h-3.5 text-gray-400 flex-shrink-0" />
            <span className="text-[#1f2937] font-semibold truncate max-w-[280px] sm:max-w-[420px]">
              {post.title}
            </span>
          </nav>
        </div>
      </div>

      {/* Main Content Area */}
      <main className="max-w-[1200px] mx-auto px-4 py-6">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          {/* LEFT CONTENT COLUMN (~68%) */}
          <div className="lg:col-span-8 flex flex-col gap-6">
            {/* Gallery Component */}
            <RoomImageGallery
              images={post.imageUrls || []}
              title={post.title}
              isFavorite={isFavorite}
              onToggleFavorite={handleToggleFavorite}
              onShare={handleShare}
            />

            {/* Room Info Component */}
            <RoomInformation post={post} />

            {/* Amenities Component */}
            <RoomAmenities amenities={post.amenities || []} />

            {/* Location Component */}
            <RoomLocation
              address={post.address}
              ward={post.ward}
              district={post.district}
              province={post.province}
            />
          </div>

          {/* RIGHT SIDEBAR COLUMN (~32%) */}
          <aside className="lg:col-span-4">
            <LandlordContactCard
              post={post}
              onBookViewing={handleBookViewing}
              onSendMessage={handleSendMessage}
            />
          </aside>
        </div>

        {/* Similar Rooms Section */}
        <SimilarRooms currentPostId={post.id} />
      </main>

      {/* Booking Appointment Modal */}
      <BookingModal
        isOpen={isBookingModalOpen}
        onClose={() => setIsBookingModalOpen(false)}
        post={post}
      />
    </div>
  );
};

export default RoomDetailPage;
