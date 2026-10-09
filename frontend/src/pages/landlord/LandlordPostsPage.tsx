import { useEffect, useState } from 'react';
import { toast } from 'react-toastify';
import { FiEdit2, FiEye, FiPlus, FiTrash2, FiFileText, FiSearch, FiRefreshCw } from 'react-icons/fi';
import { postService } from '../../services/postService';
import { PostListItem, PostStatus, RoomStatus, type Post } from '../../types/post.types';
import { getApiErrorMessage } from '../../utils/apiError';
import LandlordModal from '../../components/landlord/LandlordModal';
import useLandlordAction from '../../components/landlord/useLandlordAction';
import { PostEditor } from './CreatePostPage';
import RoomLocation from '../../components/room/RoomLocation';
import { Link } from 'react-router-dom';

const postLabels = ['Chờ duyệt', 'Đã duyệt', 'Bị từ chối', 'Đã ẩn', 'Hết hạn'];
const roomLabels = ['Còn trống', 'Đã cho thuê', 'Đang giữ chỗ', 'Tạm ngưng'];

export default function LandlordPostsPage() {
  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [query, setQuery] = useState('');
  const [filter, setFilter] = useState('');
  const [editor, setEditor] = useState<{ id?: string; confirmation?: boolean } | null>(null);
  const [editorBusy, setEditorBusy] = useState(false);
  const [detail, setDetail] = useState<Post | null>(null);
  const { openAction, dialog } = useLandlordAction();
  async function loadPosts() {
    setLoading(true);
    try {
      const response = await postService.getMyPosts();
      setPosts(response.data || []);
    } catch (error) { toast.error(getApiErrorMessage(error, 'Không thể tải danh sách tin đăng')); }
    finally { setLoading(false); }
  }
  useEffect(() => { void Promise.resolve().then(loadPosts); }, []);
  const handleDelete = async (id: number) => {
    try {
      await postService.deletePost(id);
      toast.success('Đã xóa tin đăng');
      await loadPosts();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể xóa tin đăng'));
      return false;
    }
  };
  const handleView = async (post: PostListItem) => {
    if (post.status !== PostStatus.Approved || post.roomStatus !== RoomStatus.Available) {
      toast.info(post.status !== PostStatus.Approved
        ? 'Tin chưa được công khai. Chỉ tin đã được quản trị viên duyệt mới xuất hiện cho người thuê.'
        : 'Tin đang tạm ẩn với người thuê vì phòng không còn ở trạng thái còn trống.');
      return;
    }
    try { setDetail((await postService.getMyPostById(post.id)).data); }
    catch (error) { toast.error(getApiErrorMessage(error, 'Không thể tải chi tiết tin đăng.')); }
  };
  const handleEdit = (post: PostListItem) => {
    setEditor({ id: String(post.id), confirmation: post.status === PostStatus.Approved });
  };
  const visible = posts.filter(post => (filter === '' || post.status === Number(filter))
    && [post.title, post.address, post.district, post.province].some(value => value?.toLowerCase().includes(query.trim().toLowerCase())));
  const closeEditor = () => { if (!editorBusy) setEditor(null); };
  return <div className="landlord-posts-page">
    <header className="landlord-page-heading">
      <div><p>QUẢN LÝ</p><h1>Tin đăng của tôi</h1></div>
      <button className="landlord-button primary" onClick={() => setEditor({})}><FiPlus />Đăng tin mới</button>
    </header>
    <div className="landlord-metrics">
      {[['Tổng tin đăng', posts.length], ['Chờ duyệt', posts.filter(p => p.status === PostStatus.Pending).length], ['Đã duyệt', posts.filter(p => p.status === PostStatus.Approved).length], ['Bị từ chối', posts.filter(p => p.status === PostStatus.Rejected).length]].map(([label, count]) =>
        <div key={label}><span>{label}</span><strong>{count}</strong></div>)}
    </div>
    <div className="landlord-toolbar">
      <label className="landlord-search"><FiSearch /><input aria-label="Tìm tin đăng" placeholder="Tìm tin đăng..." value={query} onChange={e => setQuery(e.target.value)} /></label>
      <select aria-label="Trạng thái tin đăng" value={filter} onChange={e => setFilter(e.target.value)}><option value="">Tất cả trạng thái</option>{postLabels.map((label, index) => <option key={label} value={index}>{label}</option>)}</select>
      <button className="landlord-icon-button" aria-label="Tải lại tin đăng" title="Tải lại tin đăng" onClick={loadPosts}><FiRefreshCw /></button>
    </div>
    <div className="landlord-table-wrap">
      <table className="landlord-table"><thead><tr><th>Tin đăng</th><th>Giá thuê / diện tích</th><th>Trạng thái tin</th><th>Phòng</th><th>Thao tác</th></tr></thead>
        <tbody>{visible.map(post => <tr key={post.id}>
          <td><div className="landlord-post-cell">{post.thumbnailUrl ? <img src={post.thumbnailUrl} alt={post.title} /> : <span className="landlord-thumbnail"><FiFileText /></span>}<div><strong>{post.title}</strong><small>{[post.address, post.ward, post.district, post.province].filter(Boolean).join(', ')}</small><small>{post.categoryName} · {post.maxOccupants} người · {new Date(post.createdAt).toLocaleDateString('vi-VN')}</small></div></div></td>
          <td><strong>{post.price.toLocaleString('vi-VN')} đ/tháng</strong><small>{post.area} m²</small></td>
          <td><span className={`landlord-status status-${post.status}`}>{postLabels[post.status] || 'Không rõ'}</span></td>
          <td>{roomLabels[post.roomStatus] || 'Không rõ'}</td>
          <td><div className="landlord-row-actions">
            <button className="landlord-icon-button" title="Xem chi tiết" aria-label={`Xem chi tiết ${post.title}`} onClick={() => handleView(post)}><FiEye /></button>
            <button className="landlord-icon-button" title="Chỉnh sửa" aria-label={`Chỉnh sửa ${post.title}`} onClick={() => handleEdit(post)}><FiEdit2 /></button>
            <button className="landlord-icon-button danger" title="Xóa tin đăng" aria-label={`Xóa ${post.title}`} onClick={() => openAction({ title: 'Xóa tin đăng', message: 'Bạn có chắc muốn xóa tin đăng "' + post.title + '"?', run: () => handleDelete(post.id) })}><FiTrash2 /></button>
          </div></td>
        </tr>)}</tbody>
      </table>
      {loading ? <p className="landlord-empty" role="status">Đang tải danh sách tin đăng...</p> : !visible.length && <div className="landlord-empty"><FiFileText /><p>{posts.length ? 'Không có tin đăng phù hợp.' : 'Bạn chưa có tin đăng nào'}</p>{!posts.length && <button className="landlord-button primary" onClick={() => setEditor({})}><FiPlus />Đăng tin ngay</button>}</div>}
    </div>
    <LandlordModal isOpen={!!editor} onClose={closeEditor} title={editor?.id ? 'Chỉnh sửa tin đăng' : 'Đăng tin cho thuê'} size="xl">
      {editor?.confirmation ? <div className="space-y-4"><p>Chỉnh sửa nội dung bài đăng sẽ đưa bài về trạng thái chờ duyệt. Bạn muốn tiếp tục?</p>
        <div className="landlord-modal-actions"><button className="landlord-button" onClick={closeEditor}>Hủy</button><button className="landlord-button primary" onClick={() => setEditor({ id: editor.id })}>Xác nhận</button></div>
      </div> : editor && <PostEditor key={editor.id || 'new'} postId={editor.id} onSaved={() => { setEditorBusy(false); setEditor(null); void loadPosts(); }} onCancel={closeEditor} onBusyChange={setEditorBusy} />}
    </LandlordModal>
    <LandlordModal isOpen={!!detail} onClose={() => setDetail(null)} title="Chi tiết tin đăng" size="xl">
      {detail && <div className="space-y-4"><h3 className="text-xl font-semibold">{detail.title}</h3><p>{[detail.address, detail.ward, detail.district, detail.province].filter(Boolean).join(', ')}</p>
        <dl className="grid grid-cols-2 sm:grid-cols-3 gap-4"><div><dt>Giá thuê</dt><dd>{detail.price.toLocaleString('vi-VN')} đ/tháng</dd></div><div><dt>Diện tích</dt><dd>{detail.area} m²</dd></div><div><dt>Số người tối đa</dt><dd>{detail.maxOccupants}</dd></div></dl>
        <div className="grid grid-cols-2 gap-3">{detail.imageUrls.map((url, i) => <img key={url + i} src={url} alt={detail.title} className="aspect-video w-full object-cover rounded-lg" />)}</div>
        <p className="whitespace-pre-wrap">{detail.description}</p><p>{detail.amenities.map(item => item.name).join(' · ')}</p>
        <RoomLocation address={detail.address} ward={detail.ward} district={detail.district} province={detail.province} latitude={detail.latitude} longitude={detail.longitude} title="Vị trí phòng" />
        <div className="landlord-modal-actions"><Link className="landlord-button" to={`/rooms/${detail.id}`}>Xem tin công khai</Link><button className="landlord-button" onClick={() => setDetail(null)}>Đóng</button></div>
      </div>}
    </LandlordModal>
    {dialog}
  </div>;
}
