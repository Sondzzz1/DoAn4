import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { FiEdit2, FiEye, FiPlus, FiTrash2 } from 'react-icons/fi';
import { postService } from '../../services/postService';
import { PostListItem, PostStatus } from '../../types/post.types';
import { ROUTES } from '../../utils/constants';
import './LandlordPages.css';

const statusLabel = (status: number) => ['Chờ duyệt', 'Đã duyệt', 'Từ chối', 'Đã ẩn', 'Hết hạn'][status] || 'Không rõ';

const LandlordPostsPage: React.FC = () => {
  const navigate = useNavigate();
  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState(true);

  const load = async () => {
    try {
      const response = await postService.getMyPosts();
      setPosts(response.data ?? []);
    } catch (error) {
      console.error('Không thể tải tin đăng của chủ trọ:', error);
      setPosts([]);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleDelete = async (id: number) => {
    if (!window.confirm('Bạn có chắc muốn ẩn tin đăng này?')) return;

    try {
      await postService.deletePost(id);
      await load();
    } catch (error) {
      console.error('Xóa tin đăng thất bại:', error);
    }
  };

  const handleStatusUpdate = async (id: number, status: PostStatus) => {
    try {
      await postService.updatePostStatus(id, status);
      await load();
    } catch (error) {
      console.error('Cập nhật trạng thái thất bại:', error);
    }
  };

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>Quản lý tin đăng</h1>
            <p>Theo dõi và cập nhật các phòng trọ đang đăng trên hệ thống.</p>
          </div>
          <div className="landlord-actions">
            <button className="landlord-btn" onClick={() => navigate(ROUTES.LANDLORD_CREATE_POST)}>
              <FiPlus style={{ marginRight: 8 }} /> Đăng tin mới
            </button>
          </div>
        </div>

        {loading ? (
          <div className="empty-state">Đang tải danh sách tin đăng...</div>
        ) : posts.length === 0 ? (
          <div className="empty-state">
            <div>
              <h3>Chưa có tin đăng nào</h3>
              <p>Hãy tạo một tin mới để bắt đầu cho thuê phòng.</p>
            </div>
          </div>
        ) : (
          <div className="landlord-grid">
            {posts.map((post) => (
              <article key={post.id} className="landlord-card">
                <div className="landlord-card-header">
                  <span className={`status-pill status-${post.status}`}>{statusLabel(post.status)}</span>
                  <span className="landlord-price">{new Intl.NumberFormat('vi-VN').format(post.price)}đ</span>
                </div>

                <div>
                  <h3>{post.title}</h3>
                  <p style={{ color: '#64748b', marginTop: 6 }}>{post.address || `${post.province}, ${post.district}`}</p>
                </div>

                <div className="landlord-meta">
                  <div className="meta-item">
                    <small>Diện tích</small>
                    <strong>{post.area} m²</strong>
                  </div>
                  <div className="meta-item">
                    <small>Max người</small>
                    <strong>{post.maxOccupants} người</strong>
                  </div>
                  <div className="meta-item">
                    <small>Trạng thái phòng</small>
                    <strong>{['Còn trống', 'Đã thuê', 'Tạm ngưng'][post.roomStatus] || 'Không rõ'}</strong>
                  </div>
                  <div className="meta-item">
                    <small>Cập nhật</small>
                    <strong>{new Intl.DateTimeFormat('vi-VN').format(new Date(post.updatedAt))}</strong>
                  </div>
                </div>

                <div className="landlord-card-actions">
                  <button className="landlord-btn-secondary" onClick={() => navigate(`/landlord/posts/${post.id}/edit`)}>
                    <FiEdit2 style={{ marginRight: 6 }} /> Sửa
                  </button>
                  <button className="landlord-btn-ghost" onClick={() => navigate(`/rooms/${post.id}`)}>
                    <FiEye style={{ marginRight: 6 }} /> Xem
                  </button>
                  <button className="landlord-btn-danger" onClick={() => handleDelete(post.id)}>
                    <FiTrash2 style={{ marginRight: 6 }} /> Ẩn
                  </button>
                </div>

                {post.status === PostStatus.Pending && (
                  <div className="landlord-card-actions">
                    <button className="landlord-btn-secondary" onClick={() => handleStatusUpdate(post.id, PostStatus.Approved)}>
                      Duyệt tin
                    </button>
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

export default LandlordPostsPage;
