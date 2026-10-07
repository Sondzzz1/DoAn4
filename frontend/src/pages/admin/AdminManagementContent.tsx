import React, { useCallback, useEffect, useRef, useState } from 'react';
import { FiCheck, FiChevronLeft, FiChevronRight, FiEdit2, FiFileText, FiPlus, FiRefreshCw, FiSearch, FiTrash2, FiUnlock, FiUser, FiX } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { useAuth } from '../../hooks/useAuth';
import { adminService, AdminCatalogItem, AdminReport, AdminRoom, AdminUser } from '../../services/adminService';
import { PostListItem, PostStatus } from '../../types/post.types';
import Modal from '../../components/common/Modal';
import { getApiErrorMessage } from '../../utils/apiError';
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
  const [formError, setFormError] = useState('');
  const [busy, setBusy] = useState(false);
  const busyRef = useRef(false);
  const [rejectId, setRejectId] = useState<number | null>(null);
  const [reason, setReason] = useState('');
  const [page, setPage] = useState(1);

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

  useEffect(() => {
    void Promise.resolve().then(load);
  }, [load]);

  const runAction = async (action: () => Promise<unknown>, message: string) => {
    if (busyRef.current) return;
    busyRef.current = true;
    setBusy(true);
    try { await action(); toast.success(message); await load(); }
    catch (e) { toast.error(getApiErrorMessage(e, 'Không thể hoàn tất thao tác.')); }
    finally { busyRef.current = false; setBusy(false); }
  };

  const handlePost = async (id: number, action: 'approve' | 'reject' | 'hide') => {
    if (action === 'approve') {
      await runAction(() => adminService.approvePost(id), 'Đã duyệt tin đăng.');
    } else if (action === 'reject') {
      setReason(''); setFormError(''); setRejectId(id);
    } else {
      await runAction(() => adminService.hidePost(id), 'Đã ẩn tin đăng.');
    }
  };
  const handleUser = async (item: AdminUser) => {
    await runAction(() => item.isActive ? adminService.lockUser(item.id) : adminService.unlockUser(item.id), item.isActive ? 'Đã khóa tài khoản.' : 'Đã mở khóa tài khoản.');
  };
  const handleCatalog = async (event: React.FormEvent) => {
    event.preventDefault();
    if (busyRef.current) return;
    if (!form.name.trim()) { setFormError('Vui lòng nhập tên.'); return; }
    busyRef.current = true; setBusy(true); setFormError('');
    const data = module === 'amenities' ? { name: form.name.trim(), description: form.description.trim(), icon: form.icon.trim() } : { name: form.name.trim(), description: form.description.trim() };
    try {
      if (module === 'amenities') {
        if (form.id) await adminService.updateAmenity(form.id, data);
        else await adminService.createAmenity(data);
      } else if (form.id) await adminService.updateCategory(form.id, data);
      else await adminService.createCategory(data);
      setModal(null); setForm({ name: '', description: '', icon: '' }); toast.success('Đã lưu thay đổi.'); await load();
    } catch (e) { setFormError(getApiErrorMessage(e, 'Không thể lưu. Vui lòng thử lại.')); }
    finally { busyRef.current = false; setBusy(false); }
  };
  const editCatalog = (item: AdminCatalogItem) => { setFormError(''); setForm({ id: item.id, name: item.name, description: item.description || '', icon: item.icon || '' }); setModal({ id: item.id, type: module === 'amenities' ? 'amenity' : 'category' }); };
  const deleteCatalog = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn xóa mục này?')) return;
    await runAction(() => module === 'amenities' ? adminService.deleteAmenity(id) : adminService.deleteCategory(id), 'Đã xóa mục.');
  };

  const current = config[module];
  const canCreate = !isLandlord && (module === 'categories' || module === 'amenities');
  const filteredRows = module === 'categories' || module === 'amenities'
    ? rows.filter(item => `${(item as AdminCatalogItem).name} ${(item as AdminCatalogItem).description || ''}`.toLocaleLowerCase('vi').includes(query.trim().toLocaleLowerCase('vi')))
    : rows;
  const pageCount = Math.max(1, Math.ceil(filteredRows.length / 10));
  const currentPage = Math.min(page, pageCount);
  const pageRows = filteredRows.slice((currentPage - 1) * 10, currentPage * 10);

  return (
    <div className="admin-management-content">
      <div className="admin-content-heading">
        <div>
          <p className="admin-content-eyebrow">{current.eyebrow}</p>
          <h1>{current.title}</h1>
          <span>{current.description}</span>
        </div>
        {canCreate && (
          <button className="admin-content-primary" disabled={busy} onClick={() => { setFormError(''); setForm({ name: '', description: '', icon: '' }); setModal({ type: module === 'amenities' ? 'amenity' : 'category' }); }}>
            <FiPlus /> Thêm mới
          </button>
        )}
      </div>

      <div className="admin-content-toolbar">
        <label>
          <FiSearch />
          <input aria-label="Tìm kiếm dữ liệu" value={query} onChange={e => { setQuery(e.target.value); setPage(1); }} placeholder={module === 'users' ? 'Tìm theo tên hoặc email...' : 'Tìm kiếm dữ liệu...'} />
        </label>
        {(module === 'posts' || module === 'rooms' || module === 'reports') && (
          <select aria-label="Lọc trạng thái" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}>
            <option value="">Tất cả trạng thái</option>
            {module === 'posts' && <><option value="0">Chờ duyệt</option><option value="1">Đã duyệt</option><option value="2">Từ chối</option><option value="3">Đã ẩn</option></>}
            {module === 'rooms' && <><option value="0">Còn trống</option><option value="1">Đã thuê</option><option value="3">Tạm ngưng</option></>}
            {module === 'reports' && <><option value="0">Mới tiếp nhận</option><option value="1">Đang xử lý</option><option value="2">Đã xử lý</option></>}
          </select>
        )}
        <button className="admin-content-refresh" title="Làm mới dữ liệu" aria-label="Làm mới dữ liệu" disabled={loading || busy} onClick={() => void load()}><FiRefreshCw /></button>
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
                {pageRows.map(item => (
                  <tr key={item.id}>
                    {module === 'users' && <><td><div className="cell-person"><span><FiUser /></span><div><b>{(item as AdminUser).fullName}</b><small>{(item as AdminUser).email}</small></div></div></td><td>{(item as AdminUser).roleName}</td><td>{(item as AdminUser).postCount} tin</td><td><em className={(item as AdminUser).isActive ? 'green' : 'red'}>{(item as AdminUser).isActive ? 'Đang hoạt động' : 'Đã khóa'}</em></td><td><button className="table-action" disabled={busy} title={(item as AdminUser).isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'} aria-label={(item as AdminUser).isActive ? 'Khóa tài khoản' : 'Mở khóa tài khoản'} onClick={() => handleUser(item as AdminUser)}>{(item as AdminUser).isActive ? <FiX /> : <FiUnlock />}</button></td></>}
                    {(module === 'posts' || module === 'approval') && <><td><b>{(item as PostListItem).title}</b><small className="table-sub">{(item as PostListItem).address || `${(item as PostListItem).province}, ${(item as PostListItem).district}`}</small></td><td>{(item as PostListItem).landlordName || 'Chưa cập nhật'}</td><td>{formatMoney((item as PostListItem).price)}</td><td><em className={`status-${(item as PostListItem).status}`}>{postStatus((item as PostListItem).status)}</em></td><td className="table-actions">{(item as PostListItem).status === 0 && <><button className="table-action approve" disabled={busy} title="Duyệt tin đăng" aria-label="Duyệt tin đăng" onClick={() => handlePost(item.id, 'approve')}><FiCheck /></button><button className="table-action danger" disabled={busy} title="Từ chối tin đăng" aria-label="Từ chối tin đăng" onClick={() => handlePost(item.id, 'reject')}><FiX /></button></>}{module === 'posts' && <button className="table-action" disabled={busy} title="Ẩn tin đăng" aria-label="Ẩn tin đăng" onClick={() => handlePost(item.id, 'hide')}><FiX /></button>}</td></>}
                    {module === 'rooms' && <><td><b>{(item as AdminRoom).roomName}</b><small className="table-sub">{(item as AdminRoom).address}</small></td><td>{(item as AdminRoom).landlordName}</td><td>{formatMoney((item as AdminRoom).price)}</td><td><em className={`status-${(item as AdminRoom).status}`}>{roomStatus((item as AdminRoom).status)}</em></td><td /></>}
                    {(module === 'categories' || module === 'amenities') && <><td><b>{(item as AdminCatalogItem).icon} {(item as AdminCatalogItem).name}</b></td><td>{(item as AdminCatalogItem).description || 'Chưa có mô tả'}</td><td><em className={(item as AdminCatalogItem).isActive === false ? 'red' : 'green'}>{(item as AdminCatalogItem).isActive === false ? 'Đã ẩn' : 'Đang dùng'}</em></td><td className="table-actions"><button className="table-action" disabled={busy} title="Chỉnh sửa" aria-label="Chỉnh sửa" onClick={() => editCatalog(item as AdminCatalogItem)}><FiEdit2 /></button><button className="table-action danger" disabled={busy} title="Xóa mục" aria-label="Xóa mục" onClick={() => deleteCatalog(item.id)}><FiTrash2 /></button></td></>}
                    {module === 'reports' && <><td><b>{(item as AdminReport).postTitle || 'Báo cáo nội dung'}</b><small className="table-sub">{(item as AdminReport).reason || (item as AdminReport).description || 'Không có mô tả'}</small></td><td>{(item as AdminReport).reporterName || 'Ẩn danh'}</td><td>{formatDate((item as AdminReport).createdAt)}</td><td><em className={`status-${(item as AdminReport).status || 0}`}>{(item as AdminReport).statusText || 'Mới tiếp nhận'}</em></td><td><select className="inline-select" aria-label="Cập nhật trạng thái báo cáo" disabled={busy} value={(item as AdminReport).status || 0} onChange={e => { const nextStatus = Number(e.target.value); void runAction(() => adminService.updateReportStatus(item.id, nextStatus), 'Đã cập nhật báo cáo.'); }}><option value="0">Mới</option><option value="1">Đang xử lý</option><option value="2">Đã xử lý</option></select></td></>}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {!loading && filteredRows.length > 0 && <nav className="admin-pagination" aria-label="Phân trang dữ liệu">
        <span>{(currentPage - 1) * 10 + 1}–{Math.min(currentPage * 10, filteredRows.length)} / {filteredRows.length} mục</span>
        <div><button aria-label="Trang trước" title="Trang trước" disabled={currentPage === 1} onClick={() => setPage(currentPage - 1)}><FiChevronLeft /></button><span>Trang {currentPage} / {pageCount}</span><button aria-label="Trang sau" title="Trang sau" disabled={currentPage === pageCount} onClick={() => setPage(currentPage + 1)}><FiChevronRight /></button></div>
      </nav>}

      <Modal isOpen={!!modal} onClose={() => { if (!busy) setModal(null); }} title={`${modal?.id ? 'Chỉnh sửa' : 'Thêm'} ${modal?.type === 'amenity' ? 'tiện ích' : 'danh mục'}`}>
        <form className="admin-catalog-form" onSubmit={handleCatalog}>
          <label>Tên <span className="text-red-600">*</span><input required maxLength={100} value={form.name} onChange={e => setForm({ ...form, name: e.target.value })} placeholder="Nhập tên" aria-invalid={!!formError} aria-describedby={formError ? 'catalog-error' : undefined} /></label>
          {modal?.type === 'amenity' && <label>Biểu tượng<input value={form.icon} onChange={e => setForm({ ...form, icon: e.target.value })} placeholder="Ví dụ: wifi" /></label>}
          <label>Mô tả<textarea rows={4} value={form.description} onChange={e => setForm({ ...form, description: e.target.value })} placeholder="Nhập mô tả" /></label>
          {formError && <p id="catalog-error" role="alert" className="form-error">{formError}</p>}
          <div className="form-actions"><button type="button" disabled={busy} onClick={() => setModal(null)}>Hủy</button><button className="admin-content-primary" disabled={busy} type="submit"><FiCheck />{busy ? 'Đang lưu...' : 'Lưu thay đổi'}</button></div>
        </form>
      </Modal>
      <Modal isOpen={rejectId !== null} onClose={() => { if (!busy) setRejectId(null); }} title="Từ chối tin đăng">
        <form className="admin-catalog-form" onSubmit={async e => {
          e.preventDefault();
          if (busyRef.current || rejectId === null) return;
          if (!reason.trim()) { setFormError('Vui lòng nhập lý do từ chối.'); return; }
          busyRef.current = true; setBusy(true); setFormError('');
          try { await adminService.rejectPost(rejectId, reason.trim()); setRejectId(null); toast.success('Đã từ chối tin đăng.'); await load(); }
          catch (error) { setFormError(getApiErrorMessage(error, 'Không thể từ chối tin đăng.')); }
          finally { busyRef.current = false; setBusy(false); }
        }}>
          <label>Lý do từ chối <span className="text-red-600">*</span><textarea required rows={4} maxLength={500} value={reason} onChange={e => setReason(e.target.value)} placeholder="Nêu thông tin cần chỉnh sửa để chủ trọ gửi duyệt lại" /></label>
          {formError && <p role="alert" className="form-error">{formError}</p>}
          <div className="form-actions"><button type="button" disabled={busy} onClick={() => setRejectId(null)}>Hủy</button><button type="submit" disabled={busy} className="admin-content-primary">{busy ? 'Đang xử lý...' : 'Xác nhận từ chối'}</button></div>
        </form>
      </Modal>
    </div>
  );
};

export default AdminManagementContent;
