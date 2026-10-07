import { useEffect, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { FiChevronLeft, FiChevronRight, FiFilter, FiMapPin, FiSearch } from 'react-icons/fi';
import { postService } from '../../services/postService';
import { categoryService, type RoomCategory } from '../../services/categoryService';
import { adminService, type AdminCatalogItem } from '../../services/adminService';
import type { PostSearchParams, PostSearchResult } from '../../types/post.types';
import RoomCard from '../../components/room/RoomCard';
import Modal from '../../components/common/Modal';
import { getApiErrorMessage } from '../../utils/apiError';
import './RoomSearch.css';

const fields = [
  ['keyword', 'Từ khóa', 'text'], ['province', 'Tỉnh/Thành phố', 'text'],
  ['district', 'Quận/Huyện', 'text'], ['ward', 'Phường/Xã', 'text'],
  ['minPrice', 'Giá từ (đ)', 'number'], ['maxPrice', 'Giá đến (đ)', 'number'],
  ['minArea', 'Diện tích từ (m²)', 'number'], ['maxArea', 'Diện tích đến (m²)', 'number'],
  ['maxOccupants', 'Số người ở', 'number'], ['latitude', 'Vĩ độ', 'number'],
  ['longitude', 'Kinh độ', 'number'], ['radiusInKm', 'Bán kính (km)', 'number'],
] as const;
const numeric = ['minPrice', 'maxPrice', 'minArea', 'maxArea', 'maxOccupants', 'categoryId', 'latitude', 'longitude', 'radiusInKm', 'pageNumber', 'pageSize'];
const empty: PostSearchResult = { items: [], totalCount: 0, pageNumber: 1, pageSize: 12, totalPages: 0 };
function parseQuery(url: string): PostSearchParams {
  const values = new URLSearchParams(url);
  const params: Record<string, string | number | number[]> = {};
  for (const [key, value] of values) {
    if (key !== 'amenityIds' && value) params[key] = numeric.includes(key) ? Number(value) : value;
  }
  const amenities = values.getAll('amenityIds').map(Number).filter(Number.isFinite);
  if (amenities.length) params.amenityIds = amenities;
  return { ...params, pageSize: Number(params.pageSize) || 12 } as PostSearchParams;
}

export default function RoomListPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const url = searchParams.toString();
  const [draft, setDraft] = useState<Record<string, string>>(() => Object.fromEntries(searchParams));
  const [amenityIds, setAmenityIds] = useState<string[]>(() => searchParams.getAll('amenityIds'));
  const [categories, setCategories] = useState<RoomCategory[]>([]);
  const [amenities, setAmenities] = useState<AdminCatalogItem[]>([]);
  const [result, setResult] = useState(empty);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filterOpen, setFilterOpen] = useState(false);
  useEffect(() => {
    let active = true;
    void Promise.resolve().then(async () => {
      if (!active) return;
      setDraft(Object.fromEntries(new URLSearchParams(url)));
      setAmenityIds(new URLSearchParams(url).getAll('amenityIds'));
      setLoading(true);
      setError('');
      try {
        const response = await postService.searchPage(parseQuery(url));
        if (active) setResult(response.data);
      } catch (e) {
        if (active) setError(getApiErrorMessage(e, 'Không thể tải danh sách phòng.'));
      } finally { if (active) setLoading(false); }
    });
    return () => { active = false; };
  }, [url]);
  useEffect(() => {
    let active = true;
    void categoryService.getActiveCategories().then(r => { if (active) setCategories(r.data || []); }).catch(() => {});
    void adminService.getAmenities().then(r => { if (active) setAmenities(r.data || []); }).catch(() => {});
    return () => { active = false; };
  }, []);
  const apply = (event: FormEvent) => {
    event.preventDefault();
    const values = new URLSearchParams();
    for (const [key, value] of Object.entries(draft)) {
      if (value.trim() && !['pageNumber', 'amenityIds', 'pageIndex'].includes(key)) values.set(key, value.trim());
    }
    amenityIds.forEach(value => values.append('amenityIds', value));
    values.set('pageNumber', '1');
    setSearchParams(values);
    setFilterOpen(false);
  };
  const changePage = (page: number) => {
    const values = new URLSearchParams(searchParams);
    values.set('pageNumber', String(page));
    setSearchParams(values);
  };
  const locate = () => {
    if (!navigator.geolocation) { setError('Trình duyệt không hỗ trợ định vị.'); return; }
    navigator.geolocation.getCurrentPosition(position => {
      setDraft(previous => ({ ...previous, latitude: String(position.coords.latitude), longitude: String(position.coords.longitude), radiusInKm: previous.radiusInKm || '5', sortBy: 'distance' }));
    }, () => setError('Không lấy được vị trí. Vui lòng kiểm tra quyền định vị.'));
  };
  const filters = (prefix: string) => <form onSubmit={apply} className="search-filter-form">
    <h2>Bộ lọc</h2>
    <div className="search-filter-fields">
      {fields.map(([key, label, type]) => <label key={key} htmlFor={prefix + key}>
        <span>{label}</span>
        <input id={prefix + key} type={type} step="any" value={draft[key] || ''} onChange={e => setDraft(previous => ({ ...previous, [key]: e.target.value }))} />
      </label>)}
    </div>
    <label htmlFor={prefix + 'categoryId'}><span>Loại phòng</span>
      <select id={prefix + 'categoryId'} value={draft.categoryId || ''} onChange={e => setDraft(previous => ({ ...previous, categoryId: e.target.value }))}>
        <option value="">Tất cả</option>{categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
      </select>
    </label>
    <fieldset><legend>Tiện ích</legend>{amenities.filter(a => a.isActive !== false).map(a => <label key={a.id} className="search-amenity">
      <input type="checkbox" checked={amenityIds.includes(String(a.id))} onChange={e => setAmenityIds(previous => e.target.checked ? [...previous, String(a.id)] : previous.filter(v => v !== String(a.id)))} />{a.name}
    </label>)}</fieldset>
    <button type="button" onClick={locate} className="search-secondary"><FiMapPin />Vị trí của tôi</button>
    <div className="search-filter-actions"><button type="submit" className="search-primary"><FiSearch />Tìm phòng</button>
      <button type="button" className="search-secondary" onClick={() => { setSearchParams({}); setFilterOpen(false); }}>Xóa lọc</button>
    </div>
  </form>;
  return <div className="rental-search">
    <header className="rental-search-heading"><div><h1>Phòng trọ cho thuê</h1>
      <p role="status">{loading ? 'Đang tìm phòng...' : error ? 'Không tải được kết quả' : result.totalCount.toLocaleString('vi-VN') + ' phòng phù hợp'}</p>
    </div><button type="button" className="search-mobile-filter search-secondary" onClick={() => setFilterOpen(true)}><FiFilter />Bộ lọc</button></header>
    <div className="rental-search-layout">
      <aside className="search-desktop-filter">{filters('desktop-')}</aside>
      <main className="rental-search-results" aria-busy={loading}>
        <div className="search-result-toolbar"><span>{loading ? '' : 'Trang ' + result.pageNumber + ' / ' + Math.max(1, result.totalPages)}</span>
          <select aria-label="Sắp xếp" value={searchParams.get('sortBy') || 'new'} onChange={e => {
            const values = new URLSearchParams(searchParams); values.set('sortBy', e.target.value); values.set('pageNumber', '1'); setSearchParams(values);
          }}>
            <option value="new">Mới nhất</option><option value="price_asc">Giá thấp đến cao</option><option value="price_desc">Giá cao đến thấp</option><option value="distance">Gần nhất</option>
          </select>
        </div>
        {error ? <p role="alert" className="form-error">{error}</p> : loading ? <p role="status">Đang tải kết quả...</p> :
          result.items.length ? <div className="search-room-grid">{result.items.map(post => <div key={post.id}><RoomCard post={post} />{post.distanceInKm != null && <p className="search-distance">{post.distanceInKm} km</p>}</div>)}</div> :
            <div className="search-empty"><FiSearch size={32} /><h2>Không tìm thấy phòng phù hợp</h2></div>}
        {!loading && !error && result.totalPages > 1 && <nav className="search-pagination" aria-label="Phân trang">
          <button type="button" className="search-secondary" disabled={result.pageNumber <= 1} onClick={() => changePage(result.pageNumber - 1)} aria-label="Trang trước"><FiChevronLeft /></button>
          <span>{result.pageNumber} / {result.totalPages}</span>
          <button type="button" className="search-secondary" disabled={result.pageNumber >= result.totalPages} onClick={() => changePage(result.pageNumber + 1)} aria-label="Trang sau"><FiChevronRight /></button>
        </nav>}
      </main>
    </div>
    <Modal isOpen={filterOpen} onClose={() => setFilterOpen(false)} title="Lọc phòng trọ">{filters('mobile-')}</Modal>
  </div>;
}
