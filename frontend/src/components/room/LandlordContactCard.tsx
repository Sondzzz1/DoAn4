import React from 'react';
import { Post } from '../../types/post.types';
import { FiPhone, FiMessageSquare, FiCalendar, FiAlertCircle, FiUser } from 'react-icons/fi';

interface LandlordContactCardProps {
  post: Post;
  onBookViewing: () => void;
  onSendMessage: () => void;
}

const LandlordContactCard: React.FC<LandlordContactCardProps> = ({
  post,
  onBookViewing,
  onSendMessage,
}) => {
  const handleCallPhone = () => {
    window.location.href = `tel:${post.landlordPhone}`;
  };

  return (
    <div className="sticky top-[90px] space-y-4">
      {/* Main Contact Card */}
      <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
        <h3 className="text-[18px] font-bold text-gray-900 mb-4">Thông tin liên hệ</h3>

        {/* Landlord Info */}
        <div className="flex items-center gap-3 mb-5 pb-5 border-b border-gray-100">
          <div className="w-12 h-12 rounded-full bg-gradient-to-br from-blue-500 to-blue-600 flex items-center justify-center flex-shrink-0 text-white font-semibold text-lg">
            {post.landlordName.charAt(0).toUpperCase()}
          </div>
          <div>
            <p className="text-[15px] font-semibold text-gray-900 mb-0.5">{post.landlordName}</p>
            <p className="text-[13px] text-gray-500 flex items-center gap-1">
              <FiUser className="w-3.5 h-3.5" />
              Chủ nhà
            </p>
          </div>
        </div>

        {/* Action Buttons */}
        <div className="space-y-2.5">
          {/* Phone Button */}
          <button
            onClick={handleCallPhone}
            className="w-full flex items-center justify-center gap-2 px-5 h-[48px] rounded-xl bg-[#00a651] text-white text-[15px] font-semibold hover:bg-[#008f45] transition-colors shadow-sm"
          >
            <FiPhone className="w-5 h-5" />
            {post.landlordPhone}
          </button>

          {/* Message Button */}
          <button
            onClick={onSendMessage}
            className="w-full flex items-center justify-center gap-2 px-5 h-[48px] rounded-xl bg-white border-2 border-[#0084ff] text-[#0084ff] text-[15px] font-semibold hover:bg-blue-50 transition-colors"
          >
            <FiMessageSquare className="w-5 h-5" />
            Nhắn tin
          </button>

          {/* Book Viewing Button */}
          <button
            onClick={onBookViewing}
            className="w-full flex items-center justify-center gap-2 px-5 h-[48px] rounded-xl bg-[#0084ff] text-white text-[15px] font-semibold hover:bg-[#0073df] transition-colors shadow-sm"
          >
            <FiCalendar className="w-5 h-5" />
            Đặt lịch xem phòng
          </button>
        </div>
      </div>

      {/* Safety Warning Box */}
      <div className="bg-amber-50 border border-amber-200 rounded-2xl p-5">
        <div className="flex items-start gap-3">
          <div className="w-10 h-10 rounded-xl bg-amber-100 flex items-center justify-center flex-shrink-0">
            <FiAlertCircle className="w-5 h-5 text-amber-600" />
          </div>
          <div className="flex-1">
            <h4 className="text-[15px] font-bold text-amber-900 mb-2">Lưu ý an toàn</h4>
            <ul className="text-[13px] text-amber-800 space-y-1.5 leading-relaxed">
              <li className="flex items-start gap-2">
                <span className="text-amber-600 mt-0.5">•</span>
                <span>Không chuyển tiền trước khi xem phòng và ký hợp đồng</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-amber-600 mt-0.5">•</span>
                <span>Yêu cầu xem giấy tờ chủ quyền nhà đất trước khi thuê</span>
              </li>
              <li className="flex items-start gap-2">
                <span className="text-amber-600 mt-0.5">•</span>
                <span>Hẹn gặp tại phòng trọ, tránh gặp ở nơi vắng vẻ</span>
              </li>
            </ul>
          </div>
        </div>
      </div>

      {/* Report Link */}
      <div className="text-center">
        <button className="text-[13px] text-gray-500 hover:text-red-500 transition-colors underline">
          Báo cáo tin đăng này
        </button>
      </div>
    </div>
  );
};

export default LandlordContactCard;
