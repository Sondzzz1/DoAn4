import React, { useEffect, useState } from 'react';
import { landlordService } from '../../services/landlordService';
import './LandlordPages.css';

interface RentalRequestItem {
  id: number;
  baiDangId: number;
  tieuDeBaiDang: string;
  nguoiThueId: number;
  ghiChu?: string;
  trangThai: number;
  ngayTao: string;
}

const statusLabel = (status: number) => ['Chờ xử lý', 'Đã duyệt', 'Từ chối'][status] || 'Không rõ';
const formatDate = (value: string) => new Intl.DateTimeFormat('vi-VN').format(new Date(value));

const LandlordRentalPage: React.FC = () => {
  const [items, setItems] = useState<RentalRequestItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    try {
      const response = await landlordService.getRentalRequests();
      setItems(response.data ?? []);
    } catch (error) {
      console.error('Không thể tải yêu cầu thuê:', error);
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleAction = async (id: number, action: 'approve' | 'reject') => {
    try {
      if (action === 'approve') await landlordService.approveRentalRequest(id);
      else await landlordService.rejectRentalRequest(id);
      await load();
    } catch (error) {
      console.error('Cập nhật yêu cầu thuê thất bại:', error);
    }
  };

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>Yêu cầu thuê phòng</h1>
            <p>Quản lý các đơn yêu cầu từ người thuê gửi tới phòng của bạn.</p>
          </div>
        </div>

        {loading ? (
          <div className="empty-state">Đang tải yêu cầu thuê...</div>
        ) : items.length === 0 ? (
          <div className="empty-state"><div><h3>Chưa có yêu cầu thuê</h3><p>Danh sách sẽ hiển thị khi người thuê gửi yêu cầu.</p></div></div>
        ) : (
          <div className="landlord-grid">
            {items.map((item) => (
              <article key={item.id} className="landlord-card">
                <div className="landlord-card-header">
                  <h3>{item.tieuDeBaiDang}</h3>
                  <span className={`status-pill status-${item.trangThai}`}>{statusLabel(item.trangThai)}</span>
                </div>

                <div className="landlord-meta">
                  <div className="meta-item">
                    <small>Người thuê</small>
                    <strong>{item.nguoiThueId}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Ngày gửi</small>
                    <strong>{formatDate(item.ngayTao)}</strong>
                  </div>
                  <div className="meta-item full-width" style={{ gridColumn: '1 / -1' }}>
                    <small>Ghi chú</small>
                    <strong>{item.ghiChu || 'Không có ghi chú'}</strong>
                  </div>
                </div>

                {item.trangThai === 0 && (
                  <div className="landlord-card-actions">
                    <button className="landlord-btn" onClick={() => handleAction(item.id, 'approve')}>Duyệt</button>
                    <button className="landlord-btn-danger" onClick={() => handleAction(item.id, 'reject')}>Từ chối</button>
                  </div>
                )}
              </article>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default LandlordRentalPage;
