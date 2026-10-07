import React, { useEffect, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { FiSearch, FiMapPin, FiHome, FiArrowUpRight, FiChevronRight, FiChevronDown } from 'react-icons/fi';
import { categoryService, RoomCategory } from '../../services/categoryService';
import { postService } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import { formatPrice } from '../../utils/helpers';
import RoomCard from '../../components/room/RoomCard';
import RoomAvailabilityBadge from '../../components/room/RoomAvailabilityBadge';
import PageState from '../../components/common/PageState';
import BlogCard from '../../components/blog/BlogCard';
import { blogService } from '../../services/blogService';
import { BlogPost } from '../../types/blog.types';
import './HomePage.css';

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
      <img src={room.thumbnailUrl} alt={room.title} loading="lazy" onError={e => { e.currentTarget.onerror = null; e.currentTarget.src = '/room-placeholder.svg'; }} />
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
  const [articles, setArticles] = useState<BlogPost[]>([]);
  const [retryKey, setRetryKey] = useState(0);

  useEffect(() => {
    let active = true;

    const loadHomeData = async () => {
      setLoading(true);
      setLoadError(false);

      try {
        const [categoryResponse, featuredResponse, recentResponse, blogResponse] = await Promise.allSettled([
          categoryService.getActiveCategories(),
          postService.getPosts({ sortBy: 'views', pageSize: 4 }),
          postService.getPosts({ sortBy: 'new', pageSize: 4 }),
          blogService.getPublishedPosts(),
        ]);

        if (!active) return;
        setCategories(categoryResponse.status === 'fulfilled' ? categoryResponse.value.data ?? [] : []);
        setFeaturedPosts(featuredResponse.status === 'fulfilled' ? featuredResponse.value.data ?? [] : []);
        setRecentPosts(recentResponse.status === 'fulfilled' ? recentResponse.value.data ?? [] : []);
        setArticles(blogResponse.status === 'fulfilled' ? (blogResponse.value.data ?? []).slice(0, 3) : []);
        setLoadError(featuredResponse.status === 'rejected' || recentResponse.status === 'rejected');
      } catch {
        if (active) setLoadError(true);
      } finally {
        if (active) setLoading(false);
      }
    };

    void loadHomeData();
    return () => { active = false; };
  }, [retryKey]);

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
    <div className="home-page bg-[#f7f8fa] min-h-screen">
      <section className="home-hero">
        <img
          src="https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?w=1920&h=700&fit=crop"
          alt="Tìm phòng trọ"
          className="home-hero-image"
        />
        <div className="home-hero-overlay" />

        <div className="home-hero-content">
          <h1 className="home-hero-title">Phòng trọ cho thuê</h1>
          <p className="home-hero-subtitle">Tìm nơi ở phù hợp với bạn, từ khu vực đến ngân sách.</p>

          <form onSubmit={handleSearch} className="hero-search">
            <div className="hero-search-main">
              <FiSearch className="w-5 h-5 text-gray-400 mr-2 flex-shrink-0" />
              <input
                type="text"
                aria-label="Từ khóa tìm phòng"
                value={query}
                onChange={event => setQuery(event.target.value)}
                placeholder="Tìm theo khu vực, đường, quận hoặc từ khóa..."
                className="hero-search-input"
              />
              <button type="submit" className="hero-search-button">Tìm kiếm</button>
            </div>

            <div className="hero-filters">
              <div className="hero-filter">
                <select aria-label="Tỉnh hoặc thành phố" value={filters.province} onChange={event => updateFilter('province', event.target.value)}>
                  <option value="">Toàn quốc</option>
                  {PROVINCES.map(province => <option key={province} value={province}>{province}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select aria-label="Khoảng giá" value={filters.price} onChange={event => updateFilter('price', event.target.value)}>
                  <option value="">Giá phòng</option>
                  {PRICE_RANGES.map(range => <option key={range.value} value={range.value}>{range.label}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select aria-label="Diện tích" value={filters.area} onChange={event => updateFilter('area', event.target.value)}>
                  <option value="">Diện tích</option>
                  {AREA_RANGES.map(range => <option key={range.value} value={range.value}>{range.label}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
              <div className="hero-filter">
                <select aria-label="Loại phòng" value={filters.categoryId} onChange={event => updateFilter('categoryId', event.target.value)}>
                  <option value="">Loại phòng</option>
                  {categories.map(category => <option key={category.id} value={category.id}>{category.name}</option>)}
                </select>
                <FiChevronDown className="hero-filter-icon" />
              </div>
            </div>
          </form>
          <div className="home-budget-links"><span>Ngân sách:</span>{PRICE_RANGES.slice(0, 3).map(range => {
            const params = new URLSearchParams(); setRangeParameters(params, range.value, 'minPrice', 'maxPrice');
            return <Link key={range.value} to={`/rooms?${params}`}>{range.label}</Link>;
          })}</div>
        </div>
      </section>

      <section className="home-discovery" id="areas">
        <div className="home-discovery-inner">
          <div className="section-heading"><div><p className="home-eyebrow">KHU VỰC</p><h2 className="section-title">Bạn muốn sống ở đâu?</h2></div><Link to="/rooms" className="section-more">Toàn quốc <FiChevronRight /></Link></div>
          <div className="home-area-grid">{PROVINCES.map((province, i) => <Link key={province} to={`/rooms?province=${encodeURIComponent(province)}`} className={`home-area-link area-${i}`}><span className="home-area-icon"><FiMapPin /></span><div><strong>{province}</strong><span>Xem phòng cho thuê</span></div><FiArrowUpRight /></Link>)}</div>
          {categories.length > 0 && <nav className="home-category-links" aria-label="Loại phòng">{categories.map(category => <Link key={category.id} to={`/rooms?categoryId=${category.id}`}><FiHome />{category.name}<FiChevronRight /></Link>)}</nav>}
        </div>
      </section>

      <section className="home-section">
        <div className="section-heading">
          <div><p className="home-eyebrow">ĐƯỢC QUAN TÂM</p><h2 className="section-title">Phòng nổi bật</h2></div>
          <Link to="/rooms?sortBy=views" className="section-more">Xem thêm<FiChevronRight /></Link>
        </div>
        {loadError ? (
          <PageState type="error" message="Không thể tải tin đăng." onRetry={() => setRetryKey(key => key + 1)} />
        ) : (
          <div className="room-grid">
            {loading ? Array.from({ length: 4 }, (_, index) => <div key={index} className="home-room-skeleton animate-pulse"><div /><span /><span /></div>) : featuredPosts.map(room => <RoomCard key={room.id} post={room} badge="Xem nhiều" />)}
          </div>
        )}
        {!loading && !loadError && featuredPosts.length === 0 && <PageState type="empty" message="Chưa có phòng được đăng. Quay lại sau để xem tin mới." />}
      </section>

      <section className="home-section">
        <div className="section-heading">
          <div><p className="home-eyebrow">MỚI CẬP NHẬT</p><h2 className="section-title">Tin mới đăng</h2></div>
          <Link to="/rooms" className="section-more">Xem thêm<FiChevronRight /></Link>
        </div>
        {!loadError && (
          <div className="recent-grid">
            {loading ? Array.from({ length: 4 }, (_, index) => <div key={index} className="recent-card animate-pulse bg-slate-200" />) : recentPosts.map(room => (
              <Link key={room.id} to={`/rooms/${room.id}`} className="recent-card">
                <RoomImage room={room} className="recent-image" />
                <div className="recent-content">
                  <div><RoomAvailabilityBadge status={room.roomStatus} /><h3 className="recent-title">{room.title}</h3><div className="recent-price">{formatPrice(room.price)}/tháng</div></div>
                  <div className="recent-footer"><span className="flex items-center gap-1"><FiMapPin size={13} />{room.area} m² · {room.district || room.province}</span><span>{formatPostedDate(room.createdAt)}</span></div>
                </div>
              </Link>
            ))}
          </div>
        )}
        {!loading && !loadError && recentPosts.length === 0 && <PageState type="empty" message="Chưa có tin mới." />}
      </section>
      {articles.length > 0 && <section className="home-section home-reading"><div className="section-heading"><div><p className="home-eyebrow">GÓC NGƯỜI THUÊ</p><h2 className="section-title">Chuyện tìm nhà, chuyện an cư</h2></div><Link to="/blog" className="section-more">Xem bài viết <FiChevronRight /></Link></div><div className="home-blog-grid">{articles.map(article => <BlogCard key={article.id} post={article} />)}</div></section>}
    </div>
  );
};

export default HomePage;
