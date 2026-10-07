import React, { useEffect, useState } from 'react';
import { FiUser, FiMail, FiPhone, FiLock, FiCalendar, FiShield, FiCheckCircle, FiSave } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { userService } from '../../services/userService';
import { UserProfile, UpdateProfileRequest, ChangePasswordRequest } from '../../types/user.types';
import { getApiErrorMessage } from '../../utils/apiError';
import PageState from '../../components/common/PageState';

const TenantProfilePage: React.FC = () => {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [activeTab, setActiveTab] = useState<'profile' | 'password'>('profile');

  const [fieldErrors, setFieldErrors] = useState<Partial<Record<'fullName' | 'phone' | 'currentPassword' | 'newPassword' | 'confirmPassword', string>>>({});
  const [submitError, setSubmitError] = useState('');

  // Form states
  const [fullName, setFullName] = useState('');
  const [phone, setPhone] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [updating, setUpdating] = useState(false);

  // Password states
  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [changingPassword, setChangingPassword] = useState(false);

  async function fetchProfile() {
    setLoading(true);
    setLoadError('');
    try {
      const response = await userService.getProfile();
      if (response.data) {
        setProfile(response.data);
        setFullName(response.data.fullName || '');
        setPhone(response.data.phone || '');
        setAvatarUrl(response.data.avatarUrl || '');
      }
    } catch (error) {
      setLoadError(getApiErrorMessage(error, 'Không thể tải thông tin cá nhân.'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void Promise.resolve().then(fetchProfile);
  }, []);

  const handleUpdateProfile = async (e: React.FormEvent) => {
    e.preventDefault();
    if (updating) return;
    const next: typeof fieldErrors = {};
    if (!fullName.trim()) next.fullName = 'Họ và tên không được để trống.';
    if (phone.trim() && !/^\\d{10,11}$/.test(phone.trim())) next.phone = 'Số điện thoại phải gồm 10 hoặc 11 chữ số.';
    setFieldErrors(next);
    setSubmitError('');
    if (Object.keys(next).length) return;

    setUpdating(true);
    try {
      const data: UpdateProfileRequest = {
        fullName: fullName.trim(),
        phone: phone.trim(),
        avatarUrl: avatarUrl.trim() || null,
      };
      const response = await userService.updateProfile(data);
      if (response.data) {
        setProfile(response.data);
        toast.success('Cập nhật thông tin thành công!');
      }
    } catch (error) {
      setSubmitError(getApiErrorMessage(error, 'Cập nhật thông tin thất bại.'));
    } finally {
      setUpdating(false);
    }
  };

  const handleChangePassword = async (e: React.FormEvent) => {
    e.preventDefault();
    if (changingPassword) return;
    const next: typeof fieldErrors = {};
    if (!currentPassword) next.currentPassword = 'Vui lòng nhập mật khẩu hiện tại.';
    if (newPassword.length < 6) next.newPassword = 'Mật khẩu mới phải có ít nhất 6 ký tự.';
    if (!confirmPassword || newPassword !== confirmPassword) next.confirmPassword = 'Xác nhận mật khẩu mới không khớp.';
    setFieldErrors(next);
    setSubmitError('');
    if (Object.keys(next).length) return;

    setChangingPassword(true);
    try {
      const data: ChangePasswordRequest = {
        currentPassword,
        newPassword,
      };
      await userService.changePassword(data);
      toast.success('Đổi mật khẩu thành công!');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (error) {
      setSubmitError(getApiErrorMessage(error, 'Đổi mật khẩu thất bại. Vui lòng kiểm tra lại mật khẩu cũ.'));
    } finally {
      setChangingPassword(false);
    }
  };

  if (loading) {
    return (
      <div className="max-w-5xl mx-auto px-4 py-12">
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm animate-pulse">
          Đang tải thông tin tài khoản...
        </div>
      </div>
    );
  }

  if (loadError || !profile) return <div className="tenant-page max-w-5xl mx-auto px-4 py-8"><PageState type="error" message={loadError || 'Không nhận được thông tin tài khoản.'} onRetry={fetchProfile} /></div>;

  return (
    <div className="tenant-page max-w-5xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-[#0084ff] font-bold mb-1">
          <FiUser /> Quản lý tài khoản
        </div>
        <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">Hồ sơ cá nhân</h1>
        <p className="text-sm text-slate-500 mt-1">
          Quản lý thông tin định danh, số điện thoại và bảo mật tài khoản người thuê phòng.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
        {/* Left Col: User Card */}
        <div className="lg:col-span-1 space-y-6">
          <div className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm text-center">
            <div className="relative inline-block mx-auto mb-4">
              <div className="w-24 h-24 rounded-full bg-gradient-to-tr from-[#0084ff] to-cyan-400 text-white flex items-center justify-center text-3xl font-black shadow-lg shadow-blue-500/20">
                {profile?.fullName?.charAt(0).toUpperCase() || 'U'}
              </div>
              <span className="absolute bottom-1 right-1 w-6 h-6 bg-emerald-500 border-2 border-white rounded-full flex items-center justify-center text-white text-xs">
                ✓
              </span>
            </div>

            <h2 className="text-xl font-bold text-slate-900">{profile?.fullName}</h2>
            <p className="text-sm text-slate-500 mt-0.5">{profile?.email}</p>

            <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-blue-50 text-[#0084ff] text-xs font-semibold rounded-full mt-3 border border-blue-100">
              <FiShield /> {profile?.role === 'Tenant' ? 'Người thuê phòng' : profile?.role}
            </div>

            <div className="border-t border-slate-100 mt-6 pt-5 text-left space-y-3 text-sm text-slate-600">
              <div className="flex items-center justify-between">
                <span className="text-slate-400 flex items-center gap-2">
                  <FiPhone size={15} /> Số điện thoại:
                </span>
                <span className="font-medium text-slate-800">{profile?.phone || 'Chưa cập nhật'}</span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400 flex items-center gap-2">
                  <FiCalendar size={15} /> Ngày tham gia:
                </span>
                <span className="font-medium text-slate-800">
                  {profile?.createdAt ? new Date(profile.createdAt).toLocaleDateString('vi-VN') : 'Mới tham gia'}
                </span>
              </div>
              <div className="flex items-center justify-between">
                <span className="text-slate-400 flex items-center gap-2">
                  <FiCheckCircle size={15} /> Trạng thái:
                </span>
                <span className="font-semibold text-emerald-600">Đang hoạt động</span>
              </div>
            </div>
          </div>

          {/* Navigation tabs */}
          <div className="bg-white rounded-2xl border border-slate-200/80 p-2 shadow-sm space-y-1">
            <button
              onClick={() => { setActiveTab('profile'); setSubmitError(''); setFieldErrors({}); }}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all ${
                activeTab === 'profile'
                  ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
                  : 'text-slate-600 hover:bg-slate-50'
              }`}
            >
              <FiUser size={18} /> Thông tin cá nhân
            </button>
            <button
              onClick={() => { setActiveTab('password'); setSubmitError(''); setFieldErrors({}); }}
              className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-semibold transition-all ${
                activeTab === 'password'
                  ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
                  : 'text-slate-600 hover:bg-slate-50'
              }`}
            >
              <FiLock size={18} /> Đổi mật khẩu
            </button>
          </div>
        </div>

        {/* Right Col: Tab Content */}
        <div className="lg:col-span-2">
          {activeTab === 'profile' ? (
            <div className="bg-white rounded-3xl border border-slate-200/80 p-8 shadow-sm">
              <h3 className="text-xl font-bold text-slate-900 mb-6 flex items-center gap-2">
                <FiUser className="text-[#0084ff]" /> Chỉnh sửa thông tin cá nhân
              </h3>

              <form onSubmit={handleUpdateProfile} noValidate className="space-y-5">
                <div>
                  <label htmlFor="profile-name" className="block text-sm font-semibold text-slate-700 mb-2">Họ và tên *</label>
                  <div className="relative">
                    <FiUser className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="text"
                      value={fullName}
                      id="profile-name"
                      autoComplete="name"
                      onChange={(e) => { setFullName(e.target.value); setFieldErrors(current => ({ ...current, fullName: undefined })); }}
                      placeholder="Nhập họ và tên..."
                      required
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10 text-sm"
                    />
                  </div>
                  {fieldErrors.fullName && <p role="alert" className="form-error mt-2">{fieldErrors.fullName}</p>}
                </div>

                <div>
                  <label htmlFor="profile-email" className="block text-sm font-semibold text-slate-700 mb-2">Địa chỉ Email</label>
                  <div className="relative">
                    <FiMail className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="email"
                      value={profile?.email || ''}
                      id="profile-email"
                      disabled
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 bg-slate-50 text-slate-500 text-sm cursor-not-allowed"
                    />
                  </div>
                  <span className="text-xs text-slate-400 mt-1 block">Email dùng để đăng nhập và không thể thay đổi.</span>
                </div>

                <div>
                  <label htmlFor="profile-phone" className="block text-sm font-semibold text-slate-700 mb-2">Số điện thoại liên hệ</label>
                  <div className="relative">
                    <FiPhone className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="tel"
                      value={phone}
                      id="profile-phone"
                      autoComplete="tel"
                      inputMode="tel"
                      onChange={(e) => { setPhone(e.target.value); setFieldErrors(current => ({ ...current, phone: undefined })); }}
                      placeholder="09xxxxxxxx..."
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10 text-sm"
                    />
                  </div>
                  {fieldErrors.phone && <p role="alert" className="form-error mt-2">{fieldErrors.phone}</p>}
                  <span className="text-xs text-slate-400 mt-1 block">Dùng để chủ trọ liên lạc xác nhận lịch xem phòng.</span>
                </div>

                {submitError && <p role="alert" className="form-error">{submitError}</p>}
                <div className="pt-4 border-t border-slate-100 flex justify-end">
                  <button
                    type="submit"
                    disabled={updating}
                    className="inline-flex items-center gap-2 px-6 py-3 bg-[#0084ff] hover:bg-[#0073e6] text-white font-semibold rounded-2xl shadow-md shadow-blue-500/20 disabled:opacity-60 transition-all cursor-pointer"
                  >
                    <FiSave /> {updating ? 'Đang lưu...' : 'Lưu thay đổi'}
                  </button>
                </div>
              </form>
            </div>
          ) : (
            <div className="bg-white rounded-3xl border border-slate-200/80 p-8 shadow-sm">
              <h3 className="text-xl font-bold text-slate-900 mb-6 flex items-center gap-2">
                <FiLock className="text-[#0084ff]" /> Thay đổi mật khẩu
              </h3>

              <form onSubmit={handleChangePassword} noValidate className="space-y-5">
                <div>
                  <label htmlFor="profile-current-password" className="block text-sm font-semibold text-slate-700 mb-2">Mật khẩu hiện tại *</label>
                  <div className="relative">
                    <FiLock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="password"
                      value={currentPassword}
                      id="profile-current-password"
                      autoComplete="current-password"
                      onChange={(e) => { setCurrentPassword(e.target.value); setFieldErrors(current => ({ ...current, currentPassword: undefined })); }}
                      placeholder="Nhập mật khẩu đang sử dụng..."
                      required
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10 text-sm"
                    />
                  </div>
                  {fieldErrors.currentPassword && <p role="alert" className="form-error mt-2">{fieldErrors.currentPassword}</p>}
                </div>

                <div>
                  <label htmlFor="profile-new-password" className="block text-sm font-semibold text-slate-700 mb-2">Mật khẩu mới *</label>
                  <div className="relative">
                    <FiLock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="password"
                      value={newPassword}
                      id="profile-new-password"
                      autoComplete="new-password"
                      minLength={6}
                      onChange={(e) => { setNewPassword(e.target.value); setFieldErrors(current => ({ ...current, newPassword: undefined })); }}
                      placeholder="Tối thiểu 6 ký tự..."
                      required
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10 text-sm"
                    />
                  </div>
                  {fieldErrors.newPassword && <p role="alert" className="form-error mt-2">{fieldErrors.newPassword}</p>}
                </div>

                <div>
                  <label htmlFor="profile-confirm-password" className="block text-sm font-semibold text-slate-700 mb-2">Xác nhận mật khẩu mới *</label>
                  <div className="relative">
                    <FiLock className="absolute left-4 top-1/2 -translate-y-1/2 text-slate-400" />
                    <input
                      type="password"
                      value={confirmPassword}
                      id="profile-confirm-password"
                      autoComplete="new-password"
                      onChange={(e) => { setConfirmPassword(e.target.value); setFieldErrors(current => ({ ...current, confirmPassword: undefined })); }}
                      placeholder="Nhập lại mật khẩu mới..."
                      required
                      className="w-full pl-11 pr-4 py-3 rounded-2xl border border-slate-200 focus:outline-none focus:border-[#0084ff] focus:ring-4 focus:ring-blue-500/10 text-sm"
                    />
                  </div>
                  {fieldErrors.confirmPassword && <p role="alert" className="form-error mt-2">{fieldErrors.confirmPassword}</p>}
                </div>

                {submitError && <p role="alert" className="form-error">{submitError}</p>}
                <div className="pt-4 border-t border-slate-100 flex justify-end">
                  <button
                    type="submit"
                    disabled={changingPassword}
                    className="inline-flex items-center gap-2 px-6 py-3 bg-[#0084ff] hover:bg-[#0073e6] text-white font-semibold rounded-2xl shadow-md shadow-blue-500/20 disabled:opacity-60 transition-all cursor-pointer"
                  >
                    <FiLock /> {changingPassword ? 'Đang cập nhật...' : 'Cập nhật mật khẩu'}
                  </button>
                </div>
              </form>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default TenantProfilePage;
