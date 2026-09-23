import React, { useEffect, useMemo, useState } from 'react';
import { FiCalendar, FiClock, FiMapPin, FiPhone, FiUser, FiXCircle } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { appointmentService, AppointmentItem } from '../../services/appointmentService';
import { APPOINTMENT_STATUS_LABELS } from '../../utils/constants';

const statusColor: Record<number, string> = {
  0: 'bg-amber-100 text-amber-700',
  1: 'bg-emerald-100 text-emerald-700',
  2: 'bg-rose-100 text-rose-700',
  3: 'bg-slate-100 text-slate-700',
  4: 'bg-blue-100 text-blue-700',
};

const TenantAppointmentsPage: React.FC = () => {
  const [appointments, setAppointments] = useState<AppointmentItem[]>([]);
  const [loading, setLoading] = useState(true);

  const loadAppointments = async () => {
    setLoading(true);
    try {
      const response = await appointmentService.getMyAppointments();
      setAppointments(response.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải lịch hẹn của bạn.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadAppointments();
  }, []);

  const total = appointments.length;
  const pending = appointments.filter((item) => item.status === 0).length;
  const approved = appointments.filter((item) => item.status === 1).length;

  const stats = useMemo(
    () => [
      { label: 'Tổng lịch hẹn', value: total },
      { label: 'Chờ xác nhận', value: pending },
      { label: 'Đã xác nhận', value: approved },
    ],
    [approved, pending, total],
  );

  const handleCancel = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn hủy lịch hẹn này?')) return;

    try {
      await appointmentService.cancelAppointment(id, 'Người thuê hủy lịch');
      toast.success('Hủy lịch hẹn thành công');
      await loadAppointments();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể hủy lịch hẹn.');
    }
  };

  return (
    <div className="p-6">
      <div className="mb-6">
        <p className="text-xs uppercase tracking-[0.2em] text-slate-500">NGƯỜI THUÊ</p>
        <h1 className="text-3xl font-bold text-slate-900">Lịch hẹn của tôi</h1>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
        {stats.map((stat) => (
          <div key={stat.label} className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
            <div className="text-sm text-slate-500">{stat.label}</div>
            <div className="text-3xl font-bold text-slate-900 mt-2">{stat.value}</div>
          </div>
        ))}
      </div>

      {loading ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-10 text-center text-slate-500">
          Đang tải lịch hẹn...
        </div>
      ) : appointments.length === 0 ? (
        <div className="bg-white rounded-2xl border border-slate-200 p-10 text-center text-slate-500">
          Bạn chưa có lịch hẹn xem phòng nào.
        </div>
      ) : (
        <div className="space-y-4">
          {appointments.map((appointment) => (
            <div key={appointment.id} className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm">
              <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4">
                <div>
                  <div className="flex items-center gap-2 mb-2 flex-wrap">
                    <h2 className="text-xl font-bold text-slate-900">{appointment.postTitle || 'Tin đăng'}</h2>
                    <span className={`px-2.5 py-1 rounded-full text-xs font-semibold ${statusColor[appointment.status] || 'bg-slate-100 text-slate-700'}`}>
                      {APPOINTMENT_STATUS_LABELS[appointment.status as keyof typeof APPOINTMENT_STATUS_LABELS] || 'Không xác định'}
                    </span>
                  </div>

                  <div className="text-sm text-slate-600 space-y-2">
                    <div className="flex items-center gap-2">
                      <FiMapPin className="text-slate-400" />
                      <span>{appointment.postAddress || 'Địa chỉ chưa cập nhật'}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiCalendar className="text-slate-400" />
                      <span>{appointment.ngayXem}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiClock className="text-slate-400" />
                      <span>{appointment.gioXem}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiUser className="text-slate-400" />
                      <span>{appointment.landlordName}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <FiPhone className="text-slate-400" />
                      <span>{appointment.landlordPhone}</span>
                    </div>
                  </div>

                  {appointment.tenantNote && (
                    <div className="mt-3 text-sm text-slate-600 bg-slate-50 rounded-lg p-3 border border-slate-200">
                      Ghi chú: {appointment.tenantNote}
                    </div>
                  )}
                </div>

                <div className="flex flex-col gap-2 min-w-[170px]">
                  <button
                    type="button"
                    onClick={() => handleCancel(appointment.id)}
                    disabled={appointment.status === 2 || appointment.status === 3 || appointment.status === 4}
                    className="inline-flex items-center justify-center gap-2 bg-red-50 text-red-600 border border-red-200 rounded-xl px-3 py-2 text-sm font-medium disabled:opacity-50 disabled:cursor-not-allowed"
                  >
                    <FiXCircle /> Hủy lịch
                  </button>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default TenantAppointmentsPage;
