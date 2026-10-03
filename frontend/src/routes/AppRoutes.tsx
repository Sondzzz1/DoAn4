import React, { Suspense, lazy } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

// Layouts
import PublicLayout from '../layouts/PublicLayout';
import AdminDashboardLayout from '../layouts/AdminDashboardLayout';

import PageState from '../components/common/PageState';

const HomePage = lazy(() => import('../pages/public/HomePage'));
const RoomListPage = lazy(() => import('../pages/public/RoomListPage'));
const RoomDetailPage = lazy(() => import('../pages/public/RoomDetailPage'));
const BlogPage = lazy(() => import('../pages/public/BlogPage'));
const BlogDetailPage = lazy(() => import('../pages/public/BlogDetailPage'));
const LoginPage = lazy(() => import('../pages/auth/LoginPage'));
const RegisterPage = lazy(() => import('../pages/auth/RegisterPage'));
const DashboardPage = lazy(() => import('../pages/dashboard/DashboardPage'));
const AdminManagementPage = lazy(() => import('../pages/admin/AdminManagementPage'));
const LandlordRoomManagementPage = lazy(() => import('../pages/landlord/LandlordRoomManagementPage'));
const TenantAppointmentsPage = lazy(() => import('../pages/tenant/TenantAppointmentsPage'));
const TenantProfilePage = lazy(() => import('../pages/tenant/TenantProfilePage'));
const TenantFavoritesPage = lazy(() => import('../pages/tenant/TenantFavoritesPage'));
const TenantRentalsPage = lazy(() => import('../pages/tenant/TenantRentalsPage'));
const LandlordAppointmentsPage = lazy(() => import('../pages/landlord/LandlordAppointmentsPage'));
const LandlordContractsPage = lazy(() => import('../pages/landlord/LandlordContractsPage'));
const LandlordPostsPage = lazy(() => import('../pages/landlord/LandlordPostsPage'));
const CreatePostPage = lazy(() => import('../pages/landlord/CreatePostPage'));
const PaymentResultPage = lazy(() => import('../pages/payment/PaymentResultPage'));

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
  const { isAuthenticated } = useAuth();

  return (
    <Suspense fallback={<PageState type="loading" message="Đang tải trang..." />}>
    <Routes>
      {/* Public Routes with Header + Footer */}
      <Route element={<PublicLayout />}>
        <Route path="/" element={<HomePage />} />
        <Route path="/rooms" element={<RoomListPage />} />
        <Route path="/rooms/:id" element={<RoomDetailPage />} />
        <Route path="/blog" element={<BlogPage />} />
        <Route path="/blog/:id" element={<BlogDetailPage />} />
        <Route path="/payment/result" element={<PaymentResultPage />} />
        
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
              <TenantProfilePage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tenant/favorites"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <TenantFavoritesPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tenant/appointments"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <TenantAppointmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/tenant/rentals"
          element={
            <ProtectedRoute allowedRoles={['Tenant']}>
              <TenantRentalsPage />
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
              <LandlordPostsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/create"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <CreatePostPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/posts/:id/edit"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <CreatePostPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/appointments"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <LandlordAppointmentsPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/rooms"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <LandlordRoomManagementPage />
            </ProtectedRoute>
          }
        />
        <Route
          path="/landlord/contracts"
          element={
            <ProtectedRoute allowedRoles={['Landlord']}>
              <LandlordContractsPage />
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
    </Suspense>
  );
};

export default AppRoutes;
