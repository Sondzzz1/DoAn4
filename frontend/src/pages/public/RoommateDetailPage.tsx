import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { FiArrowLeft, FiSend, FiUsers, FiMapPin, FiMaximize2, FiCalendar, FiChevronRight, FiCheck } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { useAuth } from '../../hooks/useAuth';
import Modal from '../../components/common/Modal';
import PageState from '../../components/common/PageState';
import StatusBadge from '../../components/common/StatusBadge';
import { roommateService, type RoommatePost } from '../../services/roommateService';
import { formatPrice } from '../../utils/helpers';
import { getApiErrorMessage } from '../../utils/apiError';
import './Roommates.css';

export default function RoommateDetailPage() {
  const { id } = useParams(); const { isTenant, isAuthenticated } = useAuth();
  const [post, setPost] = useState<RoommatePost | null>(null); const [loading, setLoading] = useState(true);
  const [error, setError] = useState(''); const [open, setOpen] = useState(false); const [message, setMessage] = useState('');
  const [busy, setBusy] = useState(false); const [formError, setFormError] = useState(''); const [sent, setSent] = useState(false);
  useEffect(() => {
    let active = true;
    void Promise.resolve().then(async () => {
      setLoading(true); setError(''); setSent(false);
      try { const data = await roommateService.detail(Number(id)); if (active) setPost(data); }
      catch (e) { if (active) setError(getApiErrorMessage(e, 'Không tải được tin ở ghép.')); }
      finally { if (active) setLoading(false); }
    });
    return () => { active = false; };
  }, [id]);
  if (loading) return <PageState type="loading" message="Đang tải tin ở ghép..." />;
  if (error || !post) return <main className="roommates-page"><Link to="/roommates">Quay lại tìm bạn ở ghép</Link><PageState type="error" message={error || 'Không tìm thấy tin.'} /></main>;
  return <main className="roommates-page rm-public-detail">
    <nav className="rm-breadcrumb" aria-label="Đường dẫn"><Link to="/">Trang chủ</Link><FiChevronRight /><Link to="/roommates">Ở ghép</Link><FiChevronRight /><span aria-current="page">{post.roomName}</span></nav>
    <Link className="rm-back" to="/roommates"><FiArrowLeft />Tất cả tin ở ghép</Link>
    <div className="rm-detail-layout"><article className="rm-detail-main">
      <div className={`rm-gallery ${post.imageUrls.length <= 1 ? 'rm-gallery-single' : ''}`}>{(post.imageUrls.length ? post.imageUrls : ['/room-placeholder.svg']).map((url, i) => <img key={url + i} src={url} alt={`${post.roomName} - ảnh ${i + 1}`} onError={e => { e.currentTarget.onerror = null; e.currentTarget.src = '/room-placeholder.svg'; }} />)}</div>
      <header className="rm-detail-heading"><StatusBadge label="Đang tìm người" tone="success" /><h1>{post.title}</h1><p className="rm-location"><FiMapPin /><span>{[post.address, post.district, post.province].filter(Boolean).join(', ')}</span></p><p className="rm-date"><FiCalendar />Đăng ngày {new Date(post.createdAt).toLocaleDateString('vi-VN')}</p></header>
      <dl className="rm-detail-facts"><div><dt><FiMaximize2 />Diện tích</dt><dd>{post.area} m²</dd></div><div><dt><FiUsers />Đang cư trú</dt><dd>{post.currentOccupants}/{post.maxOccupants} người</dd></div><div><dt><FiUsers />Cần tìm thêm</dt><dd>{Math.min(post.availableSlots, post.remainingRequestedSlots)} người</dd></div></dl>
      <section className="rm-detail-section"><h2>Thông tin ở ghép</h2><p className="rm-text">{post.description}</p></section>
      <section className="rm-detail-section"><h2>Yêu cầu người ở cùng</h2><p className="rm-text">{post.requirements || 'Trao đổi với người thuê chính.'}</p></section>
      <section className="rm-detail-section"><h2>Tiện ích</h2>{post.amenities.length ? <ul className="rm-detail-amenities">{post.amenities.map(a => <li key={a}><FiCheck />{a}</li>)}</ul> : <p className="rm-muted">Chưa có thông tin tiện ích.</p>}</section>
      <section className="rm-detail-section"><h2>Địa chỉ</h2><p className="rm-location"><FiMapPin /><span>{[post.address, post.district, post.province].filter(Boolean).join(', ')}</span></p></section>
    </article>
      <aside className="rm-contact"><div className="rm-contact-person"><span className="rm-avatar" aria-hidden="true">{post.creatorName.charAt(0).toUpperCase()}{post.creatorAvatar && <img src={post.creatorAvatar} alt="" onError={e => { e.currentTarget.style.display = 'none'; }} />}</span><div><small>Người thuê chính</small><h2>{post.creatorName}</h2></div></div>
        <div className="rm-contact-price"><small>Chi phí dự kiến</small><strong>{post.estimatedShare != null ? formatPrice(post.estimatedShare) : 'Thỏa thuận'}</strong>{post.estimatedShare != null && <span>/người/tháng</span>}</div>
        <dl className="rm-contact-facts"><div><dt>Phòng</dt><dd>{post.roomName}</dd></div><div><dt>Đang ở</dt><dd>{post.currentOccupants}/{post.maxOccupants} người</dd></div><div><dt>Còn tìm</dt><dd className="rm-available">{Math.min(post.availableSlots, post.remainingRequestedSlots)} người</dd></div></dl>
        {!isAuthenticated ? <Link className="rm-button primary" to="/login">Đăng nhập để gửi yêu cầu</Link> : <button className="rm-button primary" disabled={!isTenant || sent} onClick={() => { setFormError(''); setOpen(true); }}><FiSend />{sent ? 'Đã gửi yêu cầu' : 'Gửi yêu cầu ở ghép'}</button>}
        <p className="rm-muted rm-contact-note">Chi phí dự kiến để tham khảo. Hợp đồng và hóa đơn vẫn thuộc người thuê chính.</p>
      </aside></div>
    <Modal isOpen={open} onClose={() => { if (!busy) setOpen(false); }} title="Gửi yêu cầu ở ghép" bodyClassName="rm-modal-content"><form className="rm-form" onSubmit={async e => { e.preventDefault(); if (busy) return; setBusy(true); setFormError(''); try { await roommateService.apply(post.id, message.trim()); setSent(true); setOpen(false); toast.success('Đã gửi yêu cầu tới người thuê chính.'); } catch (ex) { setFormError(getApiErrorMessage(ex, 'Không gửi được yêu cầu.')); } finally { setBusy(false); } }}>
      <label>Lời nhắn<textarea aria-label="Lời nhắn" maxLength={1000} rows={5} value={message} onChange={e => setMessage(e.target.value)} /></label>{formError && <p role="alert" className="rm-error">{formError}</p>}<div className="rm-actions"><button type="button" className="rm-button" disabled={busy} onClick={() => setOpen(false)}>Hủy</button><button className="rm-button primary" disabled={busy}><FiSend />{busy ? 'Đang gửi...' : 'Gửi yêu cầu'}</button></div></form></Modal>
  </main>;
}
