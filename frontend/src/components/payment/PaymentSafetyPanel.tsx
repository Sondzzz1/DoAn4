import { useCallback, useEffect, useState, type FormEvent } from 'react';
import { FiCheck, FiCreditCard, FiFileText, FiRefreshCw, FiX, FiChevronLeft, FiChevronRight } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { AdminContractClosureLookup } from '../rental/ContractClosurePanel';
import { paymentSafetyService, type RefundItem, type PaymentEventItem } from '../../services/paymentSafetyService';
import type { DepositDto, ContractDto } from '../../services/rentalService';
import { getApiErrorMessage } from '../../utils/apiError';
import { formatPrice } from '../../utils/helpers';
import './PaymentSafety.css';

type Props = { role: 'tenant' | 'landlord' | 'admin'; deposits?: DepositDto[]; contracts?: ContractDto[]; onChanged?: () => Promise<void> };
type Action = { kind: 'create'; depositId: number; title: string } | { kind: 'resolve'; item: PaymentEventItem; title: string }
  | { kind: 'approve' | 'reject' | 'report' | 'confirm' | 'withdraw'; item: RefundItem; title: string };
const refundLabels = ['Chờ chủ trọ duyệt', 'Đã duyệt hoàn', 'Chờ xác nhận nhận tiền', 'Đã hoàn cọc', 'Đã từ chối', 'Đã rút yêu cầu'];
const eventLabels = ['Đã ghi nhận', 'Đã áp dụng', 'Cần đối soát', 'Đã đối soát'];
const date = (value: string) => new Date(value.endsWith('Z') ? value : value + 'Z').toLocaleString('vi-VN');

export default function PaymentSafetyPanel({ role, deposits = [], contracts = [], onChanged }: Props) {
  const [refunds, setRefunds] = useState<RefundItem[]>([]);
  const [events, setEvents] = useState<PaymentEventItem[]>([]);
  const [page, setPage] = useState(1);
  const [view, setView] = useState<'refunds' | 'events'>(role === 'admin' ? 'events' : 'refunds');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [action, setAction] = useState<Action | null>(null);
  const [note, setNote] = useState('');
  const [reference, setReference] = useState('');
  const [formError, setFormError] = useState('');
  const [busy, setBusy] = useState(false);
  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { const [r, e] = await Promise.all([paymentSafetyService.refunds(), paymentSafetyService.events(page)]); setRefunds(r); setEvents(e); }
    catch (e) { setError(getApiErrorMessage(e, 'Không thể tải hoàn cọc và đối soát.')); }
    finally { setLoading(false); }
  }, [page]);
  useEffect(() => { void Promise.resolve().then(load); }, [load]);
  function open(next: Action) { setNote(''); setReference(''); setFormError(''); setAction(next); }
  async function submit(e: FormEvent) {
    e.preventDefault(); if (!action || busy) return; setBusy(true); setFormError('');
    try {
      if (action.kind === 'create') await paymentSafetyService.createRefund(action.depositId, note.trim());
      else if (action.kind === 'resolve') await paymentSafetyService.resolve(action.item, note.trim());
      else await paymentSafetyService.act(action.item, action.kind, note.trim(), reference.trim());
      setAction(null); toast.success('Đã cập nhật yêu cầu.'); await load(); await onChanged?.();
    } catch (e) { setFormError(getApiErrorMessage(e, 'Không thể thực hiện thao tác.')); }
    finally { setBusy(false); }
  }
  const needsNote = action?.kind === 'create' || action?.kind === 'reject' || action?.kind === 'resolve' || (action?.kind === 'confirm' && role === 'admin');
  const eligible = deposits.filter(d => [1, 2].includes(d.trangThai) && !contracts.some(c => c.yeuCauThueId === d.yeuCauThueId));
  return <section className="payment-safety" aria-label="Hoàn cọc và đối soát">
    <div className="ps-heading"><h2><FiCreditCard /> Hoàn cọc và đối soát</h2><button type="button" onClick={() => void load()} title="Tải lại" aria-label="Tải lại hoàn cọc" disabled={loading}><FiRefreshCw /></button></div>
    <div className="ps-tabs" role="tablist" aria-label="Theo dõi tiền cọc"><button type="button" role="tab" aria-selected={view === 'refunds'} onClick={() => setView('refunds')}>Yêu cầu hoàn cọc</button><button type="button" role="tab" aria-selected={view === 'events'} onClick={() => setView('events')}>Sự kiện thanh toán</button></div>
    {loading ? <p role="status">Đang tải...</p> : error ? <p role="alert" className="ps-error">{error}</p> : view === 'refunds' ? <>
      {role === 'tenant' && eligible.length > 0 && <div className="ps-eligible">{eligible.map(d => <button type="button" key={d.id} onClick={() => open({ kind: 'create', depositId: d.id, title: `Đề nghị hoàn cọc #${d.id}` })}><FiCreditCard /> Yêu cầu hoàn cọc #{d.id} · {formatPrice(d.soTien)}</button>)}</div>}
      {!refunds.length && <p className="ps-empty">Chưa có yêu cầu hoàn cọc.</p>}
      <div className="ps-list">{refunds.map(r => <article className="ps-item" key={r.id}>
        <div className="ps-item-title"><h3>#{r.id} · {r.roomName}</h3><span className={r.status === 3 ? 'ps-badge done' : 'ps-badge'}>{refundLabels[r.status] || 'Không rõ'}</span></div>
        <strong>{formatPrice(r.amount)}</strong><p>{r.reason}</p><small>{date(r.requestedAtUtc)} · Cọc #{r.depositId}</small>
        {r.decisionNote && <p>Phản hồi: {r.decisionNote}</p>}{r.refundReference && <p>Chứng từ: <b>{r.refundReference}</b></p>}
        <div className="ps-actions">
          {role === 'landlord' && r.status === 0 && <><button onClick={() => open({ kind: 'approve', item: r, title: `Duyệt hoàn cọc #${r.id}` })}><FiCheck /> Duyệt hoàn</button><button onClick={() => open({ kind: 'reject', item: r, title: `Từ chối hoàn cọc #${r.id}` })}><FiX /> Từ chối</button></>}
          {role === 'landlord' && r.status === 1 && <button onClick={() => open({ kind: 'report', item: r, title: `Báo đã hoàn tiền #${r.id}` })}><FiFileText /> Báo đã hoàn tiền</button>}
          {role === 'tenant' && [0, 1].includes(r.status) && <button onClick={() => open({ kind: 'withdraw', item: r, title: `Rút yêu cầu hoàn #${r.id}` })}><FiX /> Rút yêu cầu</button>}
          {(role === 'tenant' || role === 'admin') && r.status === 2 && <button onClick={() => open({ kind: 'confirm', item: r, title: role === 'admin' ? `Xác minh hoàn tiền #${r.id}` : `Xác nhận đã nhận tiền #${r.id}` })}><FiCheck /> {role === 'admin' ? 'Xác minh bằng chứng' : 'Đã nhận đủ tiền'}</button>}
        </div>
      </article>)}</div>
    </> : <>
      {!events.length && <p className="ps-empty">Chưa có sự kiện thanh toán ở trang này.</p>}
      <div className="ps-list">{events.map(item => <article className="ps-item" key={item.id}>
        <div className="ps-item-title"><h3>{item.orderId}</h3><span className={item.processingState === 2 ? 'ps-badge warning' : 'ps-badge'}>{eventLabels[item.processingState]}</span></div>
        <strong>{formatPrice(item.reportedAmount)}</strong><p>{item.source === 0 ? 'IPN' : 'Trả về trình duyệt'} · Provider: {item.responseCode}/{item.providerStatus}</p>
        <small>{date(item.receivedAtUtc)}</small>{item.reason && <p>{item.reason}</p>}{item.resolutionNote && <p>Kết quả đối soát: {item.resolutionNote}</p>}
        {role === 'admin' && item.processingState === 2 && <div className="ps-actions"><button onClick={() => open({ kind: 'resolve', item, title: `Đối soát sự kiện #${item.id}` })}><FiCheck /> Ghi kết quả đối soát</button></div>}
      </article>)}</div>
      <div className="ps-pagination"><button aria-label="Trang trước" title="Trang trước" disabled={page === 1} onClick={() => setPage(p => p - 1)}><FiChevronLeft /></button><span>Trang {page}</span><button aria-label="Trang sau" title="Trang sau" disabled={events.length < 30} onClick={() => setPage(p => p + 1)}><FiChevronRight /></button></div>
    </>}
    <Modal isOpen={!!action} onClose={() => { if (!busy) setAction(null); }} title={action?.title}>
      <form onSubmit={submit} className="ps-form">
        {action?.kind === 'confirm' && <p>{role === 'admin' ? 'Xác minh chứng từ và người nhận trước khi hoàn tất.' : 'Chỉ xác nhận sau khi bạn đã nhận đủ số tiền hoàn cọc.'}</p>}
        {action?.kind === 'approve' && <p>Duyệt hoàn đủ tiền cọc. Phòng tiếp tục được giữ cho đến khi xác nhận nhận tiền.</p>}
        {action?.kind === 'withdraw' && <p>Rút đề nghị hoàn và giữ nguyên quyền giữ chỗ hiện tại?</p>}
        {needsNote && <label>{action?.kind === 'create' ? 'Lý do hoàn cọc' : action?.kind === 'reject' ? 'Lý do từ chối' : 'Kết quả và bằng chứng'}<textarea aria-label="Nội dung xử lý" value={note} onChange={e => setNote(e.target.value)} required minLength={action?.kind === 'create' || action?.kind === 'reject' ? 5 : 10} maxLength={action?.kind === 'create' ? 500 : 1000} rows={4} disabled={busy} /></label>}
        {action?.kind === 'report' && <label>Chứng từ / mã chuyển tiền<input aria-label="Chứng từ hoàn tiền" value={reference} onChange={e => setReference(e.target.value)} required minLength={3} maxLength={200} disabled={busy} /></label>}
        {formError && <p role="alert" className="ps-error">{formError}</p>}
        <div className="ps-actions"><button type="button" onClick={() => setAction(null)} disabled={busy}>Hủy</button><button type="submit" disabled={busy}><FiCheck />{busy ? 'Đang xử lý...' : 'Xác nhận'}</button></div>
      </form>
    </Modal>
    {role === 'admin' && <AdminContractClosureLookup />}
  </section>;
}
