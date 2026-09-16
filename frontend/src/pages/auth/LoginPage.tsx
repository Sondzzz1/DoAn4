import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { FiEye, FiEyeOff, FiMail, FiLock, FiArrowLeft } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import './Auth.css';

const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login, isLandlord, isTenant, isAdmin } = useAuth();

  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);

  const [formData, setFormData] = useState({
    email: '',
    password: '',
    remember: false,
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

    if (!formData.email.trim()) {
      toast.error('Vui lòng nhập email hoặc số điện thoại.');
      return;
    }

    if (!formData.password) {
      toast.error('Vui lòng nhập mật khẩu.');
      return;
    }

    try {
      setLoading(true);

      await login({
        email: formData.email.trim(),
        password: formData.password,
      });

      toast.success('Đăng nhập thành công!');

      // Điều hướng dựa vào vai trò người dùng
      setTimeout(() => {
        if (isLandlord) {
          navigate(ROUTES.LANDLORD_DASHBOARD);
        } else if (isAdmin) {
          navigate(ROUTES.ADMIN_DASHBOARD);
        } else {
          navigate(ROUTES.HOME);
        }
      }, 300);
    } catch (error: any) {
      const errorMessage =
        error.response?.data?.message || 'Email hoặc mật khẩu không chính xác.';
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
        <div className="auth-card">
          <div className="auth-header">
            <h1>Chào mừng trở lại</h1>
            <p>
              Đăng nhập để tiếp tục tìm kiếm
              <br />
              phòng trọ phù hợp với bạn
            </p>
          </div>

          <form onSubmit={handleSubmit}>
            {/* Email */}
            <div className="auth-field">
              <label htmlFor="email">Email hoặc số điện thoại</label>
              <div className="auth-input-wrapper">
                <FiMail className="auth-input-icon" />
                <input
                  id="email"
                  name="email"
                  type="text"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="Nhập email hoặc số điện thoại"
                  autoComplete="username"
                />
              </div>
            </div>

            {/* Password */}
            <div className="auth-field">
              <div className="auth-label-row">
                <label htmlFor="password">Mật khẩu</label>
                <Link to="/forgot-password">Quên mật khẩu?</Link>
              </div>

              <div className="auth-input-wrapper">
                <FiLock className="auth-input-icon" />
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={handleChange}
                  placeholder="Nhập mật khẩu"
                  autoComplete="current-password"
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

            {/* Remember */}
            <label className="auth-checkbox">
              <input
                type="checkbox"
                name="remember"
                checked={formData.remember}
                onChange={handleChange}
              />
              <span>Ghi nhớ đăng nhập</span>
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
                  Đang đăng nhập...
                </span>
              ) : (
                'Đăng nhập'
              )}
            </button>
          </form>

          {/* Register */}
          <div className="auth-switch">
            <span>Chưa có tài khoản?</span>
            <Link to={ROUTES.REGISTER}>Đăng ký ngay</Link>
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

export default LoginPage;
