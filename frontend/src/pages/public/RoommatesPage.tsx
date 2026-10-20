import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiSearch, FiMapPin, FiUsers, FiChevronLeft, FiChevronRight, FiMaximize2, FiImage, FiUser, FiCalendar } from 'react-icons/fi';
import PageState from '../../components/common/PageState';
import StatusBadge from '../../components/common/StatusBadge';
import { roommateService, type RoommateResult } from '../../services/roommateService';
import { getApiErrorMessage } from '../../utils/apiError';
import { formatPrice } from '../../utils/helpers';
import './Roommates.css';

export default function RoommatesPage() {
  const [params, setParams] = useSearchParams();
  const keyword = params.get('keyword') || '';
  const page = Math.max(1, Number(params.get('page')) || 1);
  const [draft, setDraft] = useState(keyword);
  const [result, setResult] = useState<RoommateResult | null>(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    let active = true;
    void Promise.resolve().then(async () => {
      setLoading(true); setError(''); setDraft(keyword);
      try { const data = await roommateService.search(keyword, page); if (active) setResult(data); }
      catch (e) { if (active) setError(getApiErrorMessage(e, 'Không tải được tin ở ghép.')); }
      finally { if (active) setLoading(false); }
    });
    return () => { active = false; };
  }, [keyword, page]);
  return <main className="roommates-page rm-public-list">
    <nav className="rm-breadcrumb" aria-label="Đường dẫn"><Link to="/">Trang chủ</Link><FiChevronRight /><span aria-current="page">Ở ghép</span></nav>
    <header className="roommates-heading"><div><p className="rm-eyebrow"><FiUsers />Ở ghép</p><h1>Tìm bạn ở ghép</h1><p>{loading ? 'Đang tải tin ở ghép...' : result ? `${result.totalCount} tin đang tìm người` : 'Tìm người cùng chia sẻ không gian sống'}</p></div><Link to="/tenant/roommates" className="rm-button"><FiUser />Ở ghép của tôi</Link></header>
    <form className="rm-search" onSubmit={e => { e.preventDefault(); setParams(draft.trim() ? { keyword: draft.trim() } : {}); }}><label><FiSearch /><input aria-label="Tìm tin ở ghép" placeholder="Nhập khu vực hoặc từ khóa..." value={draft} onChange={e => setDraft(e.target.value)} /></label><button className="rm-button primary" type="submit"><FiSearch />Tìm kiếm</button></form>
    {!loading && !error && !!result?.items.length && <div className="rm-result-toolbar"><span><strong>{result.totalCount}</strong> tin phù hợp</span><span>Trang {page} / {Math.max(1, result.totalPages)}</span></div>}
    {loading ? <PageState type="loading" message="Đang tải tin ở ghép..." /> : error ? <PageState type="error" message={error} /> : !result?.items.length ? <PageState type="empty" message="Chưa có tin tìm người ở ghép phù hợp." /> : <>
      <div className="rm-grid">{result.items.map(post => <article className="rm-card" key={post.id}>
        <div className="rm-card-media"><Link to={`/roommates/${post.id}`} aria-label={`Xem ${post.title}`}><img src={post.imageUrls[0] || '/room-placeholder.svg'} alt={post.title} loading="lazy" onError={e => { e.currentTarget.onerror = null; e.currentTarget.src = '/room-placeholder.svg'; }} /></Link><div className="rm-card-status"><StatusBadge label="Đang tìm người" tone="success" /></div>{post.imageUrls.length > 0 && <span className="rm-image-count"><FiImage />{post.imageUrls.length}</span>}</div>
        <div className="rm-card-body">
          <div className="rm-card-price-row"><div className="rm-price">{post.estimatedShare != null ? formatPrice(post.estimatedShare) : 'Thỏa thuận'}<small>{post.estimatedShare != null ? '/người/tháng' : 'Chi phí dự kiến'}</small></div><span className="rm-slot-badge">Còn {Math.min(post.availableSlots, post.remainingRequestedSlots)} chỗ</span></div>
          <h2><Link to={`/roommates/${post.id}`}>{post.title}</Link></h2>
          <p className="rm-card-address"><FiMapPin /><span>{[post.district, post.province].filter(Boolean).join(', ') || post.address}</span></p>
          <div className="rm-card-facts"><span><FiMaximize2 />{post.area} m²</span><span><FiUsers />{post.currentOccupants}/{post.maxOccupants} người</span></div>
          {!!post.amenities.length && <div className="rm-amenities">{post.amenities.slice(0, 3).map(a => <span key={a}>{a}</span>)}</div>}
          <footer className="rm-card-footer"><span><FiUser />{post.creatorName}</span><time dateTime={post.createdAt}><FiCalendar />{new Date(post.createdAt).toLocaleDateString('vi-VN')}</time></footer>
        </div>
      </article>)}</div>
      {result.totalPages > 1 && <nav className="rm-pagination" aria-label="Phân trang tin ở ghép"><button aria-label="Trang trước" className="rm-button" disabled={page === 1} onClick={() => setParams({ keyword, page: String(page - 1) })}><FiChevronLeft /></button><span>{page}/{result.totalPages}</span><button aria-label="Trang sau" className="rm-button" disabled={page >= result.totalPages} onClick={() => setParams({ keyword, page: String(page + 1) })}><FiChevronRight /></button></nav>}
    </>}
  </main>;
}
