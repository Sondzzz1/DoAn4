import React, { useEffect, useState } from 'react';
import { landlordService } from '../../services/landlordService';
import { APPOINTMENT_STATUS_LABELS } from '../../utils/constants';
import './LandlordPages.css';

interface AppointmentItem {
  id: number;
  postTitle: string;
  postAddress: string;
  tenantName: string;
  tenantPhone: string;
  scheduledAt: string;
  status: number;
  tenantNote?: string;
}

const formatDate = (value: string) => new Intl.DateTimeFormat('vi-VN', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value));

const LandlordAppointmentsPage: React.FC = () => {
  const [items, setItems] = useState<AppointmentItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    try {
      const response = await landlordService.getAppointments();
      setItems(response.data ?? []);
    } catch (error) {
      console.error('Không thể tải lịch hẹn:', error);
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleStatus = async (id: number, action: 'confirm' | 'reject' | 'complete') => {
    try {
      if (action === 'confirm') await landlordService.confirmAppointment(id);
      if (action === 'reject') await landlordService.rejectAppointment(id, 'Không phù hợp với yêu cầu của bạn');
      if (action === 'complete') await landlordService.completeAppointment(id);
      await load();
    } catch (error) {
      console.error('Cập nhật lịch hẹn thất bại:', error);
    }
  };

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>Quản lý lịch hẹn xem phòng</h1>
            <p>Xác nhận, từ chối và theo dõi buổi xem phòng với người thuê.</p>
          </div>
        </div>

        {loading ? (
          <div className="empty-state">Đang tải lịch hẹn...</div>
        ) : items.length === 0 ? (
          <div className="empty-state"><div><h3>Chưa có lịch hẹn nào</h3><p>Thông tin lịch hẹn sẽ xuất hiện tại đây.</p></div></div>
        ) : (
          <div className="landlord-grid">
            {items.map((item) => (
              <article key={item.id} className="landlord-card">
                <div className="landlord-card-header">
                  <h3>{item.postTitle}</h3>
                  <span className={`status-pill status-${item.status}`}>{APPOINTMENT_STATUS_LABELS[item.status as keyof typeof APPOINTMENT_STATUS_LABELS] || 'Không rõ'}</span>
                </div>

                <p style={{ color: '#64748b', margin: 0 }}>{item.postAddress}</p>

                <div className="landlord-meta">
                  <div className="meta-item">
                    <small>Người thuê</small>
                    <strong>{item.tenantName}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Điện thoại</small>
                    <strong>{item.tenantPhone}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Thời gian</small>
                    <strong>{formatDate(item.scheduledAt)}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Ghi chú</small>
                    <strong>{item.tenantNote || 'Không có'}</strong>
                  </div>
                </div>

                <div className="landlord-card-actions">
                  {item.status === 0 && (
                    <>
                      <button className="landlord-btn-secondary" onClick={() => handleStatus(item.id, 'confirm')}>Xác nhận</button>
                      <button className="landlord-btn-danger" onClick={() => handleStatus(item.id, 'reject')}>Từ chối</button>
                    </>
                  )}
                  {item.status === 1 && (
                    <button className="landlord-btn" onClick={() => handleStatus(item.id, 'complete')}>Hoàn thành</button>
                  )}
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default LandlordAppointmentsPage;
