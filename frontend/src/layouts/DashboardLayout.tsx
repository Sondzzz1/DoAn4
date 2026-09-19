import React from 'react';
import { Outlet } from 'react-router-dom';
import './DashboardLayout.css';

/**
 * Layout cho trang Admin và Landlord
 * KHÔNG có Header/Footer
 * Các trang dashboard tự quản lý sidebar và layout
 */
const DashboardLayout: React.FC = () => {
  return (
    <div className="dashboard-layout">
      <Outlet />
    </div>
  );
};

export default DashboardLayout;
