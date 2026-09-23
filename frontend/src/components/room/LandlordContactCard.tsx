import React from 'react';
import { Post } from '../../types/post.types';
import { FiPhone, FiMessageSquare, FiCalendar, FiAlertCircle, FiUser, FiSend, FiHeart } from 'react-icons/fi';

interface LandlordContactCardProps {
  post: Post;
  isFavorite?: boolean;
  onBookViewing: () => void;
  onSendMessage: () => void;
  onRequestRental?: () => void;
  onToggleFavorite?: () => void;
}

const LandlordContactCard: React.FC<LandlordContactCardProps> = ({
  post,
  isFavorite = false,
  onBookViewing,
  onSendMessage,
  onRequestRental,
  onToggleFavorite,
}) => {
  const handleCallPhone = () => {
    window.location.href = `tel:${post.landlordPhone}`;
  };

  return (
    <div className="sticky top-[90px] space-y-4">
      {/* Main Contact Card */}
      <div className="bg-white rounded-3xl p-6 shadow-sm border border-slate-200/80">
        <h3 className="text-lg font-bold text-slate-900 mb-4">Thông tin liên hệ</h3>

        {/* Landlord Info */}
        <div className="flex items-center gap-3.5 mb-5 pb-5 border-b border-slate-100">
          <div className="w-14 h-14 rounded-2xl bg-gradient-to-tr from-[#0084ff] to-cyan-400 flex items-center justify-center flex-shrink-0 text-white font-bold text-xl shadow-md shadow-blue-500/20">
            {post.landlordName ? post.landlordName.charAt(0).toUpperCase() : 'C'}
          </div>
          <div>
            <p className="text-base font-bold text-slate-900 mb-0.5">{post.landlordName || 'Chủ phòng trọ'}</p>
            <p className="text-xs text-slate-500 flex items-center gap-1">
              <FiUser className="text-[#0084ff]" />
              Chủ trọ đã xác thực
            </p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="space-y-3">
          {/* Phone Button */}
          <button
            onClick={handleCallPhone}
            className="w-full flex items-center justify-center gap-2 px-5 h-[46px] rounded-2xl bg-[#00a651] text-white text-sm font-bold hover:bg-[#008f45] transition-all shadow-md shadow-emerald-500/20 cursor-pointer border-none"
          >
            <FiPhone className="w-4 h-4" />
            {post.landlordPhone || 'Liên hệ chủ trọ'}
          </button>

          {/* Chat with Landlord Button */}
          <button
            onClick={onSendMessage}
            className="w-full flex items-center justify-center gap-2 px-5 h-[46px] rounded-2xl bg-gradient-to-r from-blue-600 to-indigo-600 text-white text-sm font-bold hover:from-blue-700 hover:to-indigo-700 transition-all shadow-md shadow-indigo-500/20 cursor-pointer border-none"
          >
            <FiMessageSquare className="w-4 h-4" />
            Nhắn tin trực tiếp
          </button>

          {/* Book Viewing Button */}
          <button
            onClick={onBookViewing}
            className="w-full flex items-center justify-center gap-2 px-5 h-[46px] rounded-2xl bg-[#0084ff] text-white text-sm font-bold hover:bg-[#0073df] transition-all shadow-md shadow-blue-500/20 cursor-pointer border-none"
          >
            <FiCalendar className="w-4 h-4" />
            Đặt lịch xem phòng
          </button>

          {/* Request Rental Button */}
          {onRequestRental && (
            <button
              onClick={onRequestRental}
              className="w-full flex items-center justify-center gap-2 px-5 h-[46px] rounded-2xl bg-gradient-to-r from-amber-500 to-orange-500 text-white text-sm font-bold hover:from-amber-600 hover:to-orange-600 transition-all shadow-md shadow-orange-500/20 cursor-pointer border-none"
            >
              <FiSend className="w-4 h-4" />
              Gửi yêu cầu thuê phòng
            </button>
          )}

          {/* Favorite Button */}
          {onToggleFavorite && (
            <button
              onClick={onToggleFavorite}
              className={`w-full flex items-center justify-center gap-2 px-5 h-[44px] rounded-2xl border text-sm font-semibold transition-all cursor-pointer ${
                isFavorite
                  ? 'bg-rose-50 border-rose-200 text-rose-600 font-bold'
                  : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
              }`}
            >
              <FiHeart className={`w-4 h-4 ${isFavorite ? 'fill-rose-500 text-rose-500' : ''}`} />
              {isFavorite ? 'Đã lưu phòng này' : 'Lưu tin yêu thích'}
            </button>
          )}
        </div>
      </div>

      {/* Safety Warning Box */}
      <div className="bg-amber-50/80 border border-amber-200/80 rounded-3xl p-5">
        <div className="flex items-start gap-3">
          <div className="w-9 h-9 rounded-xl bg-amber-100 flex items-center justify-center flex-shrink-0">
            <FiAlertCircle className="w-5 h-5 text-amber-600" />
          </div>
          <div className="flex-1">
            <h4 className="text-sm font-bold text-amber-900 mb-2">Lưu ý an toàn khi thuê</h4>
            <ul className="text-xs text-amber-800 space-y-1.5 leading-relaxed list-disc list-inside">
              <li>Không chuyển tiền cọc trước khi trực tiếp xem phòng.</li>
              <li>Kiểm tra kỹ hợp đồng và các khoản chi phí điện, nước, dịch vụ.</li>
              <li>Hẹn gặp trực tiếp tại địa chỉ phòng trọ, tránh điểm hẹn lạ.</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
};

export default LandlordContactCard;
