import { useState, type FormEvent } from 'react';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { monthlyBillService, type MonthlyBillDto } from '../../services/monthlyBillService';
import { formatPrice } from '../../utils/helpers';
import { getApiErrorMessage } from '../../utils/apiError';
import './CashPayments.css';

interface Props { bill: MonthlyBillDto; onClose: () => void; onChanged: () => Promise<void> }

export default function CashBillCollectionModal({ bill, onClose, onChanged }: Props) {
  const [note, setNote] = useState('');
  const [acknowledged, setAcknowledged] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  async function submit(e: FormEvent) {
    e.preventDefault(); if (!acknowledged || busy) return;
    setBusy(true); setError('');
    try {
      await monthlyBillService.payBill(bill.id, { expectedAmount: bill.tongTien, phuongThucThanhToan: 'Tiền mặt', ghiChu: note.trim() || undefined });
      toast.success('Đã nhận đủ tiền mặt và phát hành phiếu thu hóa đơn.');
      await onChanged(); onClose();
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể ghi nhận thu tiền mặt.')); }
    finally { setBusy(false); }
  }
  return <Modal isOpen title="Xác nhận thu tiền mặt hóa đơn" onClose={() => { if (!busy) onClose(); }}>
    <form className="cash-payment-dialog" onSubmit={submit}>
      <div className="cash-payment-summary"><p>Hóa đơn tháng {bill.thang}/{bill.nam} · {bill.tenPhong || `#${bill.id}`}</p><strong>{formatPrice(bill.tongTien)}</strong><span>Người thuê: {bill.tenNguoiThue}</span></div>
      <p>Xác nhận sau khi bạn đã nhận đủ tiền trực tiếp từ người thuê. Hóa đơn sẽ được đánh dấu đã thanh toán và lưu phiếu thu tiền mặt.</p>
      <label className="cash-payment-note">Ghi chú phiếu thu<textarea rows={3} value={note} onChange={e => setNote(e.target.value)} maxLength={500} disabled={busy} /></label>
      <label className="cash-payment-acknowledge"><input type="checkbox" checked={acknowledged} onChange={e => setAcknowledged(e.target.checked)} required disabled={busy} /><span>Tôi đã thực nhận đủ {formatPrice(bill.tongTien)} tiền mặt từ người thuê.</span></label>
      {error && <p role="alert" className="cash-payment-error">{error}</p>}
      <div className="cash-payment-actions"><button type="button" className="cash-payment-secondary" disabled={busy} onClick={onClose}>Đóng</button><button type="submit" className="cash-payment-primary" disabled={busy || !acknowledged}>{busy ? 'Đang xử lý...' : 'Xác nhận và tạo phiếu thu'}</button></div>
    </form>
  </Modal>;
}
