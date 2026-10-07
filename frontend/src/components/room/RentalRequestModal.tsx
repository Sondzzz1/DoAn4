import React, { useState } from 'react';
import { FiSend } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { rentalService } from '../../services/rentalService';
import { formatPrice } from '../../utils/helpers';
import { getApiErrorMessage } from '../../utils/apiError';
import Modal from '../common/Modal';

interface RentalRequestModalProps { isOpen: boolean; onClose: () => void; postId: number; postTitle: string; price: number; address: string; }

const RentalRequestModal: React.FC<RentalRequestModalProps> = ({ isOpen, onClose, postId, postTitle, price, address }) => {
  const [note, setNote] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (loading) return;
    setLoading(true); setError('');
    try {
      await rentalService.createRentalRequest({ baiDangId: postId, ghiChu: note.trim() || undefined });
      toast.success('Đã gửi yêu cầu thuê phòng.');
      onClose(); setNote('');
    } catch (error) { setError(getApiErrorMessage(error, 'Không thể gửi yêu cầu thuê phòng.')); }
    finally { setLoading(false); }
  };
  return <Modal isOpen={isOpen} onClose={() => { if (!loading) onClose(); }} title="Gửi yêu cầu thuê phòng">
    <div className="mb-5 border-b border-slate-200 pb-4"><h3 className="font-semibold text-slate-900">{postTitle}</h3><p className="mt-1 text-sm text-slate-500">{address}</p><p className="mt-2 font-bold text-[#0084ff]">{formatPrice(price)}/tháng</p></div>
    <form onSubmit={handleSubmit} className="space-y-4">
      <label htmlFor="rental-note" className="block text-sm font-medium text-slate-700">Lời nhắn gửi chủ trọ (tùy chọn)</label>
      <textarea id="rental-note" rows={4} maxLength={1000} value={note} onChange={e => setNote(e.target.value)} placeholder="Ngày dự kiến chuyển vào, số người ở..." className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <p className="text-sm leading-relaxed text-slate-500">Theo dõi phản hồi và khoản đặt cọc trong mục Thuê phòng.</p>
      {error && <p role="alert" className="form-error">{error}</p>}
      <div className="form-actions"><button type="button" disabled={loading} onClick={onClose}>Hủy</button><button type="submit" disabled={loading} className="inline-flex items-center justify-center gap-2 bg-[#0084ff] text-white disabled:opacity-60"><FiSend />{loading ? 'Đang gửi...' : 'Gửi yêu cầu'}</button></div>
    </form>
  </Modal>;
};
export default RentalRequestModal;
