import React, { useEffect, useState } from 'react';
import { adminService } from '../../services/adminService';
import './LandlordPages.css';

interface RoomItem {
  id: number;
  roomName: string;
  landlordName: string;
  address: string;
  price: number;
  status: number;
}

const statusLabel = (status: number) => ['Còn trống', 'Đã thuê', 'Đã đặt', 'Tạm ngưng'][status] || 'Không rõ';

const LandlordRoomsPage: React.FC = () => {
  const [rooms, setRooms] = useState<RoomItem[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const load = async () => {
      try {
        const response = await adminService.getRooms();
        setRooms((response.data ?? []).filter((room) => room.landlordName));
      } catch (error) {
        console.error('Không thể tải danh sách phòng:', error);
        setRooms([]);
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, []);

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>Quản lý phòng trọ</h1>
            <p>Giám sát danh sách phòng, tình trạng phòng và giá thuê hiện tại.</p>
          </div>
        </div>

        {loading ? (
          <div className="empty-state">Đang tải danh sách phòng...</div>
        ) : rooms.length === 0 ? (
          <div className="empty-state"><div><h3>Chưa có phòng nào</h3><p>Đăng tin trước để hệ thống tạo phòng cho bạn.</p></div></div>
        ) : (
          <div className="landlord-grid">
            {rooms.map((room) => (
              <article key={room.id} className="landlord-card">
                <div className="landlord-card-header">
                  <h3>{room.roomName}</h3>
                  <span className={`status-pill status-${room.status}`}>{statusLabel(room.status)}</span>
                </div>

                <p style={{ color: '#64748b', margin: 0 }}>{room.address}</p>

                <div className="landlord-meta">
                  <div className="meta-item">
                    <small>Giá thuê</small>
                    <strong>{new Intl.NumberFormat('vi-VN').format(room.price)}đ</strong>
                  </div>
                  <div className="meta-item">
                    <small>Chủ trọ</small>
                    <strong>{room.landlordName}</strong>
                  </div>
                </div>
              </article>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default LandlordRoomsPage;
