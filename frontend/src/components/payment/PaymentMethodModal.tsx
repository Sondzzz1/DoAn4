import { useEffect, useState } from 'react';
import { FiCreditCard, FiDollarSign, FiPhone } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { cashPaymentService, CASH_PAYMENT_STATUS, type CashPaymentDto, type CashPaymentTarget } from '../../services/cashPaymentService';
import { paymentService } from '../../services/paymentService';
import { savedPaymentMethodService, type SavedPaymentMethodsDto } from '../../services/savedPaymentMethodService';
import { formatPrice } from '../../utils/helpers';
import { getApiErrorMessage } from '../../utils/apiError';
import './CashPayments.css';
import './SavedPaymentMethods.css';

export interface PaymentTarget extends CashPaymentTarget {
  amount: number;
  title: string;
  landlordName?: string;
  landlordPhone?: string;
  dueAt?: string;
}

interface Props {
  target: PaymentTarget;
  cashSelection?: CashPaymentDto;
  onClose: () => void;
  onChanged: () => Promise<void>;
}

export default function PaymentMethodModal({ target, cashSelection, onClose, onChanged }: Props) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [savedMethods, setSavedMethods] = useState<SavedPaymentMethodsDto>({ enabled: false, methods: [] });
  const [cardsLoading, setCardsLoading] = useState(true);
  const [cardsError, setCardsError] = useState(false);
  const [currentTime, setCurrentTime] = useState(() => Date.now());
  const selected = cashSelection?.status === CASH_PAYMENT_STATUS.SELECTED;
  const received = cashSelection?.status === CASH_PAYMENT_STATUS.RECEIVED;
  const savedCash = selected || received ? cashSelection : undefined;
  const dueAt = savedCash?.targetDueAt || target.dueAt;
  const expiredDeposit = !!target.depositId && !!dueAt && new Date(dueAt).getTime() <= currentTime;

  useEffect(() => {
    let current = true;
    void savedPaymentMethodService.list()
      .then(data => { if (current) setSavedMethods(data); })
      .catch(() => { if (current) setCardsError(true); })
      .finally(() => { if (current) setCardsLoading(false); });
    return () => { current = false; };
  }, []);

  useEffect(() => {
    if (!target.depositId || !dueAt) return;
    const timer = window.setInterval(() => setCurrentTime(Date.now()), 1000);
    return () => window.clearInterval(timer);
  }, [target.depositId, dueAt]);

  async function choose(method: 'vnpay' | 'cash', savedPaymentMethodId?: number) {
    if (busy || selected || received || expiredDeposit) return;
    setBusy(true); setError('');
    try {
      const ids = { depositId: target.depositId, monthlyBillId: target.monthlyBillId };
      if (method === 'cash') {
        await cashPaymentService.select(ids);
        toast.info('Đã chọn trả tiền mặt. Khoản thu chỉ được thanh toán khi chủ trọ xác nhận đã nhận tiền.');
        await onChanged();
      } else {
        const response = await paymentService.createVnPayUrl({ ...ids, ...(savedPaymentMethodId ? { savedPaymentMethodId } : {}) });
        if (!response.data?.paymentUrl) throw new Error('Không nhận được liên kết thanh toán.');
        window.location.assign(response.data.paymentUrl);
      }
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không thể thực hiện lựa chọn thanh toán.'));
    } finally { setBusy(false); }
  }

  async function cancelSelection() {
    if (!cashSelection || busy) return;
    setBusy(true); setError('');
    try {
      await cashPaymentService.cancel(cashSelection.id);
      await onChanged();
      toast.info('Đã hủy lựa chọn tiền mặt. Bạn có thể chọn lại phương thức thanh toán.');
    } catch (e) { setError(getApiErrorMessage(e, 'Không thể hủy lựa chọn tiền mặt.')); }
    finally { setBusy(false); }
  }

  return <Modal isOpen onClose={() => { if (!busy) onClose(); }} title="Chọn phương thức thanh toán">
    <div className="cash-payment-dialog">
      <div className="cash-payment-summary"><p>{savedCash?.targetTitle || target.title}</p><strong>{formatPrice(savedCash?.amount ?? target.amount)}</strong>
        <span>Chủ trọ: {savedCash?.landlordName || target.landlordName || 'Liên hệ chủ trọ của phòng'}</span>
        {(savedCash?.landlordPhone || target.landlordPhone) && <span><FiPhone /> {savedCash?.landlordPhone || target.landlordPhone}</span>}
        {dueAt && <span>Hạn thanh toán: {new Date(dueAt).toLocaleString('vi-VN')}</span>}
      </div>
      {received ? <p className="cash-payment-success">Đã thu tiền mặt · Phiếu thu {cashSelection?.receiptNumber}</p> : selected ? <div className="cash-payment-selected">
        <strong>Đã chọn tiền mặt · Chưa thanh toán</strong>
        <p>Giao đủ tiền trực tiếp cho chủ trọ. Chủ trọ xác nhận đã nhận tiền thì phiếu thu mới được phát hành.</p>
        <p>Lựa chọn này không gia hạn thanh toán. Chỉ hủy để đổi phương thức khi bạn chưa giao tiền cho chủ trọ.</p>
        <button type="button" disabled={busy} onClick={() => void cancelSelection()} className="cash-payment-secondary">Hủy lựa chọn để đổi phương thức</button>
      </div> : <>
        {cardsLoading ? <p role="status" className="saved-payment-hint">Đang tải thẻ VNPay đã liên kết...</p> : savedMethods.enabled && !cardsError && savedMethods.methods.length > 0 ? <div className="saved-card-options">
          <h3>Thanh toán bằng thẻ đã liên kết</h3>
          {savedMethods.methods.map(card => <button type="button" className="saved-card-option" key={card.id} onClick={() => void choose('vnpay', card.id)} disabled={busy || expiredDeposit} aria-label={`Thanh toán bằng ${card.bankCode} ${card.maskedCardNumber}`}>
            <FiCreditCard /><span><strong>{card.bankCode} · {card.maskedCardNumber}</strong><small>Tiếp tục trên VNPay để xác thực giao dịch, có thể cần OTP.</small></span>
          </button>)}
        </div> : cardsError ? <p className="saved-payment-hint">Chưa thể tải thẻ đã liên kết. Bạn vẫn có thể chọn các phương thức bên dưới.</p> : null}
        <button type="button" className="payment-method-option" onClick={() => void choose('vnpay')} disabled={busy || expiredDeposit}>
          <FiCreditCard /><span><strong>Thanh toán VNPay</strong><small>Thanh toán và xác thực trên cổng VNPay.</small></span>
        </button>
        <button type="button" className="payment-method-option" onClick={() => void choose('cash')} disabled={busy || expiredDeposit}>
          <FiDollarSign /><span><strong>Tiền mặt trực tiếp</strong><small>Giao tiền cho chủ trọ và nhận phiếu thu sau khi chủ trọ xác nhận.</small></span>
        </button>
        <p className="cash-payment-hint">Chọn tiền mặt chưa có nghĩa là đã thanh toán và không thay đổi hạn thanh toán.</p>
      </>}
      {expiredDeposit && !received && <p role="alert" className="cash-payment-error">Khoản cọc đã hết hạn thanh toán. Vui lòng liên hệ chủ trọ.</p>}
      {error && <p role="alert" className="cash-payment-error">{error}</p>}
      {busy && <p role="status">Đang xử lý...</p>}
    </div>
  </Modal>;
}
