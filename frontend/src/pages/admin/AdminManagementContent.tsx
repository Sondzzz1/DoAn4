import React, { useCallback, useEffect, useState } from 'react';
import { FiActivity, FiCheck, FiCheckCircle, FiEdit2, FiFileText, FiHome, FiPlus, FiSearch, FiSettings, FiTag, FiTrash2, FiUnlock, FiUser, FiUsers, FiX } from 'react-icons/fi';
import { useAuth } from '../../hooks/useAuth';
import { adminService, AdminCatalogItem, AdminReport, AdminRoom, AdminUser } from '../../services/adminService';
import { PostListItem, PostStatus } from '../../types/post.types';
import './AdminManagementContent.css';

type AdminModule = 'users' | 'posts' | 'approval' | 'rooms' | 'categories' | 'amenities' | 'reports';
type Row = AdminUser | PostListItem | AdminRoom | AdminCatalogItem | AdminReport;
type CatalogForm = { id?: number; name: string; description: string; icon: string };

const config: Record<AdminModule, { title: string; eyebrow: string; description: string }> = {
  users: { title: 'Người dùng', eyebrow: 'TÀI KHOẢN HỆ THỐNG', description: 'Theo dõi quyền truy cập và tình trạng hoạt động của tài khoản.' },
  posts: { title: 'Tin đăng', eyebrow: 'NỘI DUNG HỆ THỐNG', description: 'Kiểm soát toàn bộ tin đăng và chất lượng nội dung trên nền tảng.' },
  approval: { title: 'Duyệt tin đăng', eyebrow: 'KIỂM DUYỆT', description: 'Xử lý các tin mới gửi trước khi hiển thị công khai.' },
  rooms: { title: 'Phòng trọ', eyebrow: 'TÀI SẢN ĐĂNG KÝ', description: 'Xem nhanh danh sách phòng và trạng thái vận hành.' },
  categories: { title: 'Danh mục', eyebrow: 'CẤU HÌNH HỆ THỐNG', description: 'Tổ chức các nhóm phòng để người thuê tìm kiếm dễ hơn.' },
  amenities: { title: 'Tiện ích', eyebrow: 'CẤU HÌNH HỆ THỐNG', description: 'Quản lý các tiện ích được gắn vào phòng trọ.' },
  reports: { title: 'Báo cáo vi phạm', eyebrow: 'AN TOÀN NỘI DUNG', description: 'Tiếp nhận và xử lý phản ánh từ cộng đồng.' },
};

const formatMoney = (value: number) => `${new Intl.NumberFormat('vi-VN').format(value || 0)} đ`;
const formatDate = (value?: string) => value ? new Intl.DateTimeFormat('vi-VN').format(new Date(value)) : 'Chưa cập nhật';
const postStatus = (status: number) => ['Chờ duyệt', 'Đã duyệt', 'Từ chối', 'Đã ẩn', 'Hết hạn'][status] || 'Không rõ';
const roomStatus = (status: number) => ['Còn trống', 'Đã thuê', 'Đã đặt', 'Tạm ngưng'][status] || 'Không rõ';

const AdminManagementContent: React.FC<{ module: AdminModule }> = ({ module }) => {
  const { isLandlord } = useAuth();
  const [rows, setRows] = useState<Row[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [query, setQuery] = useState('');
  const [status, setStatus] = useState('');
  const [modal, setModal] = useState<{ id?: number; type: 'category' | 'amenity' } | null>(null);
  const [form, setForm] = useState<CatalogForm>({ name: '', description: '', icon: '' });

  const load = useCallback(async () => {
    setLoading(true); setError('');
    try {
      let data: Row[] = [];
      if (module === 'users') data = (await adminService.getUsers(query)).data;
      if (module === 'posts' || module === 'approval') data = (await adminService.getPosts(query, module === 'approval' ? PostStatus.Pending : status ? Number(status) : undefined)).data;
      if (module === 'rooms') data = (await adminService.getRooms(query, status ? Number(status) : undefined)).data;
      if (module === 'categories') data = (await adminService.getCategories()).data;
      if (module === 'amenities') data = (await adminService.getAmenities()).data;
      if (module === 'reports') data = (await adminService.getReports(status ? Number(status) : undefined)).data;
      setRows(data || []);
    } catch { setError('Không thể tải dữ liệu. Vui lòng thử lại sau.'); setRows([]); }
    finally { setLoading(false); }
  }, [module, query, status]);

  useEffect(() => { void load(); }, [load]);

  const handlePost = async (id: number, action: 'approve' | 'hide') => {
    if (action === 'approve') await adminService.approvePost(id);
    else await adminService.hidePost(id);
    load();
  };
  const handleUser = async (item: AdminUser) => {
    if (item.isActive) await adminService.lockUser(item.id);
    else await adminService.unlockUser(item.id);
    load();
  };
  const handleCatalog = async (event: React.FormEvent) => {
    event.preventDefault();
    const data = module === 'amenities' ? { name: form.name, description: form.description, icon: form.icon } : { name: form.name, description: form.description };
    if (module === 'amenities') {
      if (form.id) await adminService.updateAmenity(form.id, data);
      else await adminService.createAmenity(data);
    } else if (form.id) await adminService.updateCategory(form.id, data);
    else await adminService.createCategory(data);
    setModal(null); setForm({ name: '', description: '', icon: '' }); load();
  };
  const editCatalog = (item: AdminCatalogItem) => { setForm({ id: item.id, name: item.name, description: item.description || '', icon: item.icon || '' }); setModal({ id: item.id, type: module as 'category' | 'amenity' }); };
  const deleteCatalog = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn xóa mục này?')) return;
    if (module === 'amenities') await adminService.deleteAmenity(id);
    else await adminService.deleteCategory(id);
    load();
  };

  const current = config[module];
  const canCreate = !isLandlord && (module === 'categories' || module === 'amenities');
  const filteredRows = rows;

  return (
    <div className="admin-management-content">
      <div className="admin-content-heading">
        <div>
          <p className="admin-content-eyebrow">{current.eyebrow}</p>
          <h1>{current.title}</h1>
          <span>{current.description}</span>
        </div>
        {canCreate && (
          <button className="admin-content-primary" onClick={() => { setForm({ name: '', description: '', icon: '' }); setModal({ type: module === 'amenities' ? 'amenity' : 'category' }); }}>
            <FiPlus /> Thêm mới
          </button>
        )}
      </div>

      <div className="admin-content-toolbar">
        <label>
          <FiSearch />
          <input value={query} onChange={e => setQuery(e.target.value)} placeholder={module === 'users' ? 'Tìm theo tên hoặc email...' : 'Tìm kiếm dữ liệu...'} />
        </label>
        {(module === 'posts' || module === 'rooms' || module === 'reports') && (
          <select value={status} onChange={e => setStatus(e.target.value)}>
            <option value="">Tất cả trạng thái</option>
            {module === 'posts' && <><option value="0">Chờ duyệt</option><option value="1">Đã duyệt</option><option value="2">Từ chối</option><option value="3">Đã ẩn</option></>}
            {module === 'rooms' && <><option value="0">Còn trống</option><option value="1">Đã thuê</option><option value="3">Tạm ngưng</option></>}
            {module === 'reports' && <><option value="0">Mới tiếp nhận</option><option value="1">Đang xử lý</option><option value="2">Đã xử lý</option></>}
          </select>
        )}
        <button className="admin-content-refresh" onClick={load}>Làm mới</button>
      </div>

      <section className="admin-content-table-card">
        {error && <div className="admin-content-alert">{error}</div>}
        {loading ? (
          <div className="admin-content-empty">Đang tải dữ liệu...</div>
        ) : !filteredRows.length ? (
          <div className="admin-content-empty">
            <FiFileText />
            <b>Chưa có dữ liệu</b>
            <span>Thử thay đổi bộ lọc hoặc quay lại sau.</span>
          </div>
        ) : (
          <div className="admin-content-table-wrap">
            <table>
              <thead>
                <tr>
                  {module === 'users' && <><th>Người dùng</th><th>Vai trò</th><th>Tin đăng</th><th>Trạng thái</th><th /></>}
                  {(module === 'posts' || module === 'approval') && <><th>Tin đăng</th><th>Chủ trọ</th><th>Giá thuê</th><th>Trạng thái</th><th /></>}
                  {module === 'rooms' && <><th>Phòng</th><th>Chủ trọ</th><th>Giá thuê</th><th>Trạng thái</th><th /></>}
                  {(module === 'categories' || module === 'amenities') && <><th>Tên</th><th>Mô tả</th><th>Trạng thái</th><th /></>}
                  {module === 'reports' && <><th>Nội dung báo cáo</th><th>Người gửi</th><th>Ngày gửi</th><th>Trạng thái</th><th /></>}
                </tr>
              </thead>
              <tbody>
                {filteredRows.map(item => (
                  <tr key={item.id}>
                    {module === 'users' && <><td><div className="cell-person"><span><FiUser /></span><div><b>{(item as AdminUser).fullName}</b><small>{(item as AdminUser).email}</small></div></div></td><td>{(item as AdminUser).roleName}</td><td>{(item as AdminUser).postCount} tin</td><td><em className={(item as AdminUser).isActive ? 'green' : 'red'}>{(item as AdminUser).isActive ? 'Đang hoạt động' : 'Đã khóa'}</em></td><td><button className="table-action" onClick={() => handleUser(item as AdminUser)}>{(item as AdminUser).isActive ? <FiX /> : <FiUnlock />}</button></td></>}
                    {(module === 'posts' || module === 'approval') && <><td><b>{(item as PostListItem).title}</b><small className="table-sub">{(item as PostListItem).address || `${(item as PostListItem).province}, ${(item as PostListItem).district}`}</small></td><td>{(item as PostListItem).landlordName || 'Chưa cập nhật'}</td><td>{formatMoney((item as PostListItem).price)}</td><td><em className={`status-${(item as PostListItem).status}`}>{postStatus((item as PostListItem).status)}</em></td><td className="table-actions">{(item as PostListItem).status === 0 && <button className="table-action approve" onClick={() => handlePost(item.id, 'approve')}><FiCheck /></button>}{module === 'posts' && <button className="table-action" onClick={() => handlePost(item.id, 'hide')}><FiX /></button>}</td></>}
                    {module === 'rooms' && <><td><b>{(item as AdminRoom).roomName}</b><small className="table-sub">{(item as AdminRoom).address}</small></td><td>{(item as AdminRoom).landlordName}</td><td>{formatMoney((item as AdminRoom).price)}</td><td><em className={`status-${(item as AdminRoom).status}`}>{roomStatus((item as AdminRoom).status)}</em></td><td /></>}
                    {(module === 'categories' || module === 'amenities') && <><td><b>{(item as AdminCatalogItem).icon} {(item as AdminCatalogItem).name}</b></td><td>{(item as AdminCatalogItem).description || 'Chưa có mô tả'}</td><td><em className="green">{(item as AdminCatalogItem).isActive === false ? 'Đã ẩn' : 'Đang dùng'}</em></td><td className="table-actions"><button className="table-action" onClick={() => editCatalog(item as AdminCatalogItem)}><FiEdit2 /></button><button className="table-action danger" onClick={() => deleteCatalog(item.id)}><FiTrash2 /></button></td></>}
                    {module === 'reports' && <><td><b>{(item as AdminReport).postTitle || 'Báo cáo nội dung'}</b><small className="table-sub">{(item as AdminReport).reason || (item as AdminReport).description || 'Không có mô tả'}</small></td><td>{(item as AdminReport).reporterName || 'Ẩn danh'}</td><td>{formatDate((item as AdminReport).createdAt)}</td><td><em className={`status-${(item as AdminReport).status || 0}`}>{(item as AdminReport).statusText || 'Mới tiếp nhận'}</em></td><td><select className="inline-select" value={(item as AdminReport).status || 0} onChange={async e => { await adminService.updateReportStatus(item.id, Number(e.target.value)); load(); }}><option value="0">Mới</option><option value="1">Đang xử lý</option><option value="2">Đã xử lý</option></select></td></>}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {modal && (
        <div className="admin-content-modal-backdrop" onClick={() => setModal(null)}>
          <form className="admin-content-modal" onSubmit={handleCatalog} onClick={e => e.stopPropagation()}>
            <div>
              <h2>{modal.id ? 'Chỉnh sửa' : 'Thêm'} {modal.type === 'amenity' ? 'tiện ích' : 'danh mục'}</h2>
              <button type="button" onClick={() => setModal(null)}><FiX /></button>
            </div>
            <label>Tên<input required value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} /></label>
            {modal.type === 'amenity' && <label>Biểu tượng<input value={form.icon} onChange={e => setForm({ ...form, icon: e.target.value })} placeholder="Ví dụ: wifi" /></label>}
            <label>Mô tả<textarea value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} /></label>
            <button className="admin-content-primary" type="submit"><FiCheck /> Lưu thay đổi</button>
          </form>
        </div>
      )}
    </div>
  );
};

export default AdminManagementContent;
