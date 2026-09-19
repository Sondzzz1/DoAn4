import React from 'react';
import DashboardContent from './DashboardContent';

/**
 * DashboardPage - Trang tổng quan
 * Chỉ render DashboardContent
 * Sidebar và topbar được handle bởi AdminDashboardLayout
 */
const DashboardPage: React.FC = () => {
  return <DashboardContent />;
};

export default DashboardPage;