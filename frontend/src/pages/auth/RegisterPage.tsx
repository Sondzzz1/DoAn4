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
import { getApiErrorMessage } from '../../utils/apiError';
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

  const [errors, setErrors] = useState<Partial<Record<keyof typeof formData, string>>>({});
  const [submitError, setSubmitError] = useState('');

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type, checked } = e.target;
    setErrors(current => ({ ...current, [name]: undefined }));
    setFormData((prev) => ({
      ...prev,
      [name]: type === 'checkbox' ? checked : value,
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (loading) return;
    setSubmitError('');
    const next: typeof errors = {};
    if (!formData.fullName.trim()) next.fullName = 'Vui lòng nhập họ và tên.';
    if (!/\\S+@\\S+\\.\\S+/.test(formData.email.trim())) next.email = 'Vui lòng nhập email hợp lệ.';
    if (!/^[0-9]{10,11}$/.test(formData.phone.trim())) next.phone = 'Số điện thoại phải gồm 10 hoặc 11 chữ số.';
    if (formData.password.length < 6) next.password = 'Mật khẩu phải có ít nhất 6 ký tự.';
    if (!formData.confirmPassword || formData.password !== formData.confirmPassword) next.confirmPassword = 'Mật khẩu xác nhận không khớp.';
    if (!formData.agree) next.agree = 'Vui lòng đồng ý với điều khoản sử dụng.';
    setErrors(next);
    if (Object.keys(next).length) return;

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
    } catch (error) {
      setSubmitError(getApiErrorMessage(error, 'Đăng ký thất bại. Vui lòng thử lại.'));
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

          <form onSubmit={handleSubmit} noValidate>
            {/* Full name */}
            <div className="auth-field">
              <label htmlFor="fullName">Họ và tên</label>
              <div className="auth-input-wrapper">
                <FiUser className="auth-input-icon" />
                <input
                  id="fullName"
                  required
                  aria-invalid={!!errors.fullName}
                  aria-describedby={errors.fullName ? 'auth-fullName-error' : undefined}
                  name="fullName"
                  type="text"
                  value={formData.fullName}
                  onChange={handleChange}
                  placeholder="Nguyễn Văn A"
                  autoComplete="name"
                />
              </div>
            {errors.fullName && <p id="auth-fullName-error" role="alert" className="form-error mt-2">{errors.fullName}</p>}
            </div>

            {/* Email */}
            <div className="auth-field">
              <label htmlFor="email">Email</label>
              <div className="auth-input-wrapper">
                <FiMail className="auth-input-icon" />
                <input
                  id="email"
                  required
                  aria-invalid={!!errors.email}
                  aria-describedby={errors.email ? 'auth-email-error' : undefined}
                  name="email"
                  type="email"
                  value={formData.email}
                  onChange={handleChange}
                  placeholder="example@gmail.com"
                  autoComplete="email"
                />
              </div>
            {errors.email && <p id="auth-email-error" role="alert" className="form-error mt-2">{errors.email}</p>}
            </div>

            {/* Phone */}
            <div className="auth-field">
              <label htmlFor="phone">Số điện thoại</label>
              <div className="auth-input-wrapper">
                <FiPhone className="auth-input-icon" />
                <input
                  id="phone"
                  required
                  aria-invalid={!!errors.phone}
                  aria-describedby={errors.phone ? 'auth-phone-error' : undefined}
                  name="phone"
                  type="tel"
                  value={formData.phone}
                  onChange={handleChange}
                  placeholder="09xxxxxxxx"
                  autoComplete="tel"
                />
              </div>
            {errors.phone && <p id="auth-phone-error" role="alert" className="form-error mt-2">{errors.phone}</p>}
            </div>

            {/* Password */}
            <div className="auth-field">
              <label htmlFor="password">Mật khẩu</label>
              <div className="auth-input-wrapper">
                <FiLock className="auth-input-icon" />
                <input
                  id="password"
                  required
                  aria-invalid={!!errors.password}
                  aria-describedby={errors.password ? 'auth-password-error' : undefined}
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
            {errors.password && <p id="auth-password-error" role="alert" className="form-error mt-2">{errors.password}</p>}
            </div>

            {/* Confirm password */}
            <div className="auth-field">
              <label htmlFor="confirmPassword">Xác nhận mật khẩu</label>
              <div className="auth-input-wrapper">
                <FiLock className="auth-input-icon" />
                <input
                  id="confirmPassword"
                  required
                  aria-invalid={!!errors.confirmPassword}
                  aria-describedby={errors.confirmPassword ? 'auth-confirmPassword-error' : undefined}
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
            {errors.confirmPassword && <p id="auth-confirmPassword-error" role="alert" className="form-error mt-2">{errors.confirmPassword}</p>}
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

            {errors.agree && <p role="alert" className="form-error mb-3">{errors.agree}</p> }
            {submitError && <p role="alert" className="form-error mb-3">{submitError}</p>}
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
