import React from 'react';
import AdminManagementContent from './AdminManagementContent';

type AdminModule = 'users' | 'posts' | 'approval' | 'rooms' | 'categories' | 'amenities' | 'reports';

/**
 * AdminManagementPage - Wrapper component
 * Chỉ render AdminManagementContent
 * Sidebar và topbar được handle bởi AdminDashboardLayout
 */
const AdminManagementPage: React.FC<{ module: AdminModule }> = ({ module }) => {
  return <AdminManagementContent module={module} />;
};

export default AdminManagementPage;
