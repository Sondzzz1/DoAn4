import React, { useState } from 'react';
import { Post } from '../../types/post.types';
import { FiX, FiCalendar, FiClock, FiUser, FiPhone, FiMessageSquare, FiCheck } from 'react-icons/fi';
import { toast } from 'react-toastify';

interface BookingModalProps {
  isOpen: boolean;
  onClose: () => void;
  post: Post;
}

const BookingModal: React.FC<BookingModalProps> = ({ isOpen, onClose, post }) => {
  const [formData, setFormData] = useState({
    date: '',
    time: '',
    name: '',
    phone: '',
    message: '',
  });
  const [isSubmitting, setIsSubmitting] = useState(false);

  if (!isOpen) return null;

  const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    if (!formData.date || !formData.time || !formData.name || !formData.phone) {
      toast.error('Vui lòng điền đầy đủ thông tin bắt buộc');
      return;
    }

    setIsSubmitting(true);

    try {
      // TODO: Call API to create appointment
      await new Promise((resolve) => setTimeout(resolve, 1500)); // Mock API call

      toast.success('Đặt lịch xem phòng thành công! Chủ nhà sẽ liên hệ với bạn sớm.');
      onClose();
      
      // Reset form
      setFormData({
        date: '',
        time: '',
        name: '',
        phone: '',
        message: '',
      });
    } catch (error) {
      toast.error('Có lỗi xảy ra. Vui lòng thử lại sau.');
    } finally {
      setIsSubmitting(false);
    }
  };

  // Get minimum date (today)
  const today = new Date().toISOString().split('T')[0];

  return (
    <div
      className="fixed inset-0 z-[9999] flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-2xl shadow-2xl max-w-[540px] w-full max-h-[90vh] overflow-y-auto"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <div className="sticky top-0 bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between z-10">
          <h3 className="text-[20px] font-bold text-gray-900">Đặt lịch xem phòng</h3>
          <button
            onClick={onClose}
            className="w-9 h-9 rounded-full hover:bg-gray-100 flex items-center justify-center transition-colors"
            aria-label="Đóng"
          >
            <FiX className="w-5 h-5 text-gray-600" />
          </button>
        </div>

        {/* Body */}
        <div className="px-6 py-5">
          {/* Room Info Summary */}
          <div className="mb-5 p-4 rounded-xl bg-gray-50 border border-gray-100">
            <h4 className="text-[15px] font-semibold text-gray-900 mb-1 line-clamp-2">
              {post.title}
            </h4>
            <p className="text-[13px] text-gray-600">
              {post.address}, {post.district}, {post.province}
            </p>
          </div>

          {/* Form */}
          <form onSubmit={handleSubmit} className="space-y-4">
            {/* Date & Time Row */}
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {/* Date */}
              <div>
                <label className="flex items-center gap-2 text-[14px] font-semibold text-gray-700 mb-2">
                  <FiCalendar className="w-4 h-4 text-[#0084ff]" />
                  Ngày xem <span className="text-red-500">*</span>
                </label>
                <input
                  type="date"
                  name="date"
                  value={formData.date}
                  onChange={handleChange}
                  min={today}
                  required
                  className="w-full px-4 py-2.5 rounded-xl border border-gray-300 text-[15px] focus:outline-none focus:ring-2 focus:ring-[#0084ff] focus:border-transparent transition-all"
                />
              </div>

              {/* Time */}
              <div>
                <label className="flex items-center gap-2 text-[14px] font-semibold text-gray-700 mb-2">
                  <FiClock className="w-4 h-4 text-[#0084ff]" />
                  Giờ xem <span className="text-red-500">*</span>
                </label>
                <input
                  type="time"
                  name="time"
                  value={formData.time}
                  onChange={handleChange}
                  required
                  className="w-full px-4 py-2.5 rounded-xl border border-gray-300 text-[15px] focus:outline-none focus:ring-2 focus:ring-[#0084ff] focus:border-transparent transition-all"
                />
              </div>
            </div>

            {/* Name */}
            <div>
              <label className="flex items-center gap-2 text-[14px] font-semibold text-gray-700 mb-2">
                <FiUser className="w-4 h-4 text-[#0084ff]" />
                Họ và tên <span className="text-red-500">*</span>
              </label>
              <input
                type="text"
                name="name"
                value={formData.name}
                onChange={handleChange}
                placeholder="Nhập họ và tên của bạn"
                required
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 text-[15px] placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[#0084ff] focus:border-transparent transition-all"
              />
            </div>

            {/* Phone */}
            <div>
              <label className="flex items-center gap-2 text-[14px] font-semibold text-gray-700 mb-2">
                <FiPhone className="w-4 h-4 text-[#0084ff]" />
                Số điện thoại <span className="text-red-500">*</span>
              </label>
              <input
                type="tel"
                name="phone"
                value={formData.phone}
                onChange={handleChange}
                placeholder="Nhập số điện thoại liên hệ"
                required
                pattern="[0-9]{10,11}"
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 text-[15px] placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[#0084ff] focus:border-transparent transition-all"
              />
            </div>

            {/* Message (Optional) */}
            <div>
              <label className="flex items-center gap-2 text-[14px] font-semibold text-gray-700 mb-2">
                <FiMessageSquare className="w-4 h-4 text-[#0084ff]" />
                Ghi chú (tùy chọn)
              </label>
              <textarea
                name="message"
                value={formData.message}
                onChange={handleChange}
                placeholder="Nhập lời nhắn hoặc câu hỏi cho chủ nhà..."
                rows={3}
                className="w-full px-4 py-2.5 rounded-xl border border-gray-300 text-[15px] placeholder:text-gray-400 focus:outline-none focus:ring-2 focus:ring-[#0084ff] focus:border-transparent transition-all resize-none"
              />
            </div>

            {/* Info Note */}
            <div className="p-4 rounded-xl bg-blue-50 border border-blue-100">
              <p className="text-[13px] text-blue-800 leading-relaxed">
                Chủ nhà sẽ liên hệ xác nhận lịch hẹn qua số điện thoại của bạn trong vòng 24 giờ.
              </p>
            </div>
          </form>
        </div>

        {/* Footer */}
        <div className="sticky bottom-0 bg-white border-t border-gray-100 px-6 py-4 flex items-center gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 px-5 h-[48px] rounded-xl border-2 border-gray-300 text-gray-700 text-[15px] font-semibold hover:bg-gray-50 transition-colors"
          >
            Hủy
          </button>
          <button
            type="submit"
            onClick={handleSubmit}
            disabled={isSubmitting}
            className="flex-1 flex items-center justify-center gap-2 px-5 h-[48px] rounded-xl bg-[#0084ff] text-white text-[15px] font-semibold hover:bg-[#0073df] transition-colors shadow-sm disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {isSubmitting ? (
              <>
                <div className="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                Đang gửi...
              </>
            ) : (
              <>
                <FiCheck className="w-5 h-5" />
                Xác nhận đặt lịch
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default BookingModal;
