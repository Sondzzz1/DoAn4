import { useCallback, useEffect, useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router-dom';
import { FiCreditCard, FiExternalLink, FiPlus, FiRefreshCw, FiTrash2 } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { SAVED_CARD_UNAVAILABLE_MESSAGE, savedPaymentMethodService, type SavedPaymentMethodDto, type SavedPaymentMethodsDto } from '../../services/savedPaymentMethodService';
import { getApiErrorMessage } from '../../utils/apiError';
import './SavedPaymentMethods.css';

export default function SavedPaymentMethodsPanel() {
  const [data, setData] = useState<SavedPaymentMethodsDto>({ enabled: false, methods: [] });
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [action, setAction] = useState<'link' | SavedPaymentMethodDto | null>(null);
  const [cardType, setCardType] = useState<'01' | '02'>('01');
  const [busy, setBusy] = useState(false);
  const [actionError, setActionError] = useState('');
  const [searchParams, setSearchParams] = useSearchParams();
  const handledReturn = useRef(false);

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try { setData(await savedPaymentMethodService.list()); }
    catch { setError('Chưa thể tải thẻ đã liên kết. Các phương thức thanh toán khác vẫn sử dụng bình thường.'); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { void Promise.resolve().then(load); }, [load]);
  useEffect(() => {
    const result = searchParams.get('cardLink');
    if (!result || handledReturn.current) return;
    handledReturn.current = true;
    if (result === 'success') toast.success('Đã liên kết thẻ VNPay. Bạn có thể chọn thẻ này khi thanh toán.');
    else if (result === 'cancelled') toast.info('Bạn đã hủy liên kết thẻ.');
    else toast.error('Chưa liên kết được thẻ VNPay. Vui lòng thử lại.');
    const next = new URLSearchParams(searchParams);
    next.delete('cardLink');
    setSearchParams(next, { replace: true });
    void Promise.resolve().then(load);
  }, [searchParams, setSearchParams, load]);

  function open(next: 'link' | SavedPaymentMethodDto) { setAction(next); setActionError(''); setCardType('01'); }
  async function submit(event: FormEvent) {
    event.preventDefault();
    if (!action || busy) return;
    setBusy(true); setActionError('');
    try {
      if (action === 'link') {
        const result = await savedPaymentMethodService.link(cardType);
        window.location.assign(result.redirectUrl);
      } else {
        await savedPaymentMethodService.remove(action.id);
        setAction(null); toast.success('Đã gỡ thẻ khỏi hệ thống.'); await load();
      }
    } catch (e) { setActionError(getApiErrorMessage(e, 'Không thể cập nhật thẻ đã liên kết.')); }
    finally { setBusy(false); }
  }

  return <section className="saved-payment-panel" aria-label="Thẻ VNPay đã liên kết">
    <div className="saved-payment-heading"><div><h2><FiCreditCard /> Thẻ VNPay đã liên kết</h2><p>Chọn lại thẻ đã liên kết cho những lần thanh toán sau.</p></div><div className="saved-payment-actions">
      <button type="button" className="saved-payment-secondary" onClick={() => void load()} disabled={loading || busy} aria-label="Tải lại thẻ VNPay"><FiRefreshCw /> Tải lại</button>
      {data.enabled && !error && <button type="button" className="saved-payment-primary" onClick={() => open('link')} disabled={loading || busy}><FiPlus /> Liên kết thẻ VNPay</button>}
    </div></div>
    {loading ? <p role="status">Đang tải thẻ đã liên kết...</p> : error ? <p role="alert" className="saved-payment-error">{error}</p> : <>
      {!data.enabled && <p className="saved-payment-notice">{SAVED_CARD_UNAVAILABLE_MESSAGE}</p>}
      {data.enabled && !data.methods.length && <p className="saved-payment-empty">Bạn chưa liên kết thẻ VNPay nào.</p>}
      {!!data.methods.length && <div className="saved-payment-list">{data.methods.map(method => <article className="saved-payment-card" key={method.id}>
        <FiCreditCard aria-hidden="true" /><div><strong>{method.bankCode || 'Thẻ VNPay'}</strong><span>{method.maskedCardNumber}</span><small>{method.cardType === '02' ? 'Thẻ quốc tế' : 'Thẻ nội địa'}</small></div>
        <button type="button" className="saved-payment-remove" onClick={() => open(method)} aria-label={`Gỡ thẻ ${method.bankCode} ${method.maskedCardNumber}`} disabled={busy}><FiTrash2 /> Gỡ thẻ</button>
      </article>)}</div>}
      <p className="saved-payment-hint">Bạn nhập và xác thực thông tin thẻ trên VNPay. Lần thanh toán tiếp theo vẫn có thể yêu cầu mã OTP.</p>
    </>}
    <Modal isOpen={!!action} onClose={() => { if (!busy) setAction(null); }} title={action === 'link' ? 'Liên kết thẻ VNPay' : 'Gỡ thẻ khỏi hệ thống'}>
      {action && <form className="saved-payment-form" onSubmit={submit}>
        {action === 'link' ? <>
          <p>Chọn loại thẻ, sau đó tiếp tục trên VNPay để nhập và xác thực thông tin thẻ.</p>
          <label>Loại thẻ<select value={cardType} onChange={event => setCardType(event.target.value as '01' | '02')} disabled={busy}><option value="01">Thẻ nội địa</option><option value="02">Thẻ quốc tế</option></select></label>
          <p className="saved-payment-hint">VNPay có thể tạm thu rồi hoàn khoản tiền xác thực thẻ; bạn kiểm tra thông tin trên VNPay trước khi xác nhận.</p>
        </> : <><strong>{action.bankCode} · {action.maskedCardNumber}</strong><p>Gỡ thẻ này khỏi tài khoản trên hệ thống thuê phòng? Các giao dịch và lịch sử thanh toán đã có vẫn được giữ lại.</p><p className="saved-payment-hint">Thao tác này gỡ phương thức thanh toán khỏi ứng dụng, không đóng tài khoản ngân hàng hoặc thẻ tại VNPay.</p></>}
        {actionError && <p role="alert" className="saved-payment-error">{actionError}</p>}
        <div className="saved-payment-actions"><button type="button" className="saved-payment-secondary" onClick={() => setAction(null)} disabled={busy}>Đóng</button><button type="submit" className="saved-payment-primary" disabled={busy}>{busy ? 'Đang xử lý...' : action === 'link' ? <><FiExternalLink /> Tiếp tục trên VNPay</> : 'Gỡ thẻ khỏi hệ thống'}</button></div>
      </form>}
    </Modal>
  </section>;
}
