import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { FiSearch, FiMapPin, FiHeart, FiChevronRight, FiChevronDown } from 'react-icons/fi';
import { categoryService, RoomCategory } from '../../services/categoryService';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import { formatPrice } from '../../utils/helpers';

const PRICE_RANGES = [
  { label: 'Dưới 2 triệu', value: ':2000000' },
  { label: '2 - 3 triệu', value: '2000000:3000000' },
  { label: '3 - 5 triệu', value: '3000000:5000000' },
  { label: '5 - 10 triệu', value: '5000000:10000000' },
  { label: 'Trên 10 triệu', value: '10000000:' },
];

const AREA_RANGES = [
  { label: 'Dưới 20 m²', value: ':20' },
  { label: '20 - 30 m²', value: '20:30' },
  { label: '30 - 50 m²', value: '30:50' },
  { label: 'Trên 50 m²', value: '50:' },
];

const PROVINCES = ['Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Cần Thơ'];

const setRangeParameters = (params: URLSearchParams, value: string, minKey: string, maxKey: string) => {
  if (!value) return;

  const [min, max] = value.split(':');
  if (min) params.set(minKey, min);
  if (max) params.set(maxKey, max);
};

const formatPostedDate = (dateString: string) => {
  const date = new Date(dateString);
  const difference = Math.floor((Date.now() - date.getTime()) / 86_400_000);

  if (difference <= 0) return 'Hôm nay';
  if (difference === 1) return 'Hôm qua';
  if (difference < 7) return `${difference} ngày trước`;
  return date.toLocaleDateString('vi-VN');
};

const RoomImage: React.FC<{ room: PostListItem; className: string }> = ({ room, className }) => (
  <div className={className}>
    {room.thumbnailUrl ? (
      <img src={room.thumbnailUrl} alt={room.title} />
    ) : (
      <div className="flex h-full items-center justify-center bg-slate-100 text-sm text-slate-500">Chưa có ảnh</div>
    )}
  </div>
);

const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [filters, setFilters] = useState({ province: '', price: '', area: '', categoryId: '' });
  const [categories, setCategories] = useState<RoomCategory[]>([]);
  const [featuredPosts, setFeaturedPosts] = useState<PostListItem[]>([]);
  const [recentPosts, setRecentPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    let active = true;

    const loadHomeData = async () => {
      setLoading(true);
      setLoadError(false);

      try {
        const [categoryResponse, featuredResponse, recentResponse] = await Promise.all([
          categoryService.getActiveCategories(),
          postService.getPosts({ sortBy: 'views', pageSize: 4 }),
          postService.getPosts({ sortBy: 'new', pageSize: 4 }),
        ]);

        if (!active) return;
        setCategories(categoryResponse.data ?? []);
        setFeaturedPosts(featuredResponse.data ?? []);
        setRecentPosts(recentResponse.data ?? []);
      } catch {
        if (active) setLoadError(true);
      } finally {
        if (active) setLoading(false);
      }
    };

    void loadHomeData();
    return () => { active = false; };
  }, []);

  const handleSearch = (event: React.FormEvent) => {
    event.preventDefault();
    const params = new URLSearchParams();

    if (query.trim()) params.set('keyword', query.trim());
    if (filters.province) params.set('province', filters.province);
    if (filters.categoryId) params.set('categoryId', filters.categoryId);
    setRangeParameters(params, filters.price, 'minPrice', 'maxPrice');
    setRangeParameters(params, filters.area, 'minArea', 'maxArea');

    navigate(`/rooms?${params.toString()}`);
  };

  const updateFilter = (key: keyof typeof filters, value: string) => {
    setFilters(current => ({ ...current, [key]: value }));
  };

  return (
    <div className="bg-[#f7f8fa] min-h-screen">
      <section className="home-hero">
        <img
          src="https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?w=1920&h=700&fit=crop"
          alt="Tìm phòng trọ"
          className="home-hero-image"
        />
        <div className="home-hero-overlay" />

        <div className="home-hero-content">
          <h1 className="home-hero-title">Tìm phòng trọ tốt</h1>
          <p className="home-hero-subtitle">Đăng tin miễn phí - phòng trọ, căn hộ mini trên toàn quốc</p>

          <form onSubmit={handleSearch} className="hero-search">
            <div className="hero-search-main">
              <FiSearch className="w-5 h-5 text-gray-400 mr-2 flex-shrink-0" />
              <input
                type="text"
                value={query}
                onChange={event => setQuery(event.target.value)}
                placeholder="Tìm theo khu vực, đường, quận hoặc từ khóa..."
                className="hero-search-input"
              />
              <button type="submit" className="hero-search-button">Tìm kiếm</button>
            </div>

            <div className="hero-filters">
              <div className="hero-filter">
                <select value={filters.province} onChange={event => updateFilter('province', event.target.value)}>
                  <option value="">Toàn quốc</option>
                  {PROVINCES.map(province => <option key={province} value={province}>{province}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select value={filters.price} onChange={event => updateFilter('price', event.target.value)}>
                  <option value="">Giá phòng</option>
                  {PRICE_RANGES.map(range => <option key={range.value} value={range.value}>{range.label}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select value={filters.area} onChange={event => updateFilter('area', event.target.value)}>
                  <option value="">Diện tích</option>
                  {AREA_RANGES.map(range => <option key={range.value} value={range.value}>{range.label}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select value={filters.categoryId} onChange={event => updateFilter('categoryId', event.target.value)}>
                  <option value="">Loại phòng</option>
                  {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
            </div>
          </form>
        </div>
      </section>

      <section className="home-section">
        <div className="section-heading">
          <h2 className="section-title">Tin nổi bật</h2>
          <Link to="/rooms?sortBy=views" className="section-more">Xem thêm<FiChevronRight /></Link>
        </div>
        {loadError ? (
          <p className="text-sm text-slate-600">Không thể tải tin đăng. Vui lòng thử lại sau.</p>
        ) : (
          <div className="room-grid">
            {loading ? Array.from({ length: 4 }, (_, index) => <div key={index} className="room-card animate-pulse bg-slate-200" />) : featuredPosts.map(room => (
              <div key={room.id} onClick={() => navigate(`/rooms/${room.id}`)} className="room-card">
                <RoomImage room={room} className="room-card-image" />
                <span className="room-card-badge bg-orange-500">Xem nhiều</span>
                <button className="room-card-heart" onClick={event => event.stopPropagation()} aria-label="Lưu tin"><FiHeart size={16} /></button>
                <div className="room-card-content">
                  <div className="room-card-meta"><span className="room-card-price">{formatPrice(room.price)}/tháng</span><span className="room-card-area">{room.area} m²</span></div>
                  <h3 className="room-card-title">{room.title}</h3>
                  <p className="room-card-address"><FiMapPin size={13} className="flex-shrink-0" /><span>{room.address}, {room.district}, {room.province}</span></p>
                  <p className="room-card-date">{formatPostedDate(room.createdAt)}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="home-section">
        <div className="section-heading">
          <h2 className="section-title">Tin mới đăng</h2>
          <Link to="/rooms" className="section-more">Xem thêm<FiChevronRight /></Link>
        </div>
        {!loadError && (
          <div className="recent-grid">
            {loading ? Array.from({ length: 4 }, (_, index) => <div key={index} className="recent-card animate-pulse bg-slate-200" />) : recentPosts.map(room => (
              <div key={room.id} onClick={() => navigate(`/rooms/${room.id}`)} className="recent-card">
                <RoomImage room={room} className="recent-image" />
                <div className="recent-content">
                  <div><h3 className="recent-title">{room.title}</h3><div className="recent-price">{formatPrice(room.price)}/tháng</div></div>
                  <div className="recent-footer"><span className="flex items-center gap-1"><FiMapPin size={13} />{room.area} m² - {room.address}, {room.district}</span><span>{formatPostedDate(room.createdAt)}</span></div>
                </div>
              </div>
            ))}
          </div>
        )}
      </section>
    </div>
  );
};

export default HomePage;
