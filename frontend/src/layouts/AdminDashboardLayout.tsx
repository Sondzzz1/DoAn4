import React, { useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  FiActivity,
  FiArrowLeft,
  FiCalendar,
  FiCheckCircle,
  FiChevronRight,
  FiClipboard,
  FiFileText,
  FiGrid,
  FiHome,
  FiLogOut,
  FiMenu,
  FiSettings,
  FiShield,
  FiTag,
  FiUsers,
  FiX,
} from 'react-icons/fi';
import { useAuth } from '../hooks/useAuth';
import { ROUTES } from '../utils/constants';
import NotificationBell from '../components/common/NotificationBell';
import ChatDrawer from '../components/chat/ChatDrawer';
import './AdminDashboardLayout.css';

/* =========================================================
   MENU ITEMS
========================================================= */

const landlordMenu = [
  { label: 'Tổng quan', icon: FiGrid, href: ROUTES.LANDLORD_DASHBOARD },
  { label: 'Tin đăng', icon: FiFileText, href: ROUTES.LANDLORD_POSTS },
  { label: 'Phòng trọ', icon: FiHome, href: '/landlord/rooms' },
  { label: 'Lịch hẹn xem phòng', icon: FiCalendar, href: ROUTES.LANDLORD_APPOINTMENTS },
  { label: 'Yêu cầu thuê', icon: FiClipboard, href: '/landlord/rental-requests' },
  { label: 'Hợp đồng & đặt cọc', icon: FiShield, href: '/landlord/contracts' },
];

const adminMenu = [
  { label: 'Tổng quan', icon: FiGrid, href: ROUTES.ADMIN_DASHBOARD },
  { label: 'Người dùng', icon: FiUsers, href: ROUTES.ADMIN_USERS },
  { label: 'Phòng trọ', icon: FiHome, href: '/admin/rooms' },
  { label: 'Tin đăng', icon: FiFileText, href: ROUTES.ADMIN_POSTS },
  { label: 'Duyệt tin', icon: FiCheckCircle, href: ROUTES.ADMIN_POST_APPROVAL },
  { label: 'Danh mục', icon: FiTag, href: '/admin/categories' },
  { label: 'Tiện ích', icon: FiSettings, href: ROUTES.ADMIN_AMENITIES },
  { label: 'Báo cáo vi phạm', icon: FiActivity, href: '/admin/reports' },
];

/* =========================================================
   COMPONENT
========================================================= */

const AdminDashboardLayout: React.FC = () => {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [menuOpen, setMenuOpen] = useState(false);

  const menu = isAdmin ? adminMenu : landlordMenu;

  const handleLogout = () => {
    logout();
    navigate(ROUTES.LOGIN);
  };

  return (
    <div className="admin-dashboard-shell">
      {/* ===================================================
          SIDEBAR
      =================================================== */}
      <aside className={`admin-dashboard-sidebar ${menuOpen ? 'is-open' : ''}`}>
        <div className="admin-dashboard-brand">
          <span className="admin-dashboard-brand-mark">T</span>
          <span>
            Timnhatro<span>.vn</span>
          </span>
          <button
            className="admin-dashboard-close"
            onClick={() => setMenuOpen(false)}
            aria-label="Đóng menu"
          >
            <FiX />
          </button>
        </div>

        <div className="admin-dashboard-role">
          <span className="admin-dashboard-role-dot" />
          {isAdmin ? 'Khu vực quản trị viên' : 'Khu vực chủ trọ'}
        </div>

        <nav className="admin-dashboard-nav">
          <p className="admin-dashboard-nav-label">
            {isAdmin ? 'Quản trị hệ thống' : 'Quản lý'}
          </p>

          {menu.map((item) => {
            const Icon = item.icon;
            const isActive = location.pathname === item.href;

            return (
              <Link
                key={item.href}
                to={item.href}
                className={isActive ? 'is-active' : ''}
                onClick={() => setMenuOpen(false)}
              >
                <Icon />
                <span>{item.label}</span>
                {isActive && <FiChevronRight className="admin-dashboard-nav-arrow" />}
              </Link>
            );
          })}
        </nav>

        <div className="admin-dashboard-sidebar-bottom">
          <Link to="/">
            <FiArrowLeft />
            Xem trang chính
          </Link>

          <button onClick={handleLogout}>
            <FiLogOut />
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* ===================================================
          MAIN CONTENT AREA
      =================================================== */}
      <main className="admin-dashboard-main">
        {/* TOPBAR */}
        <header className="admin-dashboard-topbar">
          <button
            className="admin-dashboard-menu-button"
            onClick={() => setMenuOpen(true)}
            aria-label="Mở menu"
          >
            <FiMenu />
          </button>

          <div className="admin-dashboard-breadcrumb">
            <span>{isAdmin ? 'Quản trị hệ thống' : 'Chủ trọ'}</span>
            <FiChevronRight />
            <strong>
              {menu.find((m) => m.href === location.pathname)?.label || 'Trang'}
            </strong>
          </div>

          <div className="flex items-center gap-3">
            <NotificationBell />
            <div className="admin-dashboard-user">
              <div className="admin-dashboard-avatar">
                {user?.fullName?.charAt(0) || 'U'}
              </div>
              <div>
                <strong>{user?.fullName || 'Người dùng'}</strong>
                <span>{isAdmin ? 'Quản trị viên' : 'Chủ trọ'}</span>
              </div>
            </div>
          </div>
        </header>

        {/* PAGE CONTENT */}
        <div className="admin-dashboard-content">
          <Outlet />
        </div>

        {/* Global Realtime Chat */}
        <ChatDrawer />
      </main>
    </div>
  );
};

export default AdminDashboardLayout;
