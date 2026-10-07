import React, { useEffect, useRef, useState } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import {
  FiActivity,
  FiArrowLeft,
  FiCalendar,
  FiCheckCircle,
  FiChevronRight,
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
  { label: 'Yêu cầu thuê & Đặt cọc', icon: FiUsers, href: ROUTES.LANDLORD_RENTAL_REQUESTS },
  { label: 'Hợp đồng & Hóa đơn', icon: FiShield, href: ROUTES.LANDLORD_CONTRACTS },
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
  const [isDesktop, setIsDesktop] = useState(() => window.matchMedia('(min-width: 1025px)').matches);
  const sidebarRef = useRef<HTMLElement>(null);

  useEffect(() => {
    const media = window.matchMedia('(min-width: 1025px)');
    const update = () => setIsDesktop(media.matches);
    media.addEventListener('change', update);
    return () => media.removeEventListener('change', update);
  }, []);

  const menu = isAdmin ? adminMenu : landlordMenu;
  const isMenuActive = (href: string) => {
    const [pathname, query] = href.split('?');
    if (query) {
      const tab = new URLSearchParams(location.search).get('tab') || 'requests';
      return location.pathname === pathname && (query.includes('requests') ? tab === 'requests' : tab !== 'requests');
    }
    return location.pathname === pathname || (pathname.endsWith('/posts') && location.pathname.startsWith(`${pathname}/`) && !location.pathname.endsWith('/approval'));
  };
  const activeMenuItem = [...menu].reverse().find((item) => isMenuActive(item.href));

  useEffect(() => {
    if (!menuOpen) return;
    const previousOverflow = document.body.style.overflow;
    const previousFocus = document.activeElement as HTMLElement | null;
    document.body.style.overflow = 'hidden';
    sidebarRef.current?.querySelector<HTMLButtonElement>('button')?.focus();
    const closeOnEscape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') setMenuOpen(false);
      if (event.key === 'Tab' && sidebarRef.current) {
        const items = [...sidebarRef.current.querySelectorAll<HTMLElement>('button,a[href]')].filter(item => item.getClientRects().length);
        const first = items[0]; const last = items[items.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
      }
    };
    const desktop = window.matchMedia('(min-width: 1025px)');
    const closeOnDesktop = () => { if (desktop.matches) setMenuOpen(false); };
    window.addEventListener('keydown', closeOnEscape);
    desktop.addEventListener('change', closeOnDesktop);
    return () => { document.body.style.overflow = previousOverflow; previousFocus?.focus(); window.removeEventListener('keydown', closeOnEscape); desktop.removeEventListener('change', closeOnDesktop); };
  }, [menuOpen]);

  const handleLogout = () => {
    logout();
    navigate(ROUTES.LOGIN);
  };

  return (
    <div className="admin-dashboard-shell">
      {/* ===================================================
          SIDEBAR
      =================================================== */}
      {menuOpen && <button className="admin-dashboard-backdrop" aria-label="Đóng menu" onClick={() => setMenuOpen(false)} />}
      <aside ref={sidebarRef} id="dashboard-navigation" inert={!isDesktop && !menuOpen} aria-hidden={!isDesktop && !menuOpen} aria-label="Điều hướng quản lý" className={`admin-dashboard-sidebar ${menuOpen ? 'is-open' : ''}`}>
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
            const isActive = activeMenuItem === item;

            return (
              <Link
                key={`${item.href}-${item.label}`}
                to={item.href}
                className={isActive ? 'is-active' : ''}
                aria-current={isActive ? 'page' : undefined}
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
            aria-expanded={menuOpen}
            aria-controls="dashboard-navigation"
          >
            <FiMenu />
          </button>

          <div className="admin-dashboard-breadcrumb">
            <span>{isAdmin ? 'Quản trị hệ thống' : 'Chủ trọ'}</span>
              <FiChevronRight />
              <strong>
              {activeMenuItem?.label || 'Trang'}
            </strong>
          </div>

          <div className="admin-dashboard-account flex items-center gap-3">
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
