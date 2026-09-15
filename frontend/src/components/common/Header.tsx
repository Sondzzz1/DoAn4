import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import { FiHeart, FiChevronDown, FiLogOut, FiUser, FiCalendar, FiHome, FiPlusCircle } from 'react-icons/fi';

const Header: React.FC = () => {
  const { user, isAuthenticated, isLandlord, isTenant, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => { logout(); navigate(ROUTES.HOME); };

  return (
    <header className="bg-white border-b border-gray-200 sticky top-0 z-50 shadow-sm min-w-[1200px]">
      <div className="max-w-[1200px] mx-auto px-4">
        <div className="flex items-center h-20 gap-10">

          {/* Logo */}
          <Link to={ROUTES.HOME} className="flex items-center gap-2 flex-shrink-0">
            <svg className="w-8 h-8 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9.75L12 3l9 6.75V21a.75.75 0 01-.75.75H3.75A.75.75 0 013 21V9.75z"/>
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 21V12h6v9"/>
            </svg>
            <span className="text-2xl font-extrabold text-blue-600 tracking-tight">Timnhatro<span className="text-blue-400">.vn</span></span>
          </Link>

          {/* Desktop Nav */}
          <nav className="flex items-center gap-8 flex-1">
            <Link to="/rooms" className="text-gray-800 hover:text-blue-600 transition-colors text-[15px] font-semibold">Phòng trọ</Link>
            <Link to="/rooms?type=house" className="text-gray-800 hover:text-blue-600 transition-colors text-[15px] font-semibold">Nhà nguyên căn</Link>
            <Link to="/rooms?type=apartment" className="text-gray-800 hover:text-blue-600 transition-colors text-[15px] font-semibold">Căn hộ</Link>
            <Link to="/rooms?type=share" className="text-gray-800 hover:text-blue-600 transition-colors text-[15px] font-semibold">Ở ghép</Link>
            <Link to="/blog" className="text-gray-800 hover:text-blue-600 transition-colors text-[15px] font-semibold">Blog</Link>
          </nav>

          {/* Right Actions */}
          <div className="flex items-center gap-5 ml-auto">
            {isAuthenticated ? (
              <>
                {isTenant && (
                  <Link to={ROUTES.TENANT_FAVORITES} className="flex items-center gap-1.5 text-gray-700 hover:text-blue-600 text-[15px] font-medium transition-colors">
                    <FiHeart className="w-5 h-5"/>Tin đã lưu
                  </Link>
                )}
                <Link to={isLandlord ? ROUTES.LANDLORD_CREATE_POST : '#'} className="text-gray-700 hover:text-blue-600 text-[15px] font-medium transition-colors">Đăng tin</Link>
                <div className="relative group">
                  <button className="flex items-center gap-2 text-gray-800 hover:text-blue-600 transition-colors">
                    <div className="w-9 h-9 bg-blue-600 rounded-full flex items-center justify-center">
                      <span className="text-white font-bold text-sm">{user?.fullName?.charAt(0).toUpperCase()}</span>
                    </div>
                    <span className="text-[15px] font-semibold">{user?.fullName}</span>
                    <FiChevronDown className="w-4 h-4"/>
                  </button>
                  <div className="absolute right-0 top-full mt-2 w-56 bg-white rounded-xl shadow-xl border border-gray-100 py-2 invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all duration-150 z-50">
                    {isTenant && (<>
                      <Link to={ROUTES.TENANT_PROFILE} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiUser className="w-4 h-4"/>Thông tin cá nhân</Link>
                      <Link to={ROUTES.TENANT_APPOINTMENTS} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiCalendar className="w-4 h-4"/>Lịch hẹn của tôi</Link>
                    </>)}
                    {isLandlord && (<>
                      <Link to={ROUTES.LANDLORD_DASHBOARD} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiHome className="w-4 h-4"/>Dashboard</Link>
                      <Link to={ROUTES.LANDLORD_POSTS} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiUser className="w-4 h-4"/>Quản lý tin đăng</Link>
                      <Link to={ROUTES.LANDLORD_CREATE_POST} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiPlusCircle className="w-4 h-4"/>Đăng tin mới</Link>
                    </>)}
                    {isAdmin && (<>
                      <Link to={ROUTES.ADMIN_DASHBOARD} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiHome className="w-4 h-4"/>Dashboard Admin</Link>
                      <Link to={ROUTES.ADMIN_POST_APPROVAL} className="flex items-center gap-2.5 px-4 py-2.5 text-[15px] text-gray-700 hover:bg-blue-50 hover:text-blue-600"><FiUser className="w-4 h-4"/>Duyệt tin đăng</Link>
                    </>)}
                    <hr className="my-1 border-gray-100"/>
                    <button onClick={handleLogout} className="flex items-center gap-2 w-full px-4 py-2.5 text-[15px] text-red-500 hover:bg-red-50">
                      <FiLogOut className="w-4 h-4"/>Đăng xuất
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <>
                <Link to="/tenant/favorites" className="flex items-center gap-1.5 text-gray-700 hover:text-blue-600 text-[15px] font-medium transition-colors">
                  <FiHeart className="w-5 h-5"/>Tin đã lưu
                </Link>
                <Link to="/login" className="text-gray-700 hover:text-blue-600 text-[15px] font-medium transition-colors ml-2">Đăng tin</Link>
                <Link to={ROUTES.REGISTER} className="border-2 border-blue-600 text-blue-600 px-5 py-2 rounded-xl text-[15px] font-bold hover:bg-blue-600 hover:text-white transition-all ml-2">Đăng ký</Link>
                <Link to={ROUTES.LOGIN} className="text-gray-800 hover:text-blue-600 text-[15px] font-bold transition-colors ml-2">Đăng nhập</Link>
              </>
            )}
          </div>
        </div>
      </div>
    </header>
  );
};
export default Header;