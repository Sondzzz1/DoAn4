import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import { FiHeart, FiChevronDown, FiLogOut, FiUser, FiCalendar, FiHome, FiPlusCircle, FiFileText } from 'react-icons/fi';
import NotificationBell from './NotificationBell';

const Header: React.FC = () => {
  const { user, isAuthenticated, isLandlord, isTenant, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate(ROUTES.HOME);
  };

  return (
    <header className="site-header">
      <div className="site-header-inner">
        {/* Logo */}
        <Link to={ROUTES.HOME} className="site-logo">
          Timnhatro<span className="site-logo-green">.vn</span>
        </Link>

        {/* Navigation */}
        <nav className="site-nav">
          <Link to="/rooms">Phòng trọ</Link>
          <Link to="/rooms?type=house">Nhà nguyên căn</Link>
          <Link to="/rooms?type=apartment">Căn hộ</Link>
          <Link to="/rooms?type=share">Ở ghép</Link>
          <Link to="/blog">Blog</Link>
        </nav>

        {/* Actions */}
        <div className="header-actions">
          {isAuthenticated ? (
            <>
              {isTenant && (
                <Link to={ROUTES.TENANT_FAVORITES} className="header-action">
                  <FiHeart size={18} />
                  Tin đã lưu
                </Link>
              )}

              <Link to={isLandlord ? ROUTES.LANDLORD_CREATE_POST : '#'} className="header-action">
                Đăng tin
              </Link>

              <NotificationBell />

              <div className="relative group">
                <button className="flex items-center gap-2 cursor-pointer bg-transparent border-none">
                  <div className="w-8 h-8 rounded-full bg-[#0084ff] text-white flex items-center justify-center font-bold text-sm">
                    {user?.fullName?.charAt(0).toUpperCase() || 'U'}
                  </div>
                  <span className="text-[14px] font-semibold text-gray-700">
                    {user?.fullName}
                  </span>
                  <FiChevronDown size={15} />
                </button>

                <div className="absolute right-0 top-full mt-2 w-56 bg-white border border-gray-100 rounded-xl shadow-xl py-2 invisible opacity-0 group-hover:visible group-hover:opacity-100 transition-all z-50">
                  {isTenant && (
                    <>
                      <Link to={ROUTES.TENANT_PROFILE} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiUser />
                        Thông tin cá nhân
                      </Link>
                      <Link to={ROUTES.TENANT_FAVORITES} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiHeart />
                        Phòng yêu thích
                      </Link>
                      <Link to={ROUTES.TENANT_APPOINTMENTS} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiCalendar />
                        Lịch hẹn của tôi
                      </Link>
                      <Link to={ROUTES.TENANT_RENTALS} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiFileText />
                        Thuê phòng & Hợp đồng
                      </Link>
                    </>
                  )}

                  {isLandlord && (
                    <>
                      <Link to={ROUTES.LANDLORD_DASHBOARD} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiHome />
                        Dashboard
                      </Link>
                      <Link to={ROUTES.LANDLORD_POSTS} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiUser />
                        Quản lý tin đăng
                      </Link>
                      <Link to={ROUTES.LANDLORD_CREATE_POST} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                        <FiPlusCircle />
                        Đăng tin mới
                      </Link>
                    </>
                  )}

                  {isAdmin && (
                    <Link to={ROUTES.ADMIN_DASHBOARD} className="flex items-center gap-3 px-4 py-2.5 text-sm text-gray-700 hover:bg-blue-50 hover:text-[#0084ff]">
                      <FiHome />
                      Dashboard Admin
                    </Link>
                  )}

                  <div className="border-t border-gray-100 my-1" />

                  <button onClick={handleLogout} className="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-red-500 hover:bg-red-50 border-none bg-transparent cursor-pointer">
                    <FiLogOut />
                    Đăng xuất
                  </button>
                </div>
              </div>
            </>
          ) : (
            <>
              <Link to="/tenant/favorites" className="header-action">
                <FiHeart size={18} />
                Tin đã lưu
              </Link>

              <Link to="/login" className="header-action">
                Đăng tin
              </Link>

              <Link to={ROUTES.REGISTER} className="header-action header-register">
                Đăng ký
              </Link>

              <Link to={ROUTES.LOGIN} className="header-action">
                Đăng nhập
              </Link>
            </>
          )}
        </div>
      </div>
    </header>
  );
};

export default Header;
