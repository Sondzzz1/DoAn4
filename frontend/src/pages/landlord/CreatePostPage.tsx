import React, { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { toast } from 'react-toastify';
import { 
  FiImage, 
  FiMapPin, 
  FiPlus, 
  FiX, 
  FiFileText, 
  FiDollarSign, 
  FiHome,
  FiUsers,
  FiCheckCircle,
  FiAlertCircle
} from 'react-icons/fi';
import { postService } from '../../services/postService';
import { adminService } from '../../services/adminService';
import { CreatePostRequest } from '../../types/post.types';

interface Amenity {
  id: number;
  name: string;
  icon?: string;
}

const CreatePostPage: React.FC = () => {
  const navigate = useNavigate();
  const { id } = useParams<{ id: string }>();
  const isEditMode = !!id;
  
  const [loading, setLoading] = useState(false);
  const [loadingData, setLoadingData] = useState(isEditMode);
  const [amenities, setAmenities] = useState<Amenity[]>([]);
  const [imageInput, setImageInput] = useState('');

  const [formData, setFormData] = useState<CreatePostRequest>({
    title: '',
    description: '',
    price: 0,
    area: 0,
    maxOccupants: 1,
    province: '',
    district: '',
    ward: '',
    address: '',
    amenityIds: [],
    imageUrls: [],
  });

  // Load danh sách tiện ích
  useEffect(() => {
    const loadAmenities = async () => {
      try {
        const response = await adminService.getAmenities();
        setAmenities(response.data || []);
      } catch (error: any) {
        console.error('Không thể tải danh sách tiện ích:', error);
      }
    };
    void loadAmenities();
  }, []);

  // Load dữ liệu tin đăng nếu đang ở chế độ chỉnh sửa
  useEffect(() => {
    if (isEditMode && id) {
      const loadPost = async () => {
        setLoadingData(true);
        try {
          const response = await postService.getPostById(Number(id));
          const post = response.data;
          
          setFormData({
            title: post.title,
            description: post.description,
            price: post.price,
            area: post.area,
            maxOccupants: post.maxOccupants,
            province: post.province,
            district: post.district,
            ward: post.ward,
            address: post.address,
            amenityIds: post.amenities.map((a) => a.id),
            imageUrls: post.imageUrls || [],
          });
        } catch (error: any) {
          toast.error('Không thể tải thông tin tin đăng');
          navigate('/landlord/posts');
        } finally {
          setLoadingData(false);
        }
      };
      void loadPost();
    }
  }, [id, isEditMode, navigate]);

  const handleInputChange = (field: keyof CreatePostRequest, value: any) => {
    setFormData((prev) => ({ ...prev, [field]: value }));
  };

  const handleAmenityToggle = (amenityId: number) => {
    setFormData((prev) => {
      const currentIds = prev.amenityIds;
      const newIds = currentIds.includes(amenityId)
        ? currentIds.filter((id) => id !== amenityId)
        : [...currentIds, amenityId];
      return { ...prev, amenityIds: newIds };
    });
  };

  const handleAddImage = () => {
    if (!imageInput.trim()) {
      toast.warning('Vui lòng nhập URL hình ảnh');
      return;
    }
    setFormData((prev) => ({
      ...prev,
      imageUrls: [...prev.imageUrls, imageInput.trim()],
    }));
    setImageInput('');
  };

  const handleRemoveImage = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      imageUrls: prev.imageUrls.filter((_, i) => i !== index),
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Validation
    if (!formData.title.trim()) {
      toast.error('Vui lòng nhập tiêu đề tin đăng');
      return;
    }
    if (!formData.description.trim()) {
      toast.error('Vui lòng nhập mô tả chi tiết');
      return;
    }
    if (formData.price <= 0) {
      toast.error('Vui lòng nhập giá thuê hợp lệ');
      return;
    }
    if (formData.area <= 0) {
      toast.error('Vui lòng nhập diện tích hợp lệ');
      return;
    }
    if (!formData.address.trim() || !formData.province.trim()) {
      toast.error('Vui lòng nhập đầy đủ địa chỉ');
      return;
    }

    setLoading(true);
    try {
      if (isEditMode && id) {
        await postService.updatePost(Number(id), formData);
        toast.success('Cập nhật tin đăng thành công! Tin của bạn sẽ được Admin duyệt lại.');
      } else {
        await postService.createPost(formData);
        toast.success('Đăng tin thành công! Tin của bạn đang chờ Admin duyệt.');
      }
      navigate('/landlord/posts');
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Có lỗi xảy ra khi lưu tin đăng');
    } finally {
      setLoading(false);
    }
  };

  if (loadingData) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 p-6">
        <div className="max-w-5xl mx-auto">
          <div className="text-center py-20">
            <div className="inline-block animate-spin rounded-full h-12 w-12 border-4 border-blue-500 border-t-transparent"></div>
            <p className="text-slate-600 mt-4">Đang tải thông tin tin đăng...</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 via-white to-purple-50 p-6">
      <div className="max-w-5xl mx-auto">
        {/* Header với gradient và icon */}
        <div className="mb-8 text-center">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-gradient-to-br from-blue-500 to-purple-600 rounded-2xl shadow-lg mb-4">
            <FiFileText className="text-white text-2xl" />
          </div>
          <p className="text-xs uppercase tracking-[0.3em] text-blue-600 font-semibold mb-2">
            {isEditMode ? 'CHỈNH SỬA TIN ĐĂNG' : 'ĐĂNG TIN MỚI'}
          </p>
          <h1 className="text-4xl font-bold bg-gradient-to-r from-blue-600 to-purple-600 bg-clip-text text-transparent mb-3">
            {isEditMode ? 'Cập nhật tin đăng' : 'Tạo tin đăng phòng trọ'}
          </h1>
          <p className="text-slate-600 max-w-2xl mx-auto">
            {isEditMode
              ? 'Cập nhật thông tin tin đăng. Tin sẽ được Admin duyệt lại sau khi chỉnh sửa.'
              : 'Điền đầy đủ thông tin để tạo tin đăng. Tin của bạn sẽ được Admin duyệt trước khi hiển thị công khai.'}
          </p>
        </div>

        {/* Progress Steps */}
        <div className="mb-8">
          <div className="flex items-center justify-center space-x-4">
            <div className="flex items-center">
              <div className="flex items-center justify-center w-10 h-10 rounded-full bg-blue-500 text-white font-semibold shadow-md">
                1
              </div>
              <span className="ml-2 text-sm font-medium text-slate-700">Thông tin</span>
            </div>
            <div className="w-16 h-1 bg-gradient-to-r from-blue-500 to-purple-500 rounded"></div>
            <div className="flex items-center">
              <div className="flex items-center justify-center w-10 h-10 rounded-full bg-purple-500 text-white font-semibold shadow-md">
                2
              </div>
              <span className="ml-2 text-sm font-medium text-slate-700">Địa chỉ</span>
            </div>
            <div className="w-16 h-1 bg-gradient-to-r from-purple-500 to-pink-500 rounded"></div>
            <div className="flex items-center">
              <div className="flex items-center justify-center w-10 h-10 rounded-full bg-pink-500 text-white font-semibold shadow-md">
                3
              </div>
              <span className="ml-2 text-sm font-medium text-slate-700">Hoàn tất</span>
            </div>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Thông tin cơ bản */}
          <div className="rounded-3xl border-2 border-blue-100 bg-white shadow-xl p-8 hover:shadow-2xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6">
              <div className="p-3 bg-gradient-to-br from-blue-500 to-blue-600 rounded-xl shadow-md">
                <FiFileText className="text-white text-xl" />
              </div>
              <h2 className="text-2xl font-bold text-slate-800">Thông tin cơ bản</h2>
            </div>

            <div className="space-y-5">
              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                  <FiFileText className="text-blue-500" />
                  Tiêu đề tin đăng <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formData.title}
                  onChange={(e) => handleInputChange('title', e.target.value)}
                  className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100 transition-all text-slate-800 placeholder:text-slate-400"
                  placeholder="VD: Phòng trọ cao cấp gần ĐH Bách Khoa, đầy đủ nội thất"
                />
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                  <FiFileText className="text-blue-500" />
                  Mô tả chi tiết <span className="text-red-500">*</span>
                </label>
                <textarea
                  value={formData.description}
                  onChange={(e) => handleInputChange('description', e.target.value)}
                  className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-blue-500 focus:ring-4 focus:ring-blue-100 transition-all text-slate-800 placeholder:text-slate-400 resize-none"
                  placeholder="Mô tả chi tiết về phòng trọ: vị trí, tiện ích, nội thất, môi trường xung quanh..."
                  rows={6}
                />
                <p className="text-xs text-slate-500 mt-2">Mô tả càng chi tiết sẽ càng thu hút người thuê</p>
              </div>

              <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                    <FiDollarSign className="text-green-500" />
                    Giá thuê (VNĐ/tháng) <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="number"
                      value={formData.price}
                      onChange={(e) => handleInputChange('price', Number(e.target.value))}
                      className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-green-500 focus:ring-4 focus:ring-green-100 transition-all text-slate-800"
                      placeholder="3000000"
                      min="0"
                    />
                    <span className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 text-sm">đ</span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                    <FiHome className="text-purple-500" />
                    Diện tích (m²) <span className="text-red-500">*</span>
                  </label>
                  <div className="relative">
                    <input
                      type="number"
                      value={formData.area}
                      onChange={(e) => handleInputChange('area', Number(e.target.value))}
                      className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-100 transition-all text-slate-800"
                      placeholder="25"
                      min="0"
                      step="0.1"
                    />
                    <span className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400 text-sm">m²</span>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2 flex items-center gap-2">
                    <FiUsers className="text-orange-500" />
                    Số người tối đa <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="number"
                    value={formData.maxOccupants}
                    onChange={(e) => handleInputChange('maxOccupants', Number(e.target.value))}
                    className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-orange-500 focus:ring-4 focus:ring-orange-100 transition-all text-slate-800"
                    placeholder="2"
                    min="1"
                  />
                </div>
              </div>
            </div>
          </div>

          {/* Địa chỉ */}
          <div className="rounded-3xl border-2 border-purple-100 bg-white shadow-xl p-8 hover:shadow-2xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6">
              <div className="p-3 bg-gradient-to-br from-purple-500 to-purple-600 rounded-xl shadow-md">
                <FiMapPin className="text-white text-xl" />
              </div>
              <h2 className="text-2xl font-bold text-slate-800">Địa chỉ</h2>
            </div>

            <div className="space-y-5">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2">
                    Tỉnh/Thành phố <span className="text-red-500">*</span>
                  </label>
                  <input
                    type="text"
                    value={formData.province}
                    onChange={(e) => handleInputChange('province', e.target.value)}
                    className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-100 transition-all text-slate-800"
                    placeholder="Hồ Chí Minh"
                  />
                </div>

                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2">
                    Quận/Huyện
                  </label>
                  <input
                    type="text"
                    value={formData.district}
                    onChange={(e) => handleInputChange('district', e.target.value)}
                    className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-100 transition-all text-slate-800"
                    placeholder="Quận 1"
                  />
                </div>

                <div>
                  <label className="block text-sm font-semibold text-slate-700 mb-2">
                    Phường/Xã
                  </label>
                  <input
                    type="text"
                    value={formData.ward}
                    onChange={(e) => handleInputChange('ward', e.target.value)}
                    className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-100 transition-all text-slate-800"
                    placeholder="Phường Bến Nghé"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-semibold text-slate-700 mb-2">
                  Địa chỉ cụ thể <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={formData.address}
                  onChange={(e) => handleInputChange('address', e.target.value)}
                  className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 outline-none focus:border-purple-500 focus:ring-4 focus:ring-purple-100 transition-all text-slate-800"
                  placeholder="Số nhà, tên đường..."
                />
              </div>
            </div>
          </div>

          {/* Tiện ích */}
          <div className="rounded-3xl border-2 border-green-100 bg-white shadow-xl p-8 hover:shadow-2xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6">
              <div className="p-3 bg-gradient-to-br from-green-500 to-green-600 rounded-xl shadow-md">
                <FiCheckCircle className="text-white text-xl" />
              </div>
              <div>
                <h2 className="text-2xl font-bold text-slate-800">Tiện ích</h2>
                <p className="text-sm text-slate-600">Chọn các tiện ích mà phòng của bạn có</p>
              </div>
            </div>

            {amenities.length > 0 ? (
              <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-3">
                {amenities.map((amenity) => (
                  <label
                    key={amenity.id}
                    className={`
                      group relative flex items-center gap-3 p-4 border-2 rounded-xl cursor-pointer transition-all duration-200
                      ${formData.amenityIds.includes(amenity.id)
                        ? 'bg-gradient-to-br from-green-50 to-emerald-50 border-green-500 shadow-md scale-105'
                        : 'bg-white border-slate-200 hover:border-green-300 hover:shadow-sm'
                      }
                    `}
                  >
                    <input
                      type="checkbox"
                      checked={formData.amenityIds.includes(amenity.id)}
                      onChange={() => handleAmenityToggle(amenity.id)}
                      className="w-5 h-5 rounded border-2 border-slate-300 text-green-500 focus:ring-2 focus:ring-green-200"
                    />
                    <span className={`text-sm font-medium ${formData.amenityIds.includes(amenity.id) ? 'text-green-700' : 'text-slate-700'}`}>
                      {amenity.name}
                    </span>
                    {formData.amenityIds.includes(amenity.id) && (
                      <FiCheckCircle className="absolute top-2 right-2 text-green-500 text-sm" />
                    )}
                  </label>
                ))}
              </div>
            ) : (
              <div className="text-center py-8">
                <div className="inline-block animate-spin rounded-full h-8 w-8 border-4 border-green-500 border-t-transparent"></div>
                <p className="text-slate-500 text-sm mt-3">Đang tải danh sách tiện ích...</p>
              </div>
            )}
          </div>

          {/* Hình ảnh */}
          <div className="rounded-3xl border-2 border-pink-100 bg-white shadow-xl p-8 hover:shadow-2xl transition-all duration-300">
            <div className="flex items-center gap-3 mb-6">
              <div className="p-3 bg-gradient-to-br from-pink-500 to-pink-600 rounded-xl shadow-md">
                <FiImage className="text-white text-xl" />
              </div>
              <div>
                <h2 className="text-2xl font-bold text-slate-800">Hình ảnh</h2>
                <p className="text-sm text-slate-600">Thêm hình ảnh để phòng của bạn nổi bật hơn</p>
              </div>
            </div>

            <div className="space-y-5">
              <div className="flex gap-3">
                <div className="flex-1 relative">
                  <input
                    type="url"
                    value={imageInput}
                    onChange={(e) => setImageInput(e.target.value)}
                    className="w-full border-2 border-slate-200 rounded-xl px-4 py-3.5 pr-12 outline-none focus:border-pink-500 focus:ring-4 focus:ring-pink-100 transition-all text-slate-800"
                    placeholder="Dán URL hình ảnh hoặc link từ Google Drive, Imgur..."
                    onKeyDown={(e) => {
                      if (e.key === 'Enter') {
                        e.preventDefault();
                        handleAddImage();
                      }
                    }}
                  />
                  <FiImage className="absolute right-4 top-1/2 -translate-y-1/2 text-slate-400" />
                </div>
                <button
                  type="button"
                  onClick={handleAddImage}
                  className="px-6 py-3.5 bg-gradient-to-r from-pink-500 to-pink-600 text-white rounded-xl hover:from-pink-600 hover:to-pink-700 flex items-center gap-2 font-medium shadow-md hover:shadow-lg transition-all"
                >
                  <FiPlus /> Thêm
                </button>
              </div>

              {formData.imageUrls.length > 0 ? (
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <p className="text-sm font-medium text-slate-700">
                      <FiCheckCircle className="inline text-green-500 mr-1" />
                      {formData.imageUrls.length} ảnh đã thêm
                    </p>
                  </div>
                  <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                    {formData.imageUrls.map((url, index) => (
                      <div key={index} className="relative group">
                        <div className="relative overflow-hidden rounded-xl border-2 border-slate-200 shadow-md hover:shadow-xl transition-all aspect-video">
                          <img
                            src={url}
                            alt={`Ảnh ${index + 1}`}
                            className="w-full h-full object-cover"
                          />
                          <div className="absolute inset-0 bg-gradient-to-t from-black/50 to-transparent opacity-0 group-hover:opacity-100 transition-opacity">
                            <div className="absolute bottom-2 left-2 text-white text-xs font-medium">
                              Ảnh {index + 1}
                            </div>
                          </div>
                          {index === 0 && (
                            <div className="absolute top-2 left-2 bg-blue-500 text-white text-xs px-2 py-1 rounded-full font-medium shadow">
                              Ảnh đại diện
                            </div>
                          )}
                        </div>
                        <button
                          type="button"
                          onClick={() => handleRemoveImage(index)}
                          className="absolute -top-2 -right-2 p-2 bg-red-500 text-white rounded-full opacity-0 group-hover:opacity-100 transition-all shadow-lg hover:bg-red-600 hover:scale-110"
                        >
                          <FiX size={16} />
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              ) : (
                <div className="border-2 border-dashed border-slate-300 rounded-xl p-12 text-center bg-slate-50">
                  <FiImage className="mx-auto text-5xl text-slate-400 mb-3" />
                  <p className="text-slate-600 font-medium mb-1">Chưa có hình ảnh nào</p>
                  <p className="text-slate-500 text-sm">
                    Hãy thêm ít nhất 1 ảnh để thu hút người thuê
                  </p>
                </div>
              )}
            </div>
          </div>

          {/* Submit buttons */}
          <div className="flex flex-col sm:flex-row gap-4 pt-4">
            <button
              type="submit"
              disabled={loading}
              className="flex-1 bg-gradient-to-r from-blue-600 to-purple-600 text-white px-8 py-4 rounded-xl hover:from-blue-700 hover:to-purple-700 disabled:opacity-50 disabled:cursor-not-allowed font-semibold text-lg shadow-lg hover:shadow-xl transition-all flex items-center justify-center gap-3 group"
            >
              {loading ? (
                <>
                  <div className="animate-spin rounded-full h-5 w-5 border-2 border-white border-t-transparent"></div>
                  <span>{isEditMode ? 'Đang cập nhật...' : 'Đang đăng tin...'}</span>
                </>
              ) : (
                <>
                  <FiCheckCircle className="group-hover:scale-110 transition-transform" />
                  <span>{isEditMode ? 'Cập nhật tin đăng' : 'Đăng tin ngay'}</span>
                </>
              )}
            </button>
            <button
              type="button"
              onClick={() => navigate('/landlord/posts')}
              className="px-8 py-4 border-2 border-slate-300 rounded-xl hover:bg-slate-50 hover:border-slate-400 font-semibold text-slate-700 transition-all flex items-center justify-center gap-2"
            >
              <FiX />
              <span>Hủy</span>
            </button>
          </div>

          {/* Helper Alert */}
          <div className="mt-6 p-4 bg-blue-50 border-l-4 border-blue-500 rounded-lg flex items-start gap-3">
            <FiAlertCircle className="text-blue-500 mt-0.5 flex-shrink-0" />
            <div className="text-sm text-blue-800">
              <p className="font-semibold mb-1">Lưu ý quan trọng:</p>
              <ul className="list-disc list-inside space-y-1 text-blue-700">
                <li>Tin đăng sẽ được Admin duyệt trước khi hiển thị công khai</li>
                <li>Vui lòng điền đầy đủ và chính xác thông tin để tăng tỷ lệ duyệt</li>
                <li>Ảnh đầu tiên sẽ là ảnh đại diện của tin đăng</li>
              </ul>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreatePostPage;
