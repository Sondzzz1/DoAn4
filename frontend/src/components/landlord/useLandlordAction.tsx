import { useState } from 'react';
import LandlordModal from './LandlordModal';
import { getApiErrorMessage } from '../../utils/apiError';

type Action = {
  title: string;
  message: string;
  run: (reason: string) => Promise<boolean | void>;
  reasonLabel?: string;
};

export default function useLandlordAction() {
  const [action, setAction] = useState<Action | null>(null);
  const [reason, setReason] = useState('');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const openAction = (next: Action) => {
    setReason('');
    setError('');
    setAction(next);
  };
  const close = () => { if (!busy) setAction(null); };
  const dialog = <LandlordModal isOpen={!!action} onClose={close} title={action?.title} size="sm">
    <form onSubmit={async event => {
      event.preventDefault();
      if (!action || busy) return;
      setBusy(true);
      setError('');
      try {
        const result = await action.run(reason);
        if (result !== false) setAction(null);
        else setError('Thao tác chưa thành công. Vui lòng kiểm tra thông báo lỗi và thử lại.');
      } catch (failure) {
        setError(getApiErrorMessage(failure, 'Không thể thực hiện thao tác.'));
      } finally { setBusy(false); }
    }} className="space-y-4">
      <p>{action?.message}</p>
      {action?.reasonLabel && <label className="landlord-field">{action.reasonLabel}
        <textarea aria-label={action.reasonLabel} value={reason} onChange={event => setReason(event.target.value)} rows={4} disabled={busy} />
      </label>}
      {error && <p role="alert" className="form-error">{error}</p>}
      <div className="landlord-modal-actions">
        <button type="button" onClick={close} disabled={busy} className="landlord-button">Hủy</button>
        <button type="submit" disabled={busy} className="landlord-button primary">{busy ? 'Đang xử lý...' : 'Xác nhận'}</button>
      </div>
    </form>
  </LandlordModal>;
  return { openAction, dialog };
}
