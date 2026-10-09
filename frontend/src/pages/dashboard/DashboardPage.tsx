import React, { useState } from 'react';
import DashboardContent from './DashboardContent';
import { useAuth } from '../../hooks/useAuth';
import LandlordModal from '../../components/landlord/LandlordModal';
import { PostEditor } from '../landlord/CreatePostPage';

const LandlordOverview: React.FC = () => {
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const [revision, setRevision] = useState(0);
  const close = () => { if (!busy) setOpen(false); };
  return <>
    <DashboardContent key={revision} onCreatePost={() => setOpen(true)} />
    <LandlordModal isOpen={open} onClose={close} title="Đăng tin cho thuê" size="xl">
      {open && <PostEditor onBusyChange={setBusy} onCancel={close} onSaved={() => { setBusy(false); setOpen(false); setRevision(value => value + 1); }} />}
    </LandlordModal>
  </>;
};

/**
 * DashboardPage - Trang tổng quan
 * Chỉ render DashboardContent
 * Sidebar và topbar được handle bởi AdminDashboardLayout
 */
const DashboardPage: React.FC = () => {
  const { isAdmin } = useAuth();
  return isAdmin ? <DashboardContent /> : <LandlordOverview />;
};

export default DashboardPage;
