import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

// Layouts
import PublicLayout from '../layouts/PublicLayout';
import AdminDashboardLayout from '../layouts/AdminDashboardLayout';

// Public Pages
import HomePage from '../pages/public/HomePage';
import RoomListPage from '../pages/public/RoomListPage';
import RoomDetailPage from '../pages/public/RoomDetailPage';
import BlogPage from '../pages/public/BlogPage';
import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';
import DashboardPage from '../pages/dashboard/DashboardPage';
import AdminManagementPage from '../pages/admin/AdminManagementPage';

// Protected Route
import ProtectedRoute from './ProtectedRoute';

// Placeholder components for routes not yet implemented
const PlaceholderPage: React.FC<{ title: string }> = ({ title }) => (
  <div className="container mx-auto px-4 py-8">
    <div className="text-center py-16">
      <h1 className="text-3xl font-bold text-gray-900 mb-4">{title}</h1>
      <p className="text-gray-600">Trang này đang được phát triển...</p>
    </div>
  </div>
);

const AppRoutes: React.FC = () => {
  const { isAuthenticated, user } = useAuth();

  return (
    <Routes>
      {/* Public Routes with Header + Footer */}
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/rooms" element={<RoomListPage />} />
        <Route path="/rooms/:id" element={<RoomDetailPage />} />
        <Route path="/blog" element={<BlogPage />} />
        
        {/* Auth Routes - redirect if already logged in */}
        <Route 
          path="/login" 
          element={isAuthenticated ? <Navigate to="/" replace /> : <LoginPage />} 
        />
        <Route 
          path="/register" 
          element={isAuthenticated ? <Navigate to="/" replace /> : <RegisterPage />} 
        />

        {/* Tenant Routes */}
        <Route
          path="/tenant/profile"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <PlaceholderPage title="Tài khoản của tôi" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tenant/favorites"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <PlaceholderPage title="Phòng yêu thích" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tenant/appointments"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <PlaceholderPage title="Lịch hẹn của tôi" />
            </ProtectedRoute>
          }
        />

        {/* 404 */}
        <Route path="*" element={<PlaceholderPage title="404 - Không tìm thấy trang" />} />
      </Route>

      {/* Dashboard Routes - Shared Sidebar Layout */}
      <Route element={<AdminDashboardLayout />}>
        {/* Landlord Routes */}
        <Route
          path="/landlord/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <AdminManagementPage module="posts" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/create"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <AdminManagementPage module="posts" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/:id/edit"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <AdminManagementPage module="posts" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/appointments"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <AdminManagementPage module="posts" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/rooms"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <AdminManagementPage module="rooms" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/rental-requests"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/contracts"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        {/* Admin Routes */}
        <Route
          path="/admin/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <DashboardPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="users" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/posts"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="posts" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/posts/approval"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="approval" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/amenities"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="amenities" />
            </ProtectedRoute>
          }
        />
        <Route 
          path="/admin/rooms" 
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="rooms" />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/admin/categories" 
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="categories" />
            </ProtectedRoute>
          } 
        />
        <Route 
          path="/admin/reports" 
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <AdminManagementPage module="reports" />
            </ProtectedRoute>
          } 
        />
      </Route>
    </Routes>
  );
};

export default AppRoutes;
