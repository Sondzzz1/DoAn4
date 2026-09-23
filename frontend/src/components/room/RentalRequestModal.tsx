import React, { useState } from 'react';
import { FiX, FiSend, FiFileText, FiInfo } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { rentalService } from '../../services/rentalService';
import { formatPrice } from '../../utils/helpers';

interface RentalRequestModalProps {
  isOpen: boolean;
  onClose: () => void;
  postId: number;
  postTitle: string;
  price: number;
  address: string;
}

const RentalRequestModal: React.FC<RentalRequestModalProps> = ({
  isOpen,
  onClose,
  postId,
  postTitle,
  price,
  address,
}) => {
  const [note, setNote] = useState('');
  const [loading, setLoading] = useState(false);

  if (!isOpen) return null;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);

    try {
      await rentalService.createRentalRequest({
        baiDangId: postId,
        ghiChu: note.trim() || undefined,
      });
      toast.success('Gửi yêu cầu thuê phòng thành công! Chủ trọ sẽ liên hệ sớm nhất.');
      onClose();
      setNote('');
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể gửi yêu cầu thuê phòng.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/50 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl max-w-lg w-full p-6 shadow-2xl animate-in fade-in zoom-in-95">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-100 pb-4 mb-4">
          <div className="flex items-center gap-2 text-slate-900 font-bold text-lg">
            <FiFileText className="text-[#0084ff]" /> Gửi yêu cầu thuê phòng
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-full bg-slate-100 hover:bg-slate-200 text-slate-500 flex items-center justify-center transition-colors border-none cursor-pointer"
          >
            <FiX size={18} />
          </button>
        </div>

        {/* Room brief */}
        <div className="bg-slate-50 rounded-2xl p-4 border border-slate-100 mb-5">
          <h4 className="font-bold text-slate-900 text-sm mb-1 line-clamp-2">{postTitle}</h4>
          <p className="text-xs text-slate-500 mb-2">{address}</p>
          <div className="text-base font-extrabold text-[#0084ff]">{formatPrice(price)}/tháng</div>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="block text-xs font-bold text-slate-700 mb-1">
              Lời nhắn gửi chủ trọ (Tùy chọn)
            </label>
            <textarea
              rows={4}
              value={note}
              onChange={(e) => setNote(e.target.value)}
              placeholder="Ví dụ: Em muốn thuê từ đầu tháng tới, ở 2 người, có xe máy..."
              className="w-full px-4 py-3 rounded-2xl border border-slate-200 text-sm focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10"
            />
          </div>

          <div className="flex items-start gap-2 text-xs text-slate-500 bg-blue-50/50 p-3 rounded-xl border border-blue-100/50">
            <FiInfo className="text-[#0084ff] shrink-0 mt-0.5" />
            <span>
              Sau khi bạn gửi yêu cầu, chủ trọ sẽ nhận được thông báo và xem xét duyệt để tiến hành làm hợp đồng thuê phòng.
            </span>
          </div>

          <div className="flex items-center justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              className="px-5 py-2.5 rounded-xl text-xs font-semibold text-slate-600 hover:bg-slate-100 border-none cursor-pointer"
            >
              Đóng
            </button>
            <button
              type="submit"
              disabled={loading}
              className="inline-flex items-center gap-2 px-6 py-2.5 bg-[#0084ff] hover:bg-[#0073e6] text-white text-xs font-bold rounded-xl shadow-md shadow-blue-500/20 disabled:opacity-60 cursor-pointer transition-all"
            >
              <FiSend /> {loading ? 'Đang gửi...' : 'Xác nhận gửi yêu cầu'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default RentalRequestModal;
