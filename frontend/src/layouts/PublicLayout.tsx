import React from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { FiCalendar, FiFileText, FiHeart, FiUser } from 'react-icons/fi';
import Header from '../components/common/Header';
import Footer from '../components/common/Footer';
import ChatDrawer from '../components/chat/ChatDrawer';

const PublicLayout: React.FC = () => {
  const location = useLocation();
  return (
    <div className="public-shell min-h-screen flex flex-col">
      <Header />
      <main className="public-main flex-1 min-w-0">
        {location.pathname.startsWith('/tenant/') && <nav className="tenant-navigation" aria-label="Khu vực người thuê"><div>
          <NavLink to="/tenant/profile"><FiUser />Tài khoản</NavLink>
          <NavLink to="/tenant/favorites"><FiHeart />Tin đã lưu</NavLink>
          <NavLink to="/tenant/appointments"><FiCalendar />Lịch xem phòng</NavLink>
          <NavLink to="/tenant/rentals"><FiFileText />Thuê phòng</NavLink>
        </div></nav>}
        <Outlet />
      </main>
      <Footer />
      <ChatDrawer />
    </div>
  );
};

export default PublicLayout;
