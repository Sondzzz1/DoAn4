import React, { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { FiCheck, FiEdit2, FiHome, FiPlus, FiSearch, FiTrash2, FiX } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { categoryService, type RoomCategory } from '../../services/categoryService';
import { adminService, type AdminCatalogItem } from '../../services/adminService';
import { roomService } from '../../services/roomService';
import { RoomItem } from '../../types/room.types';
import { getApiErrorMessage } from '../../utils/apiError';
import Input from '../../components/common/Input';
import PageState from '../../components/common/PageState';
import LeafletMapPicker from '../../components/map/LeafletMapPicker';
import { buildLocationQuery, type LocationResult } from '../../services/locationService';

const roomStatusLabel = (status: number) => ({ 0: 'Còn trống', 1: 'Đã thuê', 2: 'Đã giữ chỗ', 3: 'Tạm ngưng' }[status] || 'Không rõ');

const emptyForm = {
  tenPhong: '',
  danhMucId: 1,
  tienDien: 0,
  tienNuoc: 0,
  phiDichVu: 0,
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
  latitude: undefined as number | undefined,
  longitude: undefined as number | undefined,
  tienIchIds: [] as number[],
  danhSachAnh: [] as string[],
};

const LandlordRoomManagementPage: React.FC = () => {
  const [categories, setCategories] = useState<RoomCategory[]>([]);
  const [amenities, setAmenities] = useState<AdminCatalogItem[]>([]);
  const [rooms, setRooms] = useState<RoomItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [query, setQuery] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');
  const [locationBusy, setLocationBusy] = useState(false);
  const [locationSearch, setLocationSearch] = useState<{ query: string; revision: number }>();
  const addressEdited = useRef(false);
  const [mapSession, setMapSession] = useState(0);

  const changeAddress = (field: 'diaChi' | 'phuong' | 'quan' | 'thanhPho', value: string) => {
    addressEdited.current = true;
    setForm(previous => ({ ...previous, [field]: value, latitude: undefined, longitude: undefined }));
  };
  const searchCompletedAddress = () => {
    if (!addressEdited.current || !form.diaChi.trim()) return;
    addressEdited.current = false;
    const query = buildLocationQuery(form.diaChi, form.phuong, form.quan, form.thanhPho);
    setLocationSearch(previous => ({ query, revision: (previous?.revision || 0) + 1 }));
  };
  const handleLocationChange = useCallback((lat: number, lng: number, location?: LocationResult) => {
    setForm(previous => ({ ...previous, latitude: lat, longitude: lng,
      ...(location ? { diaChi: location.address || location.displayName, thanhPho: location.province || '',
        quan: location.district || '', phuong: location.ward || '' } : {}),
    }));
  }, []);

  const loadRooms = useCallback(async () => {
    setLoading(true);
    setLoadError('');
    try {
      const response = await roomService.getMyRooms(statusFilter ? Number(statusFilter) : undefined);
      setRooms(response.data || []);
    } catch (error) {
      setLoadError(getApiErrorMessage(error, 'Không thể tải danh sách phòng.'));
    } finally {
      setLoading(false);
    }
  }, [statusFilter]);

  useEffect(() => {
    void Promise.resolve().then(loadRooms);
  }, [loadRooms]);

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
    setMapSession(previous => previous + 1);
    addressEdited.current = false;
    setLocationSearch(undefined);
    setFormError('');
    setForm(emptyForm);
    setEditingId(null);
  };

  useEffect(() => {
    void categoryService.getActiveCategories().then(r => setCategories(r.data || [])).catch(() => {});
    void adminService.getAmenities().then(r => setAmenities(r.data || [])).catch(() => {});
  }, []);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (saving || locationBusy) return;
    if (!form.tenPhong.trim() || !form.diaChi.trim()) {
      toast.error('Vui lòng nhập tên phòng và địa chỉ.');
      return;
    }

    if (form.gia <= 0 || form.dienTich <= 0 || form.soNguoiToiDa < 1) {
      setFormError('Giá thuê, diện tích và số người tối đa phải lớn hơn 0.');
      return;
    }
    setSaving(true);
    setFormError('');
    try {
      if (editingId) {
        await roomService.updateRoom(editingId, { ...form, danhSachAnh: form.danhSachAnh.map(v => v.trim()).filter(Boolean) });
        toast.success('Cập nhật phòng thành công');
      } else {
        await roomService.createRoom({ ...form, danhSachAnh: form.danhSachAnh.map(v => v.trim()).filter(Boolean) });
        toast.success('Thêm phòng thành công');
      }
      clearForm();
      await loadRooms();
    } catch (error) {
      setFormError(getApiErrorMessage(error, 'Có lỗi xảy ra khi lưu phòng.'));
    } finally {
      setSaving(false);
    }
  };

  const handleEdit = (room: RoomItem) => {
    setMapSession(previous => previous + 1);
    addressEdited.current = false;
    setLocationSearch(undefined);
    setEditingId(room.id);
    setForm({
      tenPhong: room.roomName,
      danhMucId: room.categoryId,
      tienDien: room.electricityPrice ?? 0,
      tienNuoc: room.waterPrice ?? 0,
      phiDichVu: room.serviceFee ?? 0,
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
      latitude: room.latitude ?? undefined,
      longitude: room.longitude ?? undefined,
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
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể tạm ngưng phòng.'));
    }
  };

  const handleStatusToggle = async (id: number, status: number) => {
    try {
      if (status === 1) { toast.info('Phòng đang có hợp đồng hiệu lực. Trạng thái sẽ được cập nhật khi hợp đồng kết thúc.'); return; }
      const nextStatus = status === 0 ? 3 : 0;
      await roomService.updateRoomStatus(id, nextStatus);
      toast.success('Cập nhật trạng thái phòng thành công');
      await loadRooms();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể cập nhật trạng thái phòng.'));
    }
  };

  return (
    <div className="room-management-page">
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
        <div className="min-w-0">
          <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3 mb-4">
            <div className="relative w-full md:max-w-sm">
              <FiSearch className="absolute left-3 top-3 text-slate-400" />
              <input
                value={query}
                aria-label="Tìm phòng của tôi"
                onChange={(e) => setQuery(e.target.value)}
                className="w-full border rounded-lg pl-10 pr-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500"
                placeholder="Tìm phòng..."
              />
            </div>

            <select
              value={statusFilter}
              aria-label="Trạng thái phòng"
              onChange={(e) => setStatusFilter(e.target.value)}
              className="border rounded-lg px-3 py-2.5 outline-none focus:ring-2 focus:ring-blue-500"
            >
              <option value="">Tất cả trạng thái</option>
              <option value="0">Còn trống</option>
              <option value="1">Đã thuê</option>
              <option value="2">Đã giữ chỗ</option>
              <option value="3">Tạm ngưng</option>
            </select>
          </div>

          {loading ? (
            <div className="py-10 text-center text-slate-500">Đang tải thông tin phòng...</div>
          ) : loadError ? <PageState type="error" message={loadError} onRetry={loadRooms} /> : filteredRooms.length === 0 ? (
            <div className="py-10 text-center text-slate-500">Chưa có phòng nào phù hợp.</div>
          ) : (
            <div className="space-y-4">
              {filteredRooms.map((room) => (
                <div key={room.id} className="border rounded-xl p-4 hover:shadow-sm transition">
                  <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-3">
                    <div className="flex items-center gap-4">
                      <div className="w-20 h-20 shrink-0 rounded-lg bg-slate-100 overflow-hidden">
                        {room.imageUrls?.[0] ? (
                          <img src={room.imageUrls[0]} alt={room.roomName} className="w-full h-full object-cover" />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-slate-400"><FiHome /></div>
                        )}
                      </div>

                      <div className="min-w-0 break-words">
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
                    <button disabled={room.status === 1 || room.status === 2} title={room.status === 1 ? 'Phòng đang có hợp đồng hiệu lực' : 'Đổi giữa còn trống và tạm ngưng'} onClick={() => handleStatusToggle(room.id, room.status)} className="inline-flex items-center gap-1 bg-emerald-100 text-emerald-700 px-3 py-2 rounded-lg text-sm font-medium disabled:cursor-not-allowed disabled:opacity-50">
                      <FiCheck /> Đổi trạng thái
                    </button>
                    <button disabled={room.status === 1 || room.status === 2} onClick={() => handleDelete(room.id)} className="inline-flex items-center gap-1 bg-red-100 text-red-700 px-3 py-2 rounded-lg text-sm font-medium">
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
            <label className="block text-sm font-medium">Loại phòng
              <select aria-label="Loại phòng" value={form.danhMucId} onChange={e => setForm({ ...form, danhMucId: Number(e.target.value) })} className="mt-1 w-full border rounded-lg px-3 py-2.5">
                {categories.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </label>
            <Input label="Tên phòng" required value={form.tenPhong} onChange={(e) => setForm({ ...form, tenPhong: e.target.value })} />
            <label className="block text-sm font-medium text-slate-700">Mô tả
              <textarea value={form.moTa} onChange={(e) => setForm({ ...form, moTa: e.target.value })} className="mt-1 w-full border rounded-lg px-3 py-2.5" rows={3} />
            </label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              {([
                ['gia', 'Giá thuê (VNĐ/tháng)', 1], ['dienTich', 'Diện tích (m²)', 0.1],
                ['soNguoiToiDa', 'Số người tối đa', 1], ['tang', 'Tầng', 0],
                ['soPhongNgu', 'Số phòng ngủ', 0], ['soPhongTam', 'Số phòng tắm', 0],
                ['tienDien', 'Giá điện (đ/kWh)', 0], ['tienNuoc', 'Giá nước (đ/m³)', 0], ['phiDichVu', 'Phí dịch vụ (đ/tháng)', 0],
              ] as const).map(([field, label, min]) => (
                <Input key={field} label={label} type="number" min={min} step={field === 'dienTich' ? '0.1' : '1'} required value={form[field]} onChange={(e) => setForm({ ...form, [field]: Number(e.target.value) })} />
              ))}
            </div>
            <Input id="room-address" label="Địa chỉ" required value={form.diaChi} onChange={(e) => changeAddress('diaChi', e.target.value)} onBlur={searchCompletedAddress} />
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
              <Input label="Phường / xã" value={form.phuong} onChange={(e) => changeAddress('phuong', e.target.value)} onBlur={searchCompletedAddress} />
              <Input label="Quận / huyện" value={form.quan} onChange={(e) => changeAddress('quan', e.target.value)} onBlur={searchCompletedAddress} />
            </div>
            <Input label="Tỉnh / thành phố" value={form.thanhPho} onChange={(e) => changeAddress('thanhPho', e.target.value)} onBlur={searchCompletedAddress} />
            <LeafletMapPicker key={mapSession} latitude={form.latitude} longitude={form.longitude}
              address={form.diaChi} ward={form.phuong} district={form.quan} province={form.thanhPho}
              searchRequest={locationSearch} onLocationChange={handleLocationChange} onBusyChange={setLocationBusy} />
            <fieldset><legend className="text-sm font-medium">Tiện ích</legend>
              <div className="grid grid-cols-2 gap-2 mt-2">{amenities.filter(a => a.isActive !== false).map(a => <label key={a.id} className="flex items-center gap-2 text-sm">
                <input type="checkbox" checked={form.tienIchIds.includes(a.id)} onChange={e => setForm({ ...form, tienIchIds: e.target.checked ? [...form.tienIchIds, a.id] : form.tienIchIds.filter(v => v !== a.id) })} />{a.name}
              </label>)}</div>
            </fieldset>
            <label className="block text-sm font-medium">Ảnh phòng (mỗi dòng một URL)
              <textarea aria-label="Ảnh phòng" value={form.danhSachAnh.join('\n')} onChange={e => setForm({ ...form, danhSachAnh: e.target.value.split('\n') })} rows={3} className="mt-1 w-full border rounded-lg px-3 py-2.5" />
            </label>
            {formError && <p role="alert" className="text-sm text-red-600">{formError}</p>}

            <div className="flex gap-3 pt-2">
              <button type="submit" disabled={saving || locationBusy} className="flex-1 bg-blue-600 text-white px-4 py-2.5 rounded-lg hover:bg-blue-700 disabled:opacity-60">
                {saving ? 'Đang lưu...' : editingId ? 'Lưu thay đổi' : 'Thêm phòng'}
              </button>
              <button type="button" disabled={saving} onClick={clearForm} aria-label="Xóa nội dung biểu mẫu" title="Xóa nội dung biểu mẫu" className="px-4 py-2.5 rounded-lg border">
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
