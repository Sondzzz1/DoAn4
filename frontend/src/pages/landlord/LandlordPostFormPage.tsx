import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'react-toastify';
import { adminService } from '../../services/adminService';
import { postService } from '../../services/postService';
import { ROUTES } from '../../utils/constants';
import './LandlordPages.css';

interface FormState {
  title: string;
  description: string;
  price: string;
  area: string;
  maxOccupants: string;
  province: string;
  district: string;
  ward: string;
  address: string;
  amenityIds: number[];
}

const emptyForm: FormState = {
  title: '',
  description: '',
  price: '',
  area: '',
  maxOccupants: '2',
  province: '',
  district: '',
  ward: '',
  address: '',
  amenityIds: [],
};

const LandlordPostFormPage: React.FC = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = Boolean(id);
  const [loading, setLoading] = useState(isEdit);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [amenities, setAmenities] = useState<{ id: number; name: string }[]>([]);
  const [form, setForm] = useState<FormState>(emptyForm);

  useEffect(() => {
    const load = async () => {
      try {
        const [amenityResponse] = await Promise.all([adminService.getAmenities()]);
        setAmenities((amenityResponse.data ?? []).map((item) => ({ id: item.id, name: item.name })));

        if (isEdit && id) {
          const response = await postService.getPostById(Number(id));
          const post = response.data;
          if (post) {
            setForm({
              title: post.title,
              description: post.description,
              price: String(post.price),
              area: String(post.area),
              maxOccupants: String(post.maxOccupants),
              province: post.province,
              district: post.district,
              ward: post.ward,
              address: post.address,
              amenityIds: post.amenities.map((item) => item.id),
            });
          }
        }
      } catch (err) {
        console.error('Không thể tải dữ liệu bài đăng:', err);
        setError('Không thể tải dữ liệu bài đăng. Vui lòng thử lại.');
      } finally {
        setLoading(false);
      }
    };

    void load();
  }, [id, isEdit]);

  const toggleAmenity = (amenityId: number) => {
    setForm((current) => ({
      ...current,
      amenityIds: current.amenityIds.includes(amenityId)
        ? current.amenityIds.filter((id) => id !== amenityId)
        : [...current.amenityIds, amenityId],
    }));
  };

  const handleChange = (field: keyof FormState, value: string | number[]) => {
    setForm((current) => ({ ...current, [field]: value }));
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setSubmitting(true);
    setError('');

    try {
      const payload: any = {
        tieuDe: form.title,
        moTa: form.description,
        gia: Number(form.price || 0),
        dienTich: Number(form.area || 0),
        soNguoiToiDa: Number(form.maxOccupants || 1),
        thanhPho: form.province,
        quan: form.district,
        phuong: form.ward,
        diaChi: form.address,
        tienIchIds: form.amenityIds,
        danhSachAnh: [],
      };

      if (isEdit && id) {
        await postService.updatePost(Number(id), payload);
        toast.success('Cập nhật tin đăng thành công');
      } else {
        await postService.createPost(payload);
        toast.success('Đăng tin mới thành công');
      }

      navigate(ROUTES.LANDLORD_POSTS);
    } catch (err) {
      console.error('Lưu tin đăng thất bại:', err);
      setError('Vui lòng kiểm tra lại thông tin và thử lại.');
    } finally {
      setSubmitting(false);
    }
  };

  if (loading) {
    return <div className="landlord-page"><div className="empty-state">Đang tải dữ liệu tin đăng...</div></div>;
  }

  return (
    <div className="landlord-page">
      <div className="landlord-shell">
        <div className="landlord-header">
          <div>
            <h1>{isEdit ? 'Chỉnh sửa tin đăng' : 'Đăng tin mới'}</h1>
            <p>{isEdit ? 'Cập nhật thông tin phòng trọ và nội dung quảng cáo.' : 'Thêm bài đăng mới để tiếp cận người thuê nhanh hơn.'}</p>
          </div>
          <div className="landlord-actions">
            <button className="landlord-btn-ghost" onClick={() => navigate(ROUTES.LANDLORD_POSTS)}>Quay lại</button>
          </div>
        </div>

        <form className="landlord-form" onSubmit={handleSubmit}>
          {error && <div className="admin-alert" style={{ marginBottom: 16 }}>{error}</div>}

          <div className="form-grid">
            <div className="form-group full">
              <label>Tiêu đề tin</label>
              <input required value={form.title} onChange={(e) => handleChange('title', e.target.value)} placeholder="Ví dụ: Phòng trọ gần trường ĐH X" />
            </div>

            <div className="form-group full">
              <label>Mô tả chi tiết</label>
              <textarea required value={form.description} onChange={(e) => handleChange('description', e.target.value)} placeholder="Mô tả phòng, vị trí, tiện nghi, pháp lý..." />
            </div>

            <div className="form-group">
              <label>Giá thuê (đồng/tháng)</label>
              <input required type="number" min="0" value={form.price} onChange={(e) => handleChange('price', e.target.value)} />
            </div>

            <div className="form-group">
              <label>Diện tích (m²)</label>
              <input required type="number" min="0" value={form.area} onChange={(e) => handleChange('area', e.target.value)} />
            </div>

            <div className="form-group">
              <label>Số người tối đa</label>
              <input required type="number" min="1" value={form.maxOccupants} onChange={(e) => handleChange('maxOccupants', e.target.value)} />
            </div>

            <div className="form-group">
              <label>Tỉnh/Thành phố</label>
              <input required value={form.province} onChange={(e) => handleChange('province', e.target.value)} />
            </div>

            <div className="form-group">
              <label>Quận/Huyện</label>
              <input required value={form.district} onChange={(e) => handleChange('district', e.target.value)} />
            </div>

            <div className="form-group">
              <label>Phường/Xã</label>
              <input required value={form.ward} onChange={(e) => handleChange('ward', e.target.value)} />
            </div>

            <div className="form-group full">
              <label>Địa chỉ chi tiết</label>
              <input required value={form.address} onChange={(e) => handleChange('address', e.target.value)} />
            </div>

            <div className="form-group full">
              <label>Tiện ích</label>
              <div className="amenity-list">
                {amenities.length ? amenities.map((item) => (
                  <button
                    type="button"
                    key={item.id}
                    className={`amenity-chip ${form.amenityIds.includes(item.id) ? 'active' : ''}`}
                    onClick={() => toggleAmenity(item.id)}
                  >
                    {item.name}
                  </button>
                )) : <span>Đang tải tiện ích...</span>}
              </div>
            </div>
          </div>

          <div className="landlord-actions" style={{ marginTop: 24 }}>
            <button type="submit" className="landlord-btn" disabled={submitting}>
              {submitting ? 'Đang lưu...' : (isEdit ? 'Lưu thay đổi' : 'Đăng tin')}
            </button>
            <button type="button" className="landlord-btn-ghost" onClick={() => navigate(ROUTES.LANDLORD_POSTS)}>Hủy</button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default LandlordPostFormPage;
