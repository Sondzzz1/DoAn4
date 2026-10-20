import { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiPlus, FiEdit2, FiX, FiCheck, FiUsers, FiMessageSquare, FiLogOut, FiExternalLink, FiFileText, FiSend } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../../components/common/Modal';
import PageState from '../../components/common/PageState';
import StatusBadge, { type StatusTone } from '../../components/common/StatusBadge';
import useLandlordAction from '../../components/landlord/useLandlordAction';
import { openDirectChat } from '../../components/chat/chatEvents';
import { roommateService, roommatePostLabels, roommateRequestLabels, type RoommatePost, type RoommateRequest, type RoommateContract, type RoommateDraft } from '../../services/roommateService';
import { getApiErrorMessage } from '../../utils/apiError';
import '../public/Roommates.css';

const postTones: StatusTone[] = ['pending', 'success', 'danger', 'info', 'neutral', 'neutral'];
const requestTones: StatusTone[] = ['pending', 'info', 'success', 'danger', 'neutral'];
const blank: RoommateDraft = { contractId: 0, title: '', description: '', requestedSlots: 1, requirements: '' };
export default function RoommateWorkspacePage({ landlord = false }: { landlord?: boolean }) {
  const [searchParams] = useSearchParams(); const initialContractId = Number(searchParams.get('contractId'));
  const createOnArrival = searchParams.get('create') === '1'; const initialized = useRef(false);
  const [posts, setPosts] = useState<RoommatePost[]>([]); const [contracts, setContracts] = useState<RoommateContract[]>([]);
  const [applications, setApplications] = useState<RoommateRequest[]>([]); const [requests, setRequests] = useState<RoommateRequest[]>([]);
  const [tab, setTab] = useState('posts'); const [loading, setLoading] = useState(true); const [error, setError] = useState('');
  const [selected, setSelected] = useState<RoommatePost | null>(null); const [summary, setSummary] = useState<RoommateContract | null>(null);
  const [requestLoading, setRequestLoading] = useState(false); const [requestError, setRequestError] = useState('');
  const [formOpen, setFormOpen] = useState(false); const [editId, setEditId] = useState<number | null>(null); const [draft, setDraft] = useState<RoommateDraft>(blank);
  const [busy, setBusy] = useState(false); const [formError, setFormError] = useState('');
  const { openAction, dialog } = useLandlordAction();
  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      const p = await roommateService.posts(landlord); setPosts(p);
      if (!landlord) {
        const [c, a] = await Promise.all([roommateService.contracts(), roommateService.applications()]); setContracts(c); setApplications(a);
        if (!initialized.current) {
          initialized.current = true;
          const target = c.find(x => x.contractId === initialContractId && x.canCreatePost);
          if (createOnArrival && target) { setDraft({ ...blank, contractId: target.contractId }); setEditId(null); setFormOpen(true); }
        }
      }
    } catch (e) { setError(getApiErrorMessage(e, 'Không tải được dữ liệu ở ghép.')); }
    finally { setLoading(false); }
  }, [landlord, initialContractId, createOnArrival]);
  useEffect(() => { void Promise.resolve().then(load); }, [load]);
  const loadRequests = async (p: RoommatePost) => {
    setSelected(p); setRequestLoading(true); setRequestError(''); setRequests([]); setSummary(null);
    try { const [r, c, current] = await Promise.all([roommateService.requests(p.id), roommateService.contract(p.contractId!), roommateService.posts(landlord)]); setRequests(r); setSummary(c); setSelected(current.find(x => x.id === p.id) || p); }
    catch (e) { setRequestError(getApiErrorMessage(e, 'Không tải được yêu cầu ở ghép.')); }
    finally { setRequestLoading(false); }
  };
  const act = (title: string, message: string, run: () => Promise<unknown>, after?: () => Promise<void>, reason = false) => {
    const actingPost = selected;
    setSelected(null);
    openAction({ title, message, ...(reason ? { reasonLabel: 'Lý do từ chối' } : {}), run: async input => { await (reason && actingPost ? roommateService.moderate(actingPost.id, false, input) : run()); toast.success('Đã cập nhật.'); await load(); if (after) await after(); } });
  };
  const selectedContract = contracts.find(c => c.contractId === draft.contractId);
  const editPost = (p: RoommatePost) => { setEditId(p.id); setDraft({ contractId: p.contractId!, title: p.title, description: p.description, requestedSlots: p.requestedSlots, estimatedShare: p.estimatedShare ?? undefined, requirements: p.requirements || '' }); setFormError(''); setFormOpen(true); };
  const decide = (r: RoommateRequest, approve: boolean) => act(approve ? landlord ? 'Xác nhận người vào ở' : 'Chấp nhận người ứng tuyển' : 'Từ chối yêu cầu', approve ? landlord ? `Xác nhận ${r.applicantName} vào ở cùng?` : `Chuyển yêu cầu của ${r.applicantName} tới chủ trọ?` : `Từ chối yêu cầu của ${r.applicantName}?`, () => roommateService.decide(r.id, landlord, approve), selected ? () => loadRequests(selected) : undefined);
  return <main className={`roommates-page rm-workspace ${landlord ? 'rm-workspace-landlord' : 'tenant-page rm-workspace-tenant'}`}>
    <header className="roommates-heading">
      <div><p className="rm-eyebrow"><FiUsers />{landlord ? 'QUẢN LÝ' : 'KHU VỰC NGƯỜI THUÊ'}</p><h1>{landlord ? 'Tin và yêu cầu ở ghép' : 'Ở ghép của tôi'}</h1></div>
      <div className="rm-actions"><Link className="rm-button" to="/roommates"><FiExternalLink />Tìm bạn ở ghép</Link>{!landlord && <button className="rm-button primary" disabled={!contracts.some(c => c.canCreatePost)} onClick={() => { setDraft({ ...blank, contractId: contracts.find(c => c.canCreatePost)?.contractId || 0 }); setEditId(null); setFormError(''); setFormOpen(true); }}><FiPlus />Tạo tin ở ghép</button>}</div>
    </header>
    {!loading && !error && <div className="rm-metrics" aria-label="Thống kê tin ở ghép">{[
      ['Tổng tin đăng', posts.length], ['Đang tìm người', posts.filter(p => p.status === 1).length],
      ['Chờ duyệt', posts.filter(p => p.status === 0).length], ['Đã kết thúc', posts.filter(p => [3, 4, 5].includes(p.status)).length],
    ].map(([label, count]) => <div key={label}><span>{label}</span><strong>{count}</strong></div>)}</div>}
    {!landlord && <div className="rm-tabs" role="tablist" aria-label="Quản lý ở ghép">{([
      ['posts', 'Tin của tôi', FiFileText, posts.length], ['applications', 'Yêu cầu đã gửi', FiSend, applications.length], ['occupants', 'Người ở cùng', FiUsers, contracts.reduce((count, c) => count + c.occupants.filter(o => o.status === 0).length, 0)],
    ] as const).map(([key, label, Icon, count]) => <button key={key} role="tab" aria-selected={tab === key} onClick={() => setTab(key)}><Icon />{label}<small>{count}</small></button>)}</div>}
    {loading ? <PageState type="loading" message="Đang tải dữ liệu ở ghép..." /> : error ? <><PageState type="error" message={error} /><button className="rm-button" onClick={() => void load()}>Thử lại</button></> : tab === 'posts' ? !posts.length ? <PageState type="empty" message={landlord ? 'Chưa có tin ở ghép cần xử lý.' : 'Bạn chưa có tin tìm người ở ghép. Tạo tin từ hợp đồng đang hiệu lực còn chỗ.'} /> : <div className="rm-table-wrap"><table className="rm-table"><thead><tr><th>Tin tìm người</th><th>Người thuê chính</th><th>Sức chứa</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>{posts.map(p => <tr key={p.id}><td><strong>{p.title}</strong><small>{p.roomName} · Hợp đồng #{p.contractId}</small>{p.rejectionReason && <small className="rm-error">{p.rejectionReason}</small>}</td><td>{p.creatorName}</td><td>{p.currentOccupants}/{p.maxOccupants}<small>Cần tìm: {p.requestedSlots}</small></td><td><StatusBadge label={roommatePostLabels[p.status]} tone={postTones[p.status]} /></td><td><div className="rm-row-actions"><button className="rm-button" onClick={() => void loadRequests(p)}><FiUsers />Chi tiết / Yêu cầu</button>{!landlord && [0, 1, 2].includes(p.status) && <button title="Sửa tin" aria-label={`Sửa tin ${p.id}`} className="rm-button" onClick={() => editPost(p)}><FiEdit2 /></button>}{!landlord && ![4, 5].includes(p.status) && <button title="Đóng tin" aria-label={`Đóng tin ${p.id}`} className="rm-button danger" onClick={() => act('Đóng tin ở ghép', 'Các yêu cầu chưa hoàn tất sẽ bị hủy.', () => roommateService.close(p.id))}><FiX /></button>}</div></td></tr>)}</tbody></table></div> : tab === 'applications' ? !applications.length ? <PageState type="empty" message="Bạn chưa gửi yêu cầu ở ghép." /> : <div className="rm-table-wrap"><table className="rm-table"><thead><tr><th>Tin</th><th>Người thuê chính</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>{applications.map(r => <tr key={r.id}><td>{r.postTitle}<small>{r.message}</small></td><td>{r.creatorName}</td><td><StatusBadge label={roommateRequestLabels[r.status]} tone={requestTones[r.status]} /></td><td><div className="rm-row-actions">{[0, 1, 2].includes(r.status) && <button className="rm-button" title="Nhắn tin" onClick={() => openDirectChat({ partnerId: r.creatorTenantAccountId, partnerName: r.creatorName })}><FiMessageSquare /></button>}{[0, 1].includes(r.status) && <button className="rm-button danger" onClick={() => act('Hủy yêu cầu ở ghép', r.postTitle, () => roommateService.cancel(r.id))}><FiX />Hủy</button>}</div></td></tr>)}</tbody></table></div> : <div className="rm-contract-list">{!contracts.length ? <PageState type="empty" message="Bạn chưa có hợp đồng thuê chính." /> : contracts.map(c => <section className="rm-contract-section" key={c.contractId}><h2>{c.roomName} · Hợp đồng #{c.contractId}</h2><p className="rm-capacity"><FiUsers />Đang cư trú {c.currentOccupants}/{c.maxOccupants} · Còn {c.availableSlots} chỗ</p>{!c.occupants.length && <p className="rm-muted">Chưa có người ở ghép được ghi nhận.</p>}{c.occupants.map(o => <div className="rm-person" key={o.id}><span>{o.tenantName}<small className="rm-muted"> · {o.status === 0 ? 'Đang ở' : 'Đã rời'} · {new Date(o.joinedAt).toLocaleDateString('vi-VN')}</small></span>{o.status === 0 && c.contractStatus === 1 && <button className="rm-button danger" onClick={() => act('Xác nhận rời phòng', o.tenantName, () => roommateService.leave(o.id))}><FiLogOut />Rời phòng</button>}</div>)}</section>)}</div>}
    <Modal isOpen={formOpen} onClose={() => { if (!busy) setFormOpen(false); }} title={editId ? 'Sửa tin tìm người ở ghép' : 'Tạo tin tìm người ở ghép'} size="lg" bodyClassName={`rm-modal-content ${landlord ? 'rm-modal-landlord' : ''}`}><form className="rm-form" onSubmit={async e => { e.preventDefault(); if (busy) return; setBusy(true); setFormError(''); try { if (editId) await roommateService.update(editId, draft); else await roommateService.create(draft); setFormOpen(false); toast.success('Tin đang chờ chủ trọ duyệt.'); await load(); } catch (ex) { setFormError(getApiErrorMessage(ex, 'Không lưu được tin.')); } finally { setBusy(false); } }}>
      <label>Hợp đồng<select aria-label="Hợp đồng ở ghép" required disabled={!!editId} value={draft.contractId || ''} onChange={e => setDraft({ ...draft, contractId: Number(e.target.value), requestedSlots: 1 })}><option value="">Chọn hợp đồng</option>{contracts.filter(c => c.canCreatePost || c.contractId === draft.contractId).map(c => <option key={c.contractId} value={c.contractId}>{c.roomName} · #{c.contractId}</option>)}</select></label>
      {selectedContract && <p className="rm-capacity"><FiUsers />Sức chứa: {selectedContract.maxOccupants} · Hiện ở: {selectedContract.currentOccupants} · Còn: {selectedContract.availableSlots} chỗ</p>}
      <label>Tiêu đề<input aria-label="Tiêu đề ở ghép" required maxLength={200} value={draft.title} onChange={e => setDraft({ ...draft, title: e.target.value })} /></label><label>Mô tả<textarea aria-label="Mô tả ở ghép" required maxLength={4000} rows={4} value={draft.description} onChange={e => setDraft({ ...draft, description: e.target.value })} /></label>
      <div className="rm-form-grid"><label>Số người cần tìm<input aria-label="Số người cần tìm" required type="number" min={1} max={selectedContract ? selectedContract.availableSlots + (editId ? (posts.find(p => p.id === editId)?.requestedSlots || 0) - (posts.find(p => p.id === editId)?.remainingRequestedSlots || 0) : 0) : undefined} value={draft.requestedSlots} onChange={e => setDraft({ ...draft, requestedSlots: Number(e.target.value) })} /></label><label>Chi phí dự kiến/người/tháng<input aria-label="Chi phí dự kiến" type="number" min={0} value={draft.estimatedShare ?? ''} onChange={e => setDraft({ ...draft, estimatedShare: e.target.value ? Number(e.target.value) : undefined })} /></label></div>
      <label>Yêu cầu người ở cùng<textarea aria-label="Yêu cầu người ở cùng" maxLength={2000} rows={3} value={draft.requirements} onChange={e => setDraft({ ...draft, requirements: e.target.value })} /></label><p className="rm-muted">Chi phí dự kiến chỉ để tham khảo. Người thuê chính tiếp tục thanh toán theo hợp đồng với chủ trọ.</p>{formError && <p role="alert" className="rm-error">{formError}</p>}<div className="rm-actions"><button type="button" className="rm-button" disabled={busy} onClick={() => setFormOpen(false)}>Hủy</button><button className="rm-button primary" disabled={busy}><FiCheck />{busy ? 'Đang lưu...' : 'Gửi chủ trọ duyệt'}</button></div>
    </form></Modal>
    <Modal isOpen={!!selected} onClose={() => setSelected(null)} title="Chi tiết và yêu cầu ở ghép" size="lg" bodyClassName={`rm-modal-content ${landlord ? 'rm-modal-landlord' : ''}`}>{selected && <><h3>{selected.title}</h3><p className="rm-text">{selected.description}</p><p className="rm-muted">{selected.requirements}</p><StatusBadge label={roommatePostLabels[selected.status]} tone={postTones[selected.status]} />{requestLoading ? <PageState type="loading" message="Đang tải yêu cầu..." /> : requestError ? <PageState type="error" message={requestError} /> : <>
      {summary && <p className="rm-capacity"><FiUsers />Đang ở {summary.currentOccupants}/{summary.maxOccupants} · Còn {summary.availableSlots} chỗ</p>}
      {landlord && selected.status === 0 && <div className="rm-actions"><button className="rm-button primary" onClick={() => act('Duyệt tin ở ghép', selected.title, () => roommateService.moderate(selected.id, true), async () => setSelected(null))}><FiCheck />Duyệt tin</button><button className="rm-button danger" onClick={() => act('Từ chối tin ở ghép', selected.title, async () => {}, async () => setSelected(null), true)}><FiX />Từ chối tin</button></div>}
      {selected.status === 1 && <div className="rm-actions"><Link className="rm-button" to={`/roommates/${selected.id}`}><FiExternalLink />Xem tin công khai</Link></div>}
      <h3>Yêu cầu ứng tuyển</h3>{!requests.length && <p className="rm-muted">Chưa có yêu cầu ứng tuyển.</p>}{requests.map(r => <div className="rm-request" key={r.id}><strong>{r.applicantName}</strong><p>{r.message || 'Không có lời nhắn.'}</p><StatusBadge label={roommateRequestLabels[r.status]} tone={requestTones[r.status]} /><div className="rm-actions">{((landlord && r.status === 1) || (!landlord && r.status === 0)) && selected.status === 1 && <><button className="rm-button primary" onClick={() => decide(r, true)}><FiCheck />{landlord ? 'Xác nhận vào ở' : 'Chấp nhận'}</button><button className="rm-button danger" onClick={() => decide(r, false)}><FiX />Từ chối</button></>}{!landlord && [0, 1, 2].includes(r.status) && <button className="rm-button" onClick={() => { setSelected(null); openDirectChat({ partnerId: r.applicantTenantAccountId, partnerName: r.applicantName }); }}><FiMessageSquare />Nhắn tin</button>}</div></div>)}
      {summary && <><h3>Người ở cùng</h3>{summary.occupants.map(o => <div className="rm-person" key={o.id}><span>{o.tenantName} · {o.status === 0 ? 'Đang ở' : 'Đã rời'}</span>{o.status === 0 && summary.contractStatus === 1 && <button className="rm-button danger" onClick={() => act('Xác nhận rời phòng', o.tenantName, () => roommateService.leave(o.id), () => loadRequests(selected))}><FiLogOut />Rời phòng</button>}</div>)}</>}
    </>}<div className="rm-actions"><button className="rm-button" onClick={() => setSelected(null)}>Đóng</button></div></>}</Modal>
    {dialog}
  </main>;
}
