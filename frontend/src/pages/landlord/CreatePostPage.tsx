import { useEffect, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { FiCheck, FiHome, FiX } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { roomService } from '../../services/roomService';
import { postService } from '../../services/postService';
import { RoomStatus } from '../../types/post.types';
import type { RoomItem } from '../../types/room.types';
import { getApiErrorMessage } from '../../utils/apiError';
import LandlordModal from '../../components/landlord/LandlordModal';

export function PostEditor({ postId, onSaved, onCancel, onBusyChange }: { postId?: string; onSaved?: () => void; onCancel?: () => void; onBusyChange?: (busy: boolean) => void }) {
  const id = postId;
  const navigate = useNavigate();
  const [rooms, setRooms] = useState<RoomItem[]>([]);
  const [roomId, setRoomId] = useState(0);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  useEffect(() => { onBusyChange?.(saving); }, [saving, onBusyChange]);
  useEffect(() => {
    let active = true;
    void (async () => {
      try {
        const response = await roomService.getMyRooms();
        if (!active) return;
        setRooms(response.data || []);
        if (id) {
          const post = (await postService.getMyPostById(Number(id))).data;
          if (!active) return;
          setRoomId(post.roomId);
          setTitle(post.title);
          setDescription(post.description);
        }
      } catch (e) {
        if (active) setError(getApiErrorMessage(e, 'Không tải được thông tin phòng.'));
      } finally {
        if (active) setLoading(false);
      }
    })();
    return () => { active = false; };
  }, [id]);
  const selected = rooms.find(r => r.id === roomId);
  const candidates = rooms.filter(r => r.status === RoomStatus.Available && !r.activePostId);
  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (saving || loading) return;
    if (!roomId || !title.trim() || !description.trim()) {
      setError('Vui lòng chọn phòng và nhập đầy đủ tiêu đề, nội dung.');
      return;
    }
    setSaving(true);
    setError('');
    try {
      const data = { roomId, title: title.trim(), description: description.trim() };
      if (id) await postService.updatePost(Number(id), { title: data.title, description: data.description });
      else await postService.createPost(data);
      toast.success(id ? 'Đã cập nhật tin đăng.' : 'Đã gửi tin chờ duyệt.');
      if (onSaved) onSaved();
      else navigate('/landlord/posts');
    } catch (e) {
      setError(getApiErrorMessage(e, 'Không lưu được tin đăng.'));
    } finally { setSaving(false); }
  };
  return (
    <div className="post-editor-page">
      <div>
        {loading ? <p role="status">Đang tải phòng...</p> : (
          <form onSubmit={submit} className="space-y-6">
            <div>
              <label htmlFor="post-room" className="block font-semibold mb-2">Phòng trọ</label>
              <select id="post-room" value={roomId} disabled={!!id || saving} onChange={e => {
                const value = Number(e.target.value);
                setRoomId(value);
                const room = rooms.find(r => r.id === value);
                if (room) { setTitle(room.roomName); setDescription(room.description || ''); }
              }} className="w-full border border-slate-300 rounded-lg p-3 bg-white" required>
                <option value={0}>Chọn phòng còn trống</option>
                {(id ? rooms.filter(r => r.id === roomId) : candidates).map(r =>
                  <option key={r.id} value={r.id}>{r.roomName} · {r.price.toLocaleString('vi-VN')} đ/tháng</option>)}
              </select>
              {!id && !candidates.length && <p className="mt-3 text-slate-600">Chưa có phòng đủ điều kiện đăng tin.</p>}
              <Link to="/landlord/rooms" className="inline-flex items-center gap-2 mt-3 text-blue-700"><FiHome />Quản lý phòng</Link>
            </div>
            {selected && <div className="border-y border-slate-200 py-4 space-y-3">
              <h2 className="text-lg font-semibold">{selected.roomName}</h2>
              <p className="text-slate-600">{[selected.address, selected.ward, selected.district, selected.province].filter(Boolean).join(', ')}</p>
              <dl className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-sm">
                <div><dt>Giá thuê</dt><dd className="font-semibold">{selected.price.toLocaleString('vi-VN')} đ/tháng</dd></div>
                <div><dt>Diện tích</dt><dd>{selected.area} m²</dd></div>
                <div><dt>Số người tối đa</dt><dd>{selected.maxOccupants}</dd></div>
              </dl>
              {!!selected.imageUrls.length && <div className="grid grid-cols-2 sm:grid-cols-4 gap-3">
                {selected.imageUrls.map((url, i) => <img key={url + i} src={url} alt={selected.roomName} className="w-full aspect-video object-cover rounded-lg" />)}
              </div>}
            </div>}
            <div>
              <label htmlFor="post-title" className="block font-semibold mb-2">Tiêu đề tin đăng</label>
              <input id="post-title" value={title} onChange={e => setTitle(e.target.value)} maxLength={300} required className="w-full border border-slate-300 rounded-lg p-3" />
            </div>
            <div>
              <label htmlFor="post-description" className="block font-semibold mb-2">Nội dung tin đăng</label>
              <textarea id="post-description" value={description} onChange={e => setDescription(e.target.value)} required rows={7} className="w-full border border-slate-300 rounded-lg p-3" />
            </div>
            {error && <p role="alert" className="form-error">{error}</p>}
            <div className="flex flex-wrap gap-3">
              <button type="submit" disabled={saving || (!id && !candidates.length)} className="inline-flex items-center gap-2 bg-blue-600 text-white rounded-lg px-5 py-3 disabled:opacity-50"><FiCheck />{saving ? 'Đang lưu...' : id ? 'Lưu tin đăng' : 'Gửi duyệt'}</button>
              <button type="button" disabled={saving} onClick={() => onCancel ? onCancel() : navigate('/landlord/posts')} className="inline-flex items-center gap-2 border border-slate-300 rounded-lg px-5 py-3"><FiX />Hủy</button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}

export default function CreatePostPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const [busy, setBusy] = useState(false);
  const close = () => { if (!busy) navigate('/landlord/posts'); };
  return <LandlordModal isOpen onClose={close} title={id ? 'Chỉnh sửa tin đăng' : 'Đăng tin cho thuê'} size="xl">
    <PostEditor postId={id} onSaved={() => navigate('/landlord/posts')} onCancel={close} onBusyChange={setBusy} />
  </LandlordModal>;
}
