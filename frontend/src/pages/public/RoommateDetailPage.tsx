import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { FiArrowLeft, FiSend, FiUsers } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { useAuth } from '../../hooks/useAuth';
import Modal from '../../components/common/Modal';
import PageState from '../../components/common/PageState';
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
  return <main className="roommates-page"><Link className="rm-back" to="/roommates"><FiArrowLeft />Tất cả tin ở ghép</Link><h1>{post.title}</h1>
    <div className="rm-detail-layout"><article><div className="rm-gallery">{post.imageUrls.map((url, i) => <img key={url + i} src={url} alt={`${post.roomName} - ảnh ${i + 1}`} />)}</div><h2>Thông tin ở ghép</h2><p className="rm-text">{post.description}</p><h2>Yêu cầu người ở cùng</h2><p className="rm-text">{post.requirements || 'Trao đổi với người thuê chính.'}</p><h2>Tiện ích</h2><div className="rm-amenities">{post.amenities.map(a => <span key={a}>{a}</span>)}</div><h2>Địa chỉ</h2><p>{[post.address, post.district, post.province].filter(Boolean).join(', ')}</p></article>
      <aside className="rm-contact"><h2>{post.creatorName}</h2><p>{post.roomName} · {post.area} m²</p><p><FiUsers />Đang ở {post.currentOccupants}/{post.maxOccupants} người</p><p>Còn tìm {Math.min(post.availableSlots, post.remainingRequestedSlots)} người</p><strong>{post.estimatedShare != null ? formatPrice(post.estimatedShare) + '/người/tháng' : 'Chi phí theo thỏa thuận'}</strong><p className="rm-muted">Chi phí dự kiến để tham khảo. Hợp đồng và hóa đơn vẫn thuộc người thuê chính.</p>
        {!isAuthenticated ? <Link className="rm-button primary" to="/login">Đăng nhập để gửi yêu cầu</Link> : <button className="rm-button primary" disabled={!isTenant || sent} onClick={() => { setFormError(''); setOpen(true); }}><FiSend />{sent ? 'Đã gửi yêu cầu' : 'Gửi yêu cầu ở ghép'}</button>}
      </aside></div>
    <Modal isOpen={open} onClose={() => { if (!busy) setOpen(false); }} title="Gửi yêu cầu ở ghép"><form className="rm-form" onSubmit={async e => { e.preventDefault(); if (busy) return; setBusy(true); setFormError(''); try { await roommateService.apply(post.id, message.trim()); setSent(true); setOpen(false); toast.success('Đã gửi yêu cầu tới người thuê chính.'); } catch (ex) { setFormError(getApiErrorMessage(ex, 'Không gửi được yêu cầu.')); } finally { setBusy(false); } }}>
      <label>Lời nhắn<textarea aria-label="Lời nhắn" maxLength={1000} rows={5} value={message} onChange={e => setMessage(e.target.value)} /></label>{formError && <p role="alert" className="rm-error">{formError}</p>}<div className="rm-actions"><button type="button" className="rm-button" disabled={busy} onClick={() => setOpen(false)}>Hủy</button><button className="rm-button primary" disabled={busy}><FiSend />{busy ? 'Đang gửi...' : 'Gửi yêu cầu'}</button></div></form></Modal>
  </main>;
}
