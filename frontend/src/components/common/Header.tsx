import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import Button from './Button';

const Header: React.FC = () => {
  const { user, isAuthenticated, isLandlord, isTenant, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const [isMenuOpen, setIsMenuOpen] = useState(false);

  const handleLogout = () => {
    logout();
    navigate(ROUTES.HOME);
  };

  return (
    <header className="bg-white shadow-md sticky top-0 z-40">
      <div className="container mx-auto px-4">
        <div className="flex items-center justify-between h-16">
          {/* Logo */}
          <Link to={ROUTES.HOME} className="flex items-center space-x-2">
            <div className="text-2xl">🏠</div>
            <span className="text-xl font-bold text-blue-600">PhòngTrọ247</span>
          </Link>

          {/* Desktop Navigation */}
          <nav className="hidden md:flex items-center space-x-6">
            <Link to={ROUTES.ROOM_LIST} className="text-gray-700 hover:text-blue-600 transition-colors">
              Tìm phòng
            </Link>

            {isAuthenticated ? (
              <>
                {isTenant && (
                  <>
                    <Link to={ROUTES.TENANT_FAVORITES} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Yêu thích
                    </Link>
                    <Link to={ROUTES.TENANT_APPOINTMENTS} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Lịch hẹn
                    </Link>
                  </>
                )}

                {isLandlord && (
                  <>
                    <Link to={ROUTES.LANDLORD_POSTS} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Tin đăng
                    </Link>
                    <Link to={ROUTES.LANDLORD_CREATE_POST} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Đăng tin
                    </Link>
                    <Link to={ROUTES.LANDLORD_APPOINTMENTS} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Lịch hẹn
                    </Link>
                  </>
                )}

                {isAdmin && (
                  <>
                    <Link to={ROUTES.ADMIN_DASHBOARD} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Dashboard
                    </Link>
                    <Link to={ROUTES.ADMIN_POST_APPROVAL} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Duyệt tin
                    </Link>
                    <Link to={ROUTES.ADMIN_USERS} className="text-gray-700 hover:text-blue-600 transition-colors">
                      Người dùng
                    </Link>
                  </>
                )}

                {/* User Menu */}
                <div className="relative group">
                  <button className="flex items-center space-x-2 text-gray-700 hover:text-blue-600 transition-colors">
                    <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center">
                      <span className="text-blue-600 font-semibold">
                        {user?.fullName?.charAt(0).toUpperCase()}
                      </span>
                    </div>
                    <span>{user?.fullName}</span>
                  </button>

                  {/* Dropdown */}
                  <div className="absolute right-0 mt-2 w-48 bg-white rounded-lg shadow-lg py-2 invisible group-hover:visible opacity-0 group-hover:opacity-100 transition-all">
                    <Link
                      to={isTenant ? ROUTES.TENANT_PROFILE : isLandlord ? ROUTES.LANDLORD_DASHBOARD : ROUTES.ADMIN_DASHBOARD}
                      className="block px-4 py-2 text-gray-700 hover:bg-blue-50 transition-colors"
                    >
                      Tài khoản
                    </Link>
                    <button
                      onClick={handleLogout}
                      className="block w-full text-left px-4 py-2 text-red-600 hover:bg-red-50 transition-colors"
                    >
                      Đăng xuất
                    </button>
                  </div>
                </div>
              </>
            ) : (
              <>
                <Link to={ROUTES.LOGIN}>
                  <Button variant="outline" size="sm">
                    Đăng nhập
                  </Button>
                </Link>
                <Link to={ROUTES.REGISTER}>
                  <Button size="sm">Đăng ký</Button>
                </Link>
              </>
            )}
          </nav>

          {/* Mobile Menu Button */}
          <button
            className="md:hidden text-gray-700"
            onClick={() => setIsMenuOpen(!isMenuOpen)}
          >
            <svg
              className="h-6 w-6"
              fill="none"
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeWidth="2"
              viewBox="0 0 24 24"
              stroke="currentColor"
            >
              {isMenuOpen ? (
                <path d="M6 18L18 6M6 6l12 12" />
              ) : (
                <path d="M4 6h16M4 12h16M4 18h16" />
              )}
            </svg>
          </button>
        </div>

        {/* Mobile Menu */}
        {isMenuOpen && (
          <nav className="md:hidden py-4 border-t">
            <Link
              to={ROUTES.ROOM_LIST}
              className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
              onClick={() => setIsMenuOpen(false)}
            >
              Tìm phòng
            </Link>

            {isAuthenticated ? (
              <>
                {isTenant && (
                  <>
                    <Link
                      to={ROUTES.TENANT_FAVORITES}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Yêu thích
                    </Link>
                    <Link
                      to={ROUTES.TENANT_APPOINTMENTS}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Lịch hẹn
                    </Link>
                    <Link
                      to={ROUTES.TENANT_PROFILE}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Tài khoản
                    </Link>
                  </>
                )}

                {isLandlord && (
                  <>
                    <Link
                      to={ROUTES.LANDLORD_POSTS}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Tin đăng
                    </Link>
                    <Link
                      to={ROUTES.LANDLORD_CREATE_POST}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Đăng tin mới
                    </Link>
                    <Link
                      to={ROUTES.LANDLORD_DASHBOARD}
                      className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                      onClick={() => setIsMenuOpen(false)}
                    >
                      Dashboard
                    </Link>
                  </>
                )}

                <button
                  onClick={handleLogout}
                  className="block w-full text-left py-2 text-red-600 hover:text-red-700 transition-colors"
                >
                  Đăng xuất
                </button>
              </>
            ) : (
              <>
                <Link
                  to={ROUTES.LOGIN}
                  className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                  onClick={() => setIsMenuOpen(false)}
                >
                  Đăng nhập
                </Link>
                <Link
                  to={ROUTES.REGISTER}
                  className="block py-2 text-gray-700 hover:text-blue-600 transition-colors"
                  onClick={() => setIsMenuOpen(false)}
                >
                  Đăng ký
                </Link>
              </>
            )}
          </nav>
        )}
      </div>
    </header>
  );
};

export default Header;
