import { useState, type FormEvent } from 'react';
import { FiCheck, FiDollarSign, FiFileText, FiRefreshCw } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { cashPaymentService, CASH_PAYMENT_STATUS, type CashPaymentDto } from '../../services/cashPaymentService';
import { formatPrice } from '../../utils/helpers';
import { getApiErrorMessage } from '../../utils/apiError';
import './CashPayments.css';

interface Props {
  role: 'tenant' | 'landlord';
  items: CashPaymentDto[];
  onChanged: () => Promise<void>;
}

const utcDate = (value: string) => new Date(/(?:Z|[+-]\d\d:\d\d)$/.test(value) ? value : `${value}Z`).toLocaleString('vi-VN');

export default function CashPaymentPanel({ role, items, onChanged }: Props) {
  const [action, setAction] = useState<{ kind: 'confirm' | 'cancel'; item: CashPaymentDto } | null>(null);
  const [note, setNote] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [showHistory, setShowHistory] = useState(false);
  const visible = items.filter(item => showHistory || item.status !== CASH_PAYMENT_STATUS.CANCELLED);

  function open(kind: 'confirm' | 'cancel', item: CashPaymentDto) {
    setAction({ kind, item }); setNote(''); setAcknowledged(false); setError('');
  }

  async function submit(e: FormEvent) {
    e.preventDefault();
    if (!action || busy || (action.kind === 'confirm' && !acknowledged)) return;
    setBusy(true); setError('');
    try {
      if (action.kind === 'confirm') await cashPaymentService.confirm(action.item.id, note.trim() || undefined);
      else await cashPaymentService.cancel(action.item.id);
      setAction(null);
      toast.success(action.kind === 'confirm' ? 'Đã xác nhận nhận tiền mặt và phát hành phiếu thu.' : 'Đã hủy lựa chọn tiền mặt.');
      await onChanged();
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể cập nhật thanh toán tiền mặt.')); }
    finally { setBusy(false); }
  }

  return <section className="cash-payment-panel" aria-label="Thanh toán tiền mặt và phiếu thu">
    <div className="cash-payment-heading"><div><h2><FiDollarSign /> Tiền mặt và phiếu thu</h2><p>{role === 'landlord' ? 'Xác nhận khoản tiền đã nhận trực tiếp từ người thuê.' : 'Theo dõi lựa chọn tiền mặt và phiếu thu do chủ trọ xác nhận.'}</p></div><button type="button" className="cash-payment-secondary" aria-label="Tải lại phiếu thu" onClick={() => void onChanged()}><FiRefreshCw /> Tải lại</button></div>
    <label className="cash-payment-history"><input type="checkbox" checked={showHistory} onChange={e => setShowHistory(e.target.checked)} /> Hiện cả lựa chọn đã hủy</label>
    {!visible.length ? <p className="cash-payment-empty">Chưa có {showHistory ? 'lựa chọn thanh toán tiền mặt' : 'khoản tiền mặt hoặc phiếu thu'}.{role === 'landlord' && ' Người thuê chọn “Tiền mặt trực tiếp” tại khoản cọc hoặc hóa đơn trước khi bạn xác nhận thu tiền.'}</p> : <div className="cash-payment-list">{visible.map(item => <article className="cash-payment-item" key={item.id}>
      <div className="cash-payment-item-title"><div><small>{item.depositId ? `Tiền cọc #${item.depositId}` : `Hóa đơn #${item.monthlyBillId}`}</small><h3>{item.targetTitle}</h3></div><span className={`cash-payment-badge ${item.status === CASH_PAYMENT_STATUS.RECEIVED ? 'received' : ''}`}>{item.status === CASH_PAYMENT_STATUS.RECEIVED ? 'Đã thu tiền mặt' : item.status === CASH_PAYMENT_STATUS.CANCELLED ? 'Đã hủy lựa chọn' : 'Đã chọn tiền mặt · Chưa thanh toán'}</span></div>
      <strong className="cash-payment-amount">{formatPrice(item.amount)}</strong>
      <p>{role === 'landlord' ? `Người thuê: ${item.tenantName || `#${item.tenantAccountId}`}` : `Chủ trọ: ${item.landlordName || `#${item.landlordAccountId}`}${item.landlordPhone ? ` · ${item.landlordPhone}` : ''}`}</p>
      <small>Chọn tiền mặt: {utcDate(item.selectedAtUtc)}</small>
      {item.targetDueAt && item.status === CASH_PAYMENT_STATUS.SELECTED && <p>Hạn thanh toán: {new Date(item.targetDueAt).toLocaleString('vi-VN')}</p>}
      {item.status === CASH_PAYMENT_STATUS.RECEIVED && <div className="cash-payment-receipt"><span><FiFileText /> Phiếu thu: <b>{item.receiptNumber || `TM-${item.id}`}</b></span>{item.confirmedAtUtc && <span>Đã xác nhận nhận đủ tiền: {utcDate(item.confirmedAtUtc)}</span>}<span>Phương thức: Tiền mặt trực tiếp</span>{item.note && <p>Ghi chú: {item.note}</p>}</div>}
      {item.status === CASH_PAYMENT_STATUS.SELECTED && <>
        {item.unavailableReason && <p className="cash-payment-error">{item.unavailableReason}</p>}
        {role === 'landlord' ? <button type="button" className="cash-payment-primary" disabled={item.canConfirm === false} onClick={() => open('confirm', item)}><FiCheck /> Xác nhận đã nhận tiền mặt</button> : <div className="cash-payment-actions"><p>Giao tiền cho chủ trọ trước hạn. Khoản thu vẫn chưa được thanh toán cho đến khi chủ trọ xác nhận.</p><button type="button" className="cash-payment-secondary" onClick={() => open('cancel', item)}>Hủy lựa chọn tiền mặt</button></div>}
      </>}
    </article>)}</div>}
    <Modal isOpen={!!action} onClose={() => { if (!busy) setAction(null); }} title={action?.kind === 'confirm' ? 'Xác nhận đã nhận tiền mặt' : 'Hủy lựa chọn tiền mặt'}>
      {action && <form className="cash-payment-dialog" onSubmit={submit}>
        <div className="cash-payment-summary"><p>{action.item.targetTitle}</p><strong>{formatPrice(action.item.amount)}</strong><span>Người thuê: {action.item.tenantName || `#${action.item.tenantAccountId}`}</span></div>
        {action.kind === 'confirm' ? <>
          <p>Phiếu thu sẽ ghi nhận bạn đã nhận đủ số tiền trên bằng tiền mặt.{action.item.depositId ? ' Khoản cọc sẽ được xác nhận để tiếp tục lập hợp đồng.' : ' Hóa đơn sẽ chuyển sang đã thanh toán.'}</p>
          <label className="cash-payment-note">Ghi chú phiếu thu<textarea value={note} onChange={e => setNote(e.target.value)} maxLength={500} rows={3} placeholder="Nội dung thu tiền (nếu có)" disabled={busy} /></label>
          <label className="cash-payment-acknowledge"><input type="checkbox" checked={acknowledged} onChange={e => setAcknowledged(e.target.checked)} required disabled={busy} /><span>Tôi đã thực nhận đủ {formatPrice(action.item.amount)} tiền mặt từ người thuê.</span></label>
        </> : <p>Chỉ hủy lựa chọn khi bạn chưa giao tiền cho chủ trọ. Việc này không hủy khoản cọc/hóa đơn và không thay đổi hạn thanh toán.</p>}
        {error && <p role="alert" className="cash-payment-error">{error}</p>}
        <div className="cash-payment-actions"><button type="button" className="cash-payment-secondary" onClick={() => setAction(null)} disabled={busy}>Đóng</button><button type="submit" className="cash-payment-primary" disabled={busy || (action.kind === 'confirm' && !acknowledged)}>{busy ? 'Đang xử lý...' : action.kind === 'confirm' ? 'Xác nhận và tạo phiếu thu' : 'Hủy lựa chọn'}</button></div>
      </form>}
    </Modal>
  </section>;
}
