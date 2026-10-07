import React, { useState } from 'react';
import { FiCalendar, FiCheck, FiClock } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { Post } from '../../types/post.types';
import { appointmentService } from '../../services/appointmentService';
import { getApiErrorMessage } from '../../utils/apiError';
import Modal from '../common/Modal';
import Input from '../common/Input';

interface BookingModalProps { isOpen: boolean; onClose: () => void; post: Post; }

const BookingModal: React.FC<BookingModalProps> = ({ isOpen, onClose, post }) => {
  const [formData, setFormData] = useState({ date: '', time: '', phone: '', message: '' });
  const [errors, setErrors] = useState<Partial<Record<keyof typeof formData, string>>>({});
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState('');
  const now = new Date();
  const today = [now.getFullYear(), String(now.getMonth() + 1).padStart(2, '0'), String(now.getDate()).padStart(2, '0')].join('-');
  const change = (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
    const key = event.target.name as keyof typeof formData;
    setFormData(current => ({ ...current, [key]: event.target.value }));
    setErrors(current => ({ ...current, [key]: undefined }));
  };
  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    if (isSubmitting) return;
    const next: typeof errors = {};
    if (!formData.date) next.date = 'Vui lòng chọn ngày xem.';
    if (!formData.time) next.time = 'Vui lòng chọn giờ xem.';
    if (formData.date && formData.time && new Date(formData.date + 'T' + formData.time) <= new Date()) next.time = 'Lịch xem phải ở thời điểm tương lai.';
    if (!/^[0-9]{10,11}$/.test(formData.phone.trim())) next.phone = 'Nhập số điện thoại gồm 10–11 chữ số.';
    setErrors(next);
    if (Object.keys(next).length) return;
    setIsSubmitting(true); setSubmitError('');
    try {
      const response = await appointmentService.createAppointment({ baiDangId: post.id, chuTroId: post.landlordId, ngayXem: formData.date, gioXem: formData.time, ghiChu: formData.message || '' });
      if (!response.success) throw new Error(response.message || 'Không thể đặt lịch xem phòng.');
      toast.success(response.message || 'Đã gửi lịch xem phòng.');
      onClose();
      setFormData({ date: '', time: '', phone: '', message: '' });
    } catch (error) { setSubmitError(getApiErrorMessage(error, 'Không thể gửi lịch hẹn. Vui lòng thử lại.')); }
    finally { setIsSubmitting(false); }
  };

  return <Modal isOpen={isOpen} onClose={() => { if (!isSubmitting) onClose(); }} title="Đặt lịch xem phòng">
    <div className="mb-5 border-b border-slate-200 pb-4"><h3 className="font-semibold text-slate-900">{post.title}</h3><p className="mt-1 text-sm text-slate-500">{[post.address, post.district, post.province].filter(Boolean).join(', ')}</p></div>
    <form onSubmit={handleSubmit} noValidate className="space-y-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Input label="Ngày xem" name="date" type="date" min={today} required value={formData.date} onChange={change} error={errors.date} />
        <Input label="Giờ xem" name="time" type="time" required value={formData.time} onChange={change} error={errors.time} />
      </div>
      <Input label="Số điện thoại liên hệ" name="phone" type="tel" inputMode="tel" autoComplete="tel" required value={formData.phone} onChange={change} placeholder="Nhập số điện thoại" error={errors.phone} />
      <label htmlFor="booking-note" className="block text-sm font-medium text-slate-700">Lời nhắn cho chủ trọ</label>
      <textarea id="booking-note" name="message" rows={4} maxLength={1000} value={formData.message} onChange={change} placeholder="Thời gian bạn thuận tiện, câu hỏi về phòng..." className="w-full rounded-md border border-slate-300 px-3 py-2 text-sm" />
      <p className="flex items-start gap-2 text-sm text-slate-500"><FiCalendar className="mt-1 shrink-0" /><span>Chủ trọ sẽ phản hồi lịch hẹn trong mục Lịch xem phòng.</span></p>
      {submitError && <p className="form-error" role="alert">{submitError}</p>}
      <div className="form-actions"><button type="button" disabled={isSubmitting} onClick={onClose}>Hủy</button><button type="submit" disabled={isSubmitting} className="inline-flex items-center justify-center gap-2 bg-[#0084ff] text-white disabled:opacity-60">{isSubmitting ? <FiClock /> : <FiCheck />}{isSubmitting ? 'Đang gửi...' : 'Xác nhận lịch hẹn'}</button></div>
    </form>
  </Modal>;
};
export default BookingModal;
