import React from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

// Layouts
import PublicLayout from '../layouts/PublicLayout';

// Public Pages
import HomePage from '../pages/public/HomePage';
import RoomListPage from '../pages/public/RoomListPage';
import RoomDetailPage from '../pages/public/RoomDetailPage';
import LoginPage from '../pages/auth/LoginPage';
import RegisterPage from '../pages/auth/RegisterPage';

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
      {/* Public Routes */}
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/rooms" element={<RoomListPage />} />
        <Route path="/rooms/:id" element={<RoomDetailPage />} />
        
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

        {/* Landlord Routes */}
        <Route
          path="/landlord/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <PlaceholderPage title="Dashboard Chủ nhà" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <PlaceholderPage title="Quản lý tin đăng" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/create"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <PlaceholderPage title="Đăng tin mới" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/:id/edit"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <PlaceholderPage title="Chỉnh sửa tin đăng" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/appointments"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <PlaceholderPage title="Quản lý lịch hẹn" />
            </ProtectedRoute>
          }
        />

        {/* Admin Routes */}
        <Route
          path="/admin/dashboard"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <PlaceholderPage title="Dashboard Admin" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/users"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <PlaceholderPage title="Quản lý người dùng" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/posts"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <PlaceholderPage title="Quản lý tin đăng" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/posts/approval"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <PlaceholderPage title="Duyệt tin đăng" />
            </ProtectedRoute>
          }
        />
        <Route
          path="/admin/amenities"
          element={
            <ProtectedRoute allowedRoles={['Admin']}>
              <PlaceholderPage title="Quản lý tiện ích" />
            </ProtectedRoute>
          }
        />

        {/* 404 */}
        <Route path="*" element={<PlaceholderPage title="404 - Không tìm thấy trang" />} />
      </Route>
    </Routes>
  );
};

export default AppRoutes;
