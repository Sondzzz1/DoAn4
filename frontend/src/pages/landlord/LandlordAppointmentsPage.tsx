import React, { useEffect, useState } from 'react';
import { FiCheck, FiClock, FiMapPin, FiPhone, FiUser, FiX } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { landlordService } from '../../services/landlordService';
import { AppointmentItem } from '../../services/appointmentService';
import { APPOINTMENT_STATUS_LABELS } from '../../utils/constants';

const statusColor: Record<number, string> = {
  0: 'bg-amber-100 text-amber-700',
  1: 'bg-emerald-100 text-emerald-700',
  2: 'bg-rose-100 text-rose-700',
  3: 'bg-slate-100 text-slate-700',
  4: 'bg-blue-100 text-blue-700',
};

const LandlordAppointmentsPage: React.FC = () => {
  const [appointments, setAppointments] = useState<AppointmentItem[]>([]);
  const [loading, setLoading] = useState(true);

  const loadAppointments = async () => {
    setLoading(true);
    try {
      const response = await landlordService.getAppointments();
      const data = response?.data || response || [];
      setAppointments(Array.isArray(data) ? data : []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải lịch hẹn của bạn.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadAppointments();
  }, []);

  const updateAppointment = async (id: number, action: 'confirm' | 'reject' | 'complete') => {
    try {
      if (action === 'confirm') {
        await landlordService.confirmAppointment(id);
        toast.success('Đã xác nhận lịch hẹn');
      } else if (action === 'reject') {
        await landlordService.rejectAppointment(id, 'Chủ trọ không thể nhận lịch vào thời điểm này.');
        toast.success('Đã từ chối lịch hẹn');
      } else {
        await landlordService.completeAppointment(id);
        toast.success('Đã đánh dấu hoàn thành buổi xem phòng');
      }

      await loadAppointments();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể cập nhật lịch hẹn.');
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <p className="text-xs uppercase tracking-[0.2em] text-slate-500">CHỦ TRỌ</p>
        <h1 className="text-3xl font-bold text-slate-900">Lịch hẹn xem phòng</h1>
      </div>

      {loading ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-10 text-center text-slate-500">
          Đang tải danh sách lịch hẹn...
        </div>
      ) : appointments.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-10 text-center text-slate-500">
          Chưa có lịch hẹn nào cần xử lý.
        </div>
      ) : (
        <div className="space-y-4">
          {appointments.map((appointment) => (
            <div key={appointment.id} className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm">
              <div className="flex flex-col xl:flex-row xl:items-start xl:justify-between gap-4">
                <div className="flex-1">
                  <div className="flex items-center gap-2 mb-2 flex-wrap">
                    <h2 className="text-xl font-bold text-slate-900">{appointment.postTitle || 'Tin đăng'}</h2>
                    <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${statusColor[appointment.status] || 'bg-slate-100 text-slate-700'}`}>
                      {APPOINTMENT_STATUS_LABELS[appointment.status as keyof typeof APPOINTMENT_STATUS_LABELS] || 'Không xác định'}
                    </span>
                  </div>

                  <div className="grid grid-cols-1 md:grid-cols-2 gap-3 text-sm text-slate-600">
                    <div className="flex items-center gap-2">
                      <FiMapPin className="text-slate-400" />
                      <span>{appointment.postAddress || 'Địa chỉ chưa cập nhật'}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiClock className="text-slate-400" />
                      <span>{appointment.ngayXem} • {appointment.gioXem}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiUser className="text-slate-400" />
                      <span>{appointment.tenantName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiPhone className="text-slate-400" />
                      <span>{appointment.tenantPhone}</span>
                    </div>
                  </div>

                  {appointment.tenantNote && (
                    <div className="mt-3 text-sm text-slate-600 bg-slate-50 rounded-lg p-3 border border-slate-200">
                      Ghi chú của người thuê: {appointment.tenantNote}
                    </div>
                  )}
                </div>

                <div className="flex flex-wrap gap-2">
                  {appointment.status === 0 && (
                    <>
                      <button
                        type="button"
                        onClick={() => updateAppointment(appointment.id, 'confirm')}
                        className="inline-flex items-center gap-2 bg-emerald-500 text-white px-3 py-2 rounded-lg text-sm font-medium"
                      >
                        <FiCheck /> Xác nhận
                      </button>
                      <button
                        type="button"
                        onClick={() => updateAppointment(appointment.id, 'reject')}
                        className="inline-flex items-center gap-2 bg-rose-500 text-white px-3 py-2 rounded-lg text-sm font-medium"
                      >
                        <FiX /> Từ chối
                      </button>
                    </>
                  )}

                  {appointment.status === 1 && (
                    <button
                      type="button"
                      onClick={() => updateAppointment(appointment.id, 'complete')}
                      className="inline-flex items-center gap-2 bg-blue-500 text-white px-3 py-2 rounded-lg text-sm font-medium"
                    >
                      <FiCheck /> Hoàn thành
                    </button>
                  )}
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default LandlordAppointmentsPage;
