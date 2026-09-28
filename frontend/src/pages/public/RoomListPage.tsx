import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiChevronRight, FiMapPin, FiSearch, FiClock } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { postService, PostQueryParams } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import RoomSidebar from '../../components/room/RoomSidebar';
import { formatPrice } from '../../utils/helpers';
import './RoomListPage.css';

const RoomListPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  // Filter states
  const [keyword, setKeyword] = useState<string>(searchParams.get('keyword') || '');
  const [sortBy, setSortBy] = useState<string>(searchParams.get('sortBy') || '');
  const [categoryFilter, setCategoryFilter] = useState<string>('Cho thuê phòng trọ');
  const [priceFilter, setPriceFilter] = useState<string>('Tất cả giá');
  const [areaFilter, setAreaFilter] = useState<string>('Tất cả diện tích');

  const fetchPosts = async () => {
    setLoading(true);
    setError(null);
    try {
      const params: PostQueryParams = {
        keyword: keyword.trim() || undefined,
        sortBy: sortBy || undefined,
      };

      const res = await postService.getPosts(params);
      setPosts(res.data || []);
    } catch (error: any) {
      setError('Không thể tải danh sách phòng trọ.');
      toast.error('Không thể tải danh sách phòng trọ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchPosts();
  }, [sortBy]);

  const handleSearchSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    void fetchPosts();
  };

  const formatDate = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffTime = Math.abs(now.getTime() - date.getTime());
    const diffDays = Math.ceil(diffTime / (1000 * 60 * 60 * 24));
    
    if (diffDays === 0) return 'Hôm nay';
    if (diffDays === 1) return 'Hôm qua';
    if (diffDays < 7) return `${diffDays} ngày trước`;
    return date.toLocaleDateString('vi-VN');
  };

  // Convert PostListItem to RoomListItem format for RoomSidebar
  const latestRooms = posts.slice(0, 5).map(post => ({
    id: post.id,
    title: post.title,
    price: post.price,
    area: post.area,
    imageUrl: post.thumbnailUrl || '',
    address: post.address || '',
    ward: post.ward,
    district: post.district,
    province: post.province,
    createdAt: post.createdAt,
    category: 'room' as const,
  }));

  return (
    <div className="room-list-page">
      {/* ===== SEARCH SECTION ===== */}
      <div className="room-list-search-wrapper">
        <form onSubmit={handleSearchSubmit} className="room-search-section">
          {/* Location Dropdown */}
          <button type="button" className="room-search-location">
            <FiMapPin />
            <span>Toàn quốc</span>
            <FiChevronRight className="room-search-chevron" />
          </button>

          {/* Search Input */}
          <div className="room-search-input-wrapper">
            <FiSearch />
            <input
              type="text"
              placeholder="Tìm kiếm theo địa điểm"
              value={keyword}
              onChange={(e) => setKeyword(e.target.value)}
            />
          </div>

          {/* Search Button */}
          <button type="submit" className="room-search-button">
            Tìm kiếm
          </button>
        </form>
      </div>

      {/* ===== FILTER BAR ===== */}
      <div className="room-list-filter-wrapper">
        <div className="room-filter-bar">
          <div className="room-filter-select">
            <span>{categoryFilter}</span>
            <FiChevronRight />
          </div>

          <div className="room-filter-select">
            <span>{priceFilter}</span>
            <FiChevronRight />
          </div>

          <div className="room-filter-select">
            <span>{areaFilter}</span>
            <FiChevronRight />
          </div>
        </div>
      </div>

      {/* ===== MAIN CONTAINER ===== */}
      <div className="room-list-container">
        {/* Breadcrumb */}
        <nav className="room-list-breadcrumb">
          <Link to="/">Trang chủ</Link>
          <FiChevronRight />
          <span>Cho thuê phòng trọ</span>
        </nav>

        {/* Heading */}
        <div className="room-list-heading">
          <div>
            <h1>Cho Thuê Phòng Trọ - Tin Giá Rẻ, Chính Chủ, Tiện Nghi</h1>
            <p>Hiện có <strong>{posts.length}</strong> tin</p>
          </div>

          <select
            value={sortBy}
            onChange={(e) => setSortBy(e.target.value)}
            className="room-sort-select"
          >
            <option value="">Tin mới đăng</option>
            <option value="price_asc">Giá thấp đến cao</option>
            <option value="price_desc">Giá cao đến thấp</option>
          </select>
        </div>

        {/* ===== LAYOUT: Main + Sidebar ===== */}
        <div className="room-list-layout">
          {/* Main Content */}
          <main className="room-list-main">
            {loading ? (
              /* Loading State */
              <div className="room-list-items">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="room-list-card" style={{ opacity: 0.6 }}>
                    <div className="room-list-card-image" style={{ background: '#e5e7eb' }} />
                    <div className="room-list-card-content">
                      <div style={{ height: '24px', background: '#e5e7eb', borderRadius: '4px', marginBottom: '8px' }} />
                      <div style={{ height: '16px', background: '#e5e7eb', borderRadius: '4px', width: '70%' }} />
                    </div>
                  </div>
                ))}
              </div>
            ) : error ? (
              /* Error State */
              <div className="room-list-empty">
                <div style={{ textAlign: 'center' }}>
                  <div style={{ fontSize: '48px', marginBottom: '16px' }}>⚠️</div>
                  <h3 style={{ margin: '0 0 8px', fontSize: '18px', fontWeight: 700 }}>{error}</h3>
                  <button
                    onClick={() => fetchPosts()}
                    style={{
                      marginTop: '16px',
                      padding: '10px 24px',
                      background: '#0084ff',
                      color: 'white',
                      border: 'none',
                      borderRadius: '6px',
                      cursor: 'pointer',
                      fontWeight: 600
                    }}
                  >
                    Thử lại
                  </button>
                </div>
              </div>
            ) : posts.length === 0 ? (
              /* Empty State */
              <div className="room-list-empty">
                <div style={{ textAlign: 'center' }}>
                  <FiMapPin size={48} color="#9ca3af" style={{ marginBottom: '16px' }} />
                  <h3 style={{ margin: '0 0 8px', fontSize: '18px', fontWeight: 700, color: '#111827' }}>
                    Không tìm thấy phòng trọ
                  </h3>
                  <p style={{ margin: 0, color: '#6b7280', fontSize: '14px' }}>
                    Hãy thử thay đổi bộ lọc tìm kiếm
                  </p>
                </div>
              </div>
            ) : (
              /* Room List */
              <div className="room-list-items">
                {posts.map((post) => (
                  <Link
                    key={post.id}
                    to={`/rooms/${post.id}`}
                    className="room-list-card"
                  >
                    {/* Image */}
                    <div className="room-list-card-image">
                      {post.thumbnailUrl ? (
                        <img src={post.thumbnailUrl} alt={post.title} />
                      ) : (
                        <div style={{
                          width: '100%',
                          height: '100%',
                          display: 'flex',
                          alignItems: 'center',
                          justifyContent: 'center',
                          fontSize: '48px'
                        }}>
                          🏠
                        </div>
                      )}
                    </div>

                    {/* Content */}
                    <div className="room-list-card-content">
                      <h3 className="room-list-card-title">{post.title}</h3>

                      <div className="room-list-card-address">
                        <FiMapPin />
                        <span>{post.address}, {post.district}, {post.province}</span>
                      </div>

                      <div className="room-list-card-price-row">
                        <span className="room-list-card-price">{formatPrice(post.price)}/tháng</span>
                        <span className="room-list-card-dot">•</span>
                        <span className="room-list-card-area">{post.area}m²</span>
                      </div>

                      <div className="room-list-card-bottom">
                        <span className="room-list-card-date">
                          <FiClock />
                          {formatDate(post.createdAt)}
                        </span>
                      </div>
                    </div>
                  </Link>
                ))}
              </div>
            )}
          </main>

          {/* Sidebar */}
          <aside className="room-sidebar">
            <RoomSidebar latestRooms={latestRooms} />
          </aside>
        </div>
      </div>
    </div>
  );
};

export default RoomListPage;
