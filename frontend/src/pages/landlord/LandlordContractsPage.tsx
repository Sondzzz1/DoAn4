import React, { useEffect, useState } from 'react';
import { landlordService } from '../../services/landlordService';
import './LandlordPages.css';

interface ContractItem {
  id: number;
  baiDangId: number;
  nguoiThueId: number;
  chuTroId: number;
  ngayBatDau: string;
  ngayKetThuc: string;
  tienThueHangThang: number;
  trangThai: number;
  nguoiThueDaXacNhan: boolean;
  chuTroDaXacNhan: boolean;
}

const statusLabel = (status: number) => ['Chờ xác nhận', 'Đã kích hoạt', 'Từ chối'][status] || 'Không rõ';
const formatDate = (value: string) => new Intl.DateTimeFormat('vi-VN').format(new Date(value));

const LandlordContractsPage: React.FC = () => {
  const [items, setItems] = useState<ContractItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    try {
      const response = await landlordService.getContracts();
      setItems(response.data ?? []);
    } catch (error) {
      console.error('Không thể tải hợp đồng:', error);
      setItems([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleConfirm = async (id: number) => {
    try {
      await landlordService.confirmContract(id);
      await load();
    } catch (error) {
      console.error('Xác nhận hợp đồng thất bại:', error);
    }
  };

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>Hợp đồng & đặt cọc</h1>
            <p>Theo dõi các hợp đồng, xác nhận đặt cọc và hoàn tất thủ tục cho thuê.</p>
          </div>
        </div>

        {loading ? (
          <div className="empty-state">Đang tải hợp đồng...</div>
        ) : items.length === 0 ? (
          <div className="empty-state"><div><h3>Chưa có hợp đồng</h3><p>Hợp đồng sẽ xuất hiện sau khi người thuê chốt yêu cầu.</p></div></div>
        ) : (
          <div className="landlord-grid">
            {items.map((item) => (
              <article key={item.id} className="landlord-card">
                <div className="landlord-card-header">
                  <h3>Hợp đồng #{item.id}</h3>
                  <span className={`status-pill status-${item.trangThai}`}>{statusLabel(item.trangThai)}</span>
                </div>

                <div className="landlord-meta">
                  <div className="meta-item">
                    <small>Người thuê</small>
                    <strong>{item.nguoiThueId}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Tiền thuê</small>
                    <strong>{new Intl.NumberFormat('vi-VN').format(item.tienThueHangThang)}đ</strong>
                  </div>
                  <div className="meta-item">
                    <small>Bắt đầu</small>
                    <strong>{formatDate(item.ngayBatDau)}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Kết thúc</small>
                    <strong>{formatDate(item.ngayKetThuc)}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Chủ trọ xác nhận</small>
                    <strong>{item.chuTroDaXacNhan ? 'Đã xác nhận' : 'Chưa xác nhận'}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Người thuê xác nhận</small>
                    <strong>{item.nguoiThueDaXacNhan ? 'Đã xác nhận' : 'Chưa xác nhận'}</strong>
                  </div>
                </div>

                {item.trangThai === 0 && (
                  <div className="landlord-card-actions">
                    <button className="landlord-btn" onClick={() => handleConfirm(item.id)}>Xác nhận hợp đồng</button>
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

export default LandlordContractsPage;
