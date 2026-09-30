import React, { useEffect, useMemo, useState } from 'react';
import { FiCalendar, FiClock, FiMapPin, FiPhone, FiUser, FiXCircle } from 'react-icons/fi';
import { toast } from 'react-toastify';
import StatusBadge, { StatusTone } from '../../components/common/StatusBadge';
import PageState from '../../components/common/PageState';
import { appointmentService, AppointmentItem } from '../../services/appointmentService';
import { APPOINTMENT_STATUS, APPOINTMENT_STATUS_LABELS } from '../../utils/constants';
import { getApiErrorMessage } from '../../utils/apiError';

const tone = (status: number): StatusTone => status === 0 ? 'pending' : status === 1 ? 'success' : status === 3 ? 'info' : 'danger';

const TenantAppointmentsPage: React.FC = () => {
  const [appointments, setAppointments] = useState<AppointmentItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [filter, setFilter] = useState<number | 'all'>('all');

  const loadAppointments = async () => {
    setLoading(true); setError('');
    try { setAppointments((await appointmentService.getMyAppointments()).data || []); }
    catch (e) { const message = getApiErrorMessage(e, 'Không thể tải lịch hẹn của bạn.'); setError(message); toast.error(message); }
    finally { setLoading(false); }
  };
  useEffect(() => { void loadAppointments(); }, []);

  const visible = useMemo(() => filter === 'all' ? appointments : appointments.filter(x => x.status === filter), [appointments, filter]);
  const cancel = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn hủy lịch hẹn này?')) return;
    try { await appointmentService.cancelAppointment(id, 'Người thuê hủy lịch'); toast.success('Đã hủy lịch hẹn'); await loadAppointments(); }
    catch (e) { toast.error(getApiErrorMessage(e, 'Không thể hủy lịch hẹn.')); }
  };

  return <div className="mx-auto max-w-6xl p-4 md:p-6">
    <header className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><p className="text-xs font-semibold uppercase text-blue-600">Người thuê</p><h1 className="text-2xl font-bold text-slate-900">Lịch hẹn của tôi</h1><p className="mt-1 text-sm text-slate-500">Theo dõi lịch xem phòng và phản hồi từ chủ trọ.</p></div>
      <select value={filter} onChange={e => setFilter(e.target.value === 'all' ? 'all' : Number(e.target.value))} className="rounded-md border border-slate-300 bg-white px-3 py-2 text-sm">
        <option value="all">Tất cả trạng thái</option>{Object.entries(APPOINTMENT_STATUS_LABELS).map(([k,v]) => <option key={k} value={k}>{v}</option>)}
      </select>
    </header>
    {loading ? <PageState type="loading" message="Đang tải lịch hẹn..." /> : error ? <PageState type="error" message={error} onRetry={loadAppointments} /> : visible.length === 0 ? <PageState type="empty" message="Chưa có lịch xem phòng phù hợp." /> :
      <div className="space-y-3">{visible.map(a => <article key={a.id} className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
        <div className="flex flex-col gap-4 md:flex-row md:justify-between">
          <div className="min-w-0 flex-1"><div className="mb-3 flex flex-wrap items-center gap-2"><h2 className="text-lg font-bold text-slate-900">{a.roomName || a.postTitle}</h2><StatusBadge label={APPOINTMENT_STATUS_LABELS[a.status as keyof typeof APPOINTMENT_STATUS_LABELS] || 'Không xác định'} tone={tone(a.status)} /></div>
            <div className="grid gap-2 text-sm text-slate-600 sm:grid-cols-2"><span className="flex gap-2"><FiMapPin />{a.postAddress}</span><span className="flex gap-2"><FiCalendar />{new Date(a.scheduledAt).toLocaleDateString('vi-VN')}</span><span className="flex gap-2"><FiClock />{new Date(a.scheduledAt).toLocaleTimeString('vi-VN',{hour:'2-digit',minute:'2-digit'})}</span><span className="flex gap-2"><FiUser />{a.landlordName}</span><span className="flex gap-2"><FiPhone />{a.landlordPhone}</span></div>
            {a.landlordResponse && <p className="mt-3 rounded-md bg-rose-50 p-3 text-sm text-rose-700">Phản hồi: {a.landlordResponse}</p>}
          </div>
          {(a.status === APPOINTMENT_STATUS.PENDING || a.status === APPOINTMENT_STATUS.CONFIRMED) && <button onClick={() => cancel(a.id)} className="inline-flex h-10 items-center justify-center gap-2 rounded-md border border-rose-200 px-4 text-sm font-semibold text-rose-700 hover:bg-rose-50"><FiXCircle />Hủy lịch</button>}
        </div>
      </article>)}</div>}
  </div>;
};
export default TenantAppointmentsPage;
