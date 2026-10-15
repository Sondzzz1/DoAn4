import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiSearch, FiMapPin, FiUsers, FiChevronLeft, FiChevronRight } from 'react-icons/fi';
import PageState from '../../components/common/PageState';
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
  return <main className="roommates-page">
    <div className="roommates-heading"><div><h1>Tìm bạn ở ghép</h1><p>{result ? `${result.totalCount} tin đang tìm người` : 'Tìm người cùng chia sẻ không gian sống'}</p></div><Link to="/tenant/roommates" className="rm-button">Ở ghép của tôi</Link></div>
    <form className="rm-search" onSubmit={e => { e.preventDefault(); setParams(draft.trim() ? { keyword: draft.trim() } : {}); }}><label><FiSearch /><input aria-label="Tìm tin ở ghép" placeholder="Khu vực hoặc từ khóa" value={draft} onChange={e => setDraft(e.target.value)} /></label><button className="rm-button primary" type="submit">Tìm kiếm</button></form>
    {loading ? <PageState type="loading" message="Đang tải tin ở ghép..." /> : error ? <PageState type="error" message={error} /> : !result?.items.length ? <PageState type="empty" message="Chưa có tin tìm người ở ghép phù hợp." /> : <>
      <div className="rm-grid">{result.items.map(post => <article className="rm-card" key={post.id}>
        <Link to={`/roommates/${post.id}`}><img src={post.imageUrls[0] || '/room-placeholder.svg'} alt={post.title} loading="lazy" onError={e => { e.currentTarget.onerror = null; e.currentTarget.src = '/room-placeholder.svg'; }} /></Link>
        <div className="rm-card-body"><div className="rm-price">{post.estimatedShare != null ? `${formatPrice(post.estimatedShare)}/người/tháng` : 'Chi phí theo thỏa thuận'}<small>Chi phí dự kiến</small></div><h2><Link to={`/roommates/${post.id}`}>{post.title}</Link></h2><p><FiMapPin />{[post.district, post.province].filter(Boolean).join(', ') || post.address}</p><p><FiUsers />{post.currentOccupants}/{post.maxOccupants} người · còn tìm {Math.min(post.availableSlots, post.remainingRequestedSlots)} người</p><div className="rm-amenities">{post.amenities.slice(0, 3).map(a => <span key={a}>{a}</span>)}</div><p className="rm-muted">{post.creatorName} · {new Date(post.createdAt).toLocaleDateString('vi-VN')}</p></div>
      </article>)}</div>
      {result.totalPages > 1 && <nav className="rm-pagination" aria-label="Phân trang tin ở ghép"><button aria-label="Trang trước" className="rm-button" disabled={page === 1} onClick={() => setParams({ keyword, page: String(page - 1) })}><FiChevronLeft /></button><span>{page}/{result.totalPages}</span><button aria-label="Trang sau" className="rm-button" disabled={page >= result.totalPages} onClick={() => setParams({ keyword, page: String(page + 1) })}><FiChevronRight /></button></nav>}
    </>}
  </main>;
}
