import React, { useEffect, useMemo, useState } from 'react';
import { FiCheck, FiEdit2, FiHome, FiPlus, FiSearch, FiTrash2, FiX } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { roomService } from '../../services/roomService';
import { RoomItem } from '../../types/room.types';

const roomStatusLabel = (status: number) => ['Còn trống', 'Đã thuê', 'Tạm ngưng'][status] || 'Không rõ';

const emptyForm = {
  tenPhong: '',
  moTa: '',
  gia: 0,
  dienTich: 0,
  soNguoiToiDa: 2,
  soPhongNgu: 1,
  soPhongTam: 1,
  tang: 1,
  diaChi: '',
  phuong: '',
  quan: '',
  thanhPho: '',
  tienIchIds: [] as number[],
  danhSachAnh: [] as string[],
};

const LandlordRoomManagementPage: React.FC = () => {
  const [rooms, setRooms] = useState<RoomItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(emptyForm);

  const loadRooms = async () => {
    setLoading(true);
    try {
      const response = await roomService.getMyRooms(statusFilter ? Number(statusFilter) : undefined);
      setRooms(response.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải danh sách phòng.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadRooms();
  }, [statusFilter]);

  const filteredRooms = useMemo(() => {
    const q = query.trim().toLowerCase();
    return rooms.filter((room) => {
      if (!q) return true;
      return [room.roomName, room.address, room.district, room.province]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(q));
    });
  }, [query, rooms]);

  const clearForm = () => {
    setForm(emptyForm);
    setEditingId(null);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!form.tenPhong.trim() || !form.diaChi.trim()) {
      toast.error('Vui lòng nhập tên phòng và địa chỉ.');
      return;
    }

    try {
      if (editingId) {
        await roomService.updateRoom(editingId, { ...form });
        toast.success('Cập nhật phòng thành công');
      } else {
        await roomService.createRoom({ ...form });
        toast.success('Thêm phòng thành công');
      }
      clearForm();
      await loadRooms();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Có lỗi xảy ra khi lưu phòng.');
    }
  };

  const handleEdit = (room: RoomItem) => {
    setEditingId(room.id);
    setForm({
      tenPhong: room.roomName,
      moTa: room.description || '',
      gia: room.price,
      dienTich: room.area,
      soNguoiToiDa: room.maxOccupants,
      soPhongNgu: room.bedrooms ?? 1,
      soPhongTam: room.bathrooms ?? 1,
      tang: room.floor ?? 1,
      diaChi: room.address,
      phuong: room.ward || '',
      quan: room.district || '',
      thanhPho: room.province || '',
      tienIchIds: room.amenityIds || [],
      danhSachAnh: room.imageUrls || [],
    });
  };

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn tạm ngưng phòng này?')) return;

    try {
      await roomService.deleteRoom(id);
      toast.success('Phòng đã được tạm ngưng');
      await loadRooms();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tạm ngưng phòng.');
    }
  };

  const handleStatusToggle = async (id: number, status: number) => {
    try {
      const nextStatus = status === 0 ? 2 : 0;
      await roomService.updateRoomStatus(id, nextStatus);
      toast.success('Cập nhật trạng thái phòng thành công');
      await loadRooms();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể cập nhật trạng thái phòng.');
    }
  };

  return (
    <div className="p-6">
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-6">
        <div>
          <p className="text-xs uppercase tracking-[0.2em] text-slate-500">QUẢN LÝ</p>
          <h1 className="text-3xl font-bold text-slate-900">Phòng trọ của tôi</h1>
        </div>
        <button
          onClick={() => { clearForm(); document.getElementById('room-form')?.scrollIntoView({ behavior: 'smooth' }); }}
          className="inline-flex items-center gap-2 bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700"
        >
          <FiPlus /> Thêm phòng
        </button>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-[1.3fr_0.7fr] gap-6">
        <div className="rounded-2xl border bg-white shadow-sm p-4">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
            <div className="relative w-full md:max-w-sm">
              <FiSearch className="absolute left-3 top-3 text-slate-400" />
              <input
                value={query}
                onChange={(e) => setQuery(e.target.value)}
                className="w-full border rounded-lg pl-10 pr-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Tìm phòng..."
              />
            </div>

            <select
              value={statusFilter}
              onChange={(e) => setStatusFilter(e.target.value)}
              className="border rounded-lg px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="0">Còn trống</option>
              <option value="1">Đã thuê</option>
              <option value="2">Tạm ngưng</option>
            </select>
          </div>

          {loading ? (
            <div className="py-10 text-center text-slate-500">Đang tải thông tin phòng...</div>
          ) : filteredRooms.length === 0 ? (
            <div className="py-10 text-center text-slate-500">Chưa có phòng nào phù hợp.</div>
          ) : (
            <div className="space-y-4">
              {filteredRooms.map((room) => (
                <div key={room.id} className="border rounded-xl p-4 hover:shadow-sm transition">
                  <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
                    <div className="flex items-center gap-4">
                      <div className="w-20 h-20 rounded-xl bg-slate-100 overflow-hidden">
                        {room.imageUrls?.[0] ? (
                          <img src={room.imageUrls[0]} alt={room.roomName} className="w-full h-full object-cover" />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-slate-400"><FiHome /></div>
                        )}
                      </div>

                      <div>
                        <h3 className="font-semibold text-lg text-slate-900">{room.roomName}</h3>
                        <p className="text-sm text-slate-500">{room.address}</p>
                        <div className="flex flex-wrap gap-2 mt-2 text-xs">
                          <span className="bg-slate-100 px-2 py-1 rounded-full">{room.area} m²</span>
                          <span className="bg-slate-100 px-2 py-1 rounded-full">{room.maxOccupants} người</span>
                          <span className="bg-slate-100 px-2 py-1 rounded-full">{room.price.toLocaleString()} đ/tháng</span>
                        </div>
                      </div>
                    </div>

                    <div className="flex items-center gap-2">
                      <span className="px-2.5 py-1 rounded-full bg-blue-50 text-blue-700 text-xs font-medium">
                        {roomStatusLabel(room.status)}
                      </span>
                    </div>
                  </div>

                  <div className="flex flex-wrap gap-2 mt-4">
                    <button onClick={() => handleEdit(room)} className="inline-flex items-center gap-1 bg-amber-100 text-amber-700 px-3 py-2 rounded-lg text-sm font-medium">
                      <FiEdit2 /> Sửa
                    </button>
                    <button onClick={() => handleStatusToggle(room.id, room.status)} className="inline-flex items-center gap-1 bg-emerald-100 text-emerald-700 px-3 py-2 rounded-lg text-sm font-medium">
                      <FiCheck /> Đổi trạng thái
                    </button>
                    <button onClick={() => handleDelete(room.id)} className="inline-flex items-center gap-1 bg-red-100 text-red-700 px-3 py-2 rounded-lg text-sm font-medium">
                      <FiTrash2 /> Tạm ngưng
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        <div id="room-form" className="rounded-2xl border bg-white shadow-sm p-4 h-fit">
          <h2 className="text-xl font-semibold mb-4">{editingId ? 'Cập nhật phòng' : 'Thêm phòng mới'}</h2>

          <form onSubmit={handleSubmit} className="space-y-3">
            <input value={form.tenPhong} onChange={(e) => setForm({ ...form, tenPhong: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Tên phòng" />
            <textarea value={form.moTa} onChange={(e) => setForm({ ...form, moTa: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Mô tả" rows={3} />

            <div className="grid grid-cols-2 gap-3">
              <input type="number" value={form.gia} onChange={(e) => setForm({ ...form, gia: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Giá" />
              <input type="number" value={form.dienTich} onChange={(e) => setForm({ ...form, dienTich: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Diện tích" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <input type="number" value={form.soNguoiToiDa} onChange={(e) => setForm({ ...form, soNguoiToiDa: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Số người tối đa" />
              <input type="number" value={form.tang} onChange={(e) => setForm({ ...form, tang: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Tầng" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <input type="number" value={form.soPhongNgu} onChange={(e) => setForm({ ...form, soPhongNgu: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Số phòng ngủ" />
              <input type="number" value={form.soPhongTam} onChange={(e) => setForm({ ...form, soPhongTam: Number(e.target.value) })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Số phòng tắm" />
            </div>

            <input value={form.diaChi} onChange={(e) => setForm({ ...form, diaChi: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Địa chỉ" />
            <div className="grid grid-cols-2 gap-3">
              <input value={form.phuong} onChange={(e) => setForm({ ...form, phuong: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Phường" />
              <input value={form.quan} onChange={(e) => setForm({ ...form, quan: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Quận" />
            </div>
            <input value={form.thanhPho} onChange={(e) => setForm({ ...form, thanhPho: e.target.value })} className="w-full border rounded-lg px-3 py-2.5" placeholder="Thành phố" />

            <div className="flex gap-3 pt-2">
              <button type="submit" className="flex-1 bg-blue-600 text-white px-4 py-2.5 rounded-lg hover:bg-blue-700">
                {editingId ? 'Lưu thay đổi' : 'Thêm phòng'}
              </button>
              <button type="button" onClick={clearForm} className="px-4 py-2.5 rounded-lg border">
                <FiX />
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default LandlordRoomManagementPage;