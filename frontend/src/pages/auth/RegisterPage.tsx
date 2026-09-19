import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  FiEye,
  FiEyeOff,
  FiUser,
  FiMail,
  FiPhone,
  FiLock,
  FiArrowLeft,
} from 'react-icons/fi';
import { toast } from 'react-toastify';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES, STORAGE_KEYS } from '../../utils/constants';
import './Auth.css';

const RegisterPage: React.FC = () => {
  const navigate = useNavigate();
  const { register } = useAuth();

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const [formData, setFormData] = useState({
    fullName: '',
    email: '',
    phone: '',
    password: '',
    confirmPassword: '',
    agree: false,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!formData.fullName.trim()) {
      toast.error('Vui lòng nhập họ và tên.');
      return;
    }

    if (!formData.email.trim()) {
      toast.error('Vui lòng nhập email.');
      return;
    }

    if (!/\S+@\S+\.\S+/.test(formData.email.trim())) {
      toast.error('Email không hợp lệ.');
      return;
    }

    if (!formData.phone.trim()) {
      toast.error('Vui lòng nhập số điện thoại.');
      return;
    }

    if (formData.password.length < 6) {
      toast.error('Mật khẩu phải có ít nhất 6 ký tự.');
      return;
    }

    if (formData.password !== formData.confirmPassword) {
      toast.error('Mật khẩu xác nhận không khớp.');
      return;
    }

    if (!formData.agree) {
      toast.error('Vui lòng đồng ý với điều khoản sử dụng.');
      return;
    }

    try {
      setLoading(true);

      // Đăng ký tài khoản mặc định là Tenant
      await register({
        fullName: formData.fullName.trim(),
        email: formData.email.trim(),
        phone: formData.phone.trim(),
        password: formData.password,
        confirmPassword: formData.confirmPassword,
        roleName: 'Tenant',
      });

      toast.success('Đăng ký tài khoản thành công!');

      setTimeout(() => {
        const storedUser = JSON.parse(localStorage.getItem(STORAGE_KEYS.USER) || 'null');
        navigate(storedUser?.role === 'Landlord' ? ROUTES.LANDLORD_DASHBOARD : ROUTES.HOME);
      }, 300);
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.message || 'Đăng ký thất bại. Vui lòng thử lại.';
      toast.error(errorMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="auth-page">
      <div className="auth-background">
        <div className="auth-decoration auth-decoration-1" />
        <div className="auth-decoration auth-decoration-2" />
      </div>

      <div className="auth-container">
        {/* Logo */}
        <Link to={ROUTES.HOME} className="auth-logo">
          Timnhatro<span>.vn</span>
        </Link>

        {/* Card */}
        <div className="auth-card auth-register-card">
          <div className="auth-header">
            <h1>Tạo tài khoản</h1>
            <p>
              Tham gia Timnhatro.vn để tìm kiếm
              <br />
              căn phòng phù hợp với bạn
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            {/* Full name */}
            <div className="auth-field">
              <label htmlFor="fullName">Họ và tên</label>
              <div className="auth-input-wrapper">
                <FiUser className="auth-input-icon" />
                <input
                  id="fullName"
                  name="fullName"
                  type="text"
                  value={formData.fullName}
                  onChange={handleChange}
                  placeholder="Nguyễn Văn A"
                  autoComplete="name"
                />
              </div>
            </div>

            {/* Email */}
            <div className="auth-field">
              <label htmlFor="email">Email</label>
              <div className="auth-input-wrapper">
                <FiMail className="auth-input-icon" />
                <input
                  id="email"
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="example@gmail.com"
                  autoComplete="email"
                />
              </div>
            </div>

            {/* Phone */}
            <div className="auth-field">
              <label htmlFor="phone">Số điện thoại</label>
              <div className="auth-input-wrapper">
                <FiPhone className="auth-input-icon" />
                <input
                  id="phone"
                  name="phone"
                  type="tel"
                  value={formData.phone}
                  onChange={handleChange}
                  placeholder="09xxxxxxxx"
                  autoComplete="tel"
                />
              </div>
            </div>

            {/* Password */}
            <div className="auth-field">
              <label htmlFor="password">Mật khẩu</label>
              <div className="auth-input-wrapper">
                <FiLock className="auth-input-icon" />
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={handleChange}
                  placeholder="Ít nhất 6 ký tự"
                  autoComplete="new-password"
                />

                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowPassword((prev) => !prev)}
                  aria-label={showPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                >
                  {showPassword ? <FiEyeOff /> : <FiEye />}
                </button>
              </div>
            </div>

            {/* Confirm password */}
            <div className="auth-field">
              <label htmlFor="confirmPassword">Xác nhận mật khẩu</label>
              <div className="auth-input-wrapper">
                <FiLock className="auth-input-icon" />
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  value={formData.confirmPassword}
                  onChange={handleChange}
                  placeholder="Nhập lại mật khẩu"
                  autoComplete="new-password"
                />

                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowConfirmPassword((prev) => !prev)}
                  aria-label={showConfirmPassword ? 'Ẩn mật khẩu' : 'Hiện mật khẩu'}
                >
                  {showConfirmPassword ? <FiEyeOff /> : <FiEye />}
                </button>
              </div>
            </div>

            {/* Terms */}
            <label className="auth-checkbox auth-terms">
              <input
                type="checkbox"
                name="agree"
                checked={formData.agree}
                onChange={handleChange}
              />
              <span>
                Tôi đồng ý với{' '}
                <Link to="/terms">Điều khoản sử dụng</Link> và{' '}
                <Link to="/privacy">Chính sách bảo mật</Link>
              </span>
            </label>

            {/* Submit */}
            <button
              type="submit"
              className="auth-submit"
              disabled={loading}
            >
              {loading ? (
                <span className="auth-loading">
                  <span />
                  Đang tạo tài khoản...
                </span>
              ) : (
                'Tạo tài khoản'
              )}
            </button>
          </form>

          <div className="auth-switch">
            <span>Đã có tài khoản?</span>
            <Link to={ROUTES.LOGIN}>Đăng nhập</Link>
          </div>
        </div>

        {/* Back */}
        <Link to={ROUTES.HOME} className="auth-back">
          <FiArrowLeft />
          Quay lại trang chủ
        </Link>
      </div>
    </div>
  );
};

export default RegisterPage;
