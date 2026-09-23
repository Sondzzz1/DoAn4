import React, { useEffect, useState, useRef } from 'react';
import { useNavigate } from 'react-router-dom';
import { FiBell, FiCheck, FiCheckCircle, FiInfo, FiCalendar, FiDollarSign, FiFileText, FiAlertCircle } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { notificationService, NotificationItem } from '../../services/notificationService';
import { useAuth } from '../../hooks/useAuth';

const NotificationBell: React.FC = () => {
  const { isAuthenticated } = useAuth();
  const navigate = useNavigate();
  const [isOpen, setIsOpen] = useState(false);
  const [notifications, setNotifications] = useState<NotificationItem[]>([]);
  const [unreadCount, setUnreadCount] = useState<number>(0);
  const [loading, setLoading] = useState<boolean>(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isAuthenticated) return;

    // Start SignalR connection
    void notificationService.startConnection();

    // Listen to real-time notifications
    const unsubscribe = notificationService.onReceiveNotification((noti) => {
      setNotifications((prev) => [noti, ...prev]);
      setUnreadCount((prev) => prev + 1);
      toast.info(`🔔 ${noti.title}: ${noti.content.slice(0, 45)}...`, {
        position: 'top-right',
        autoClose: 5000,
      });
    });

    void fetchNotifications();
    void fetchUnreadCount();

    // Handle outside click to close dropdown
    const handleClickOutside = (e: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(e.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);

    return () => {
      unsubscribe();
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, [isAuthenticated]);

  const fetchNotifications = async () => {
    try {
      setLoading(true);
      const res = await notificationService.getMyNotifications();
      if (res.success && res.data) {
        setNotifications(res.data);
      }
    } catch {
      // Ignored
    } finally {
      setLoading(false);
    }
  };

  const fetchUnreadCount = async () => {
    try {
      const res = await notificationService.getUnreadCount();
      if (res.success && typeof res.data === 'number') {
        setUnreadCount(res.data);
      }
    } catch {
      // Ignored
    }
  };

  const handleToggle = () => {
    if (!isOpen) {
      void fetchNotifications();
      void fetchUnreadCount();
    }
    setIsOpen(!isOpen);
  };

  const handleNotificationClick = async (noti: NotificationItem) => {
    if (!noti.isRead) {
      try {
        await notificationService.markAsRead(noti.id);
        setNotifications((prev) =>
          prev.map((item) => (item.id === noti.id ? { ...item, isRead: true } : item))
        );
        setUnreadCount((prev) => Math.max(0, prev - 1));
      } catch {
        // Ignored
      }
    }

    setIsOpen(false);
    if (noti.link) {
      navigate(noti.link);
    }
  };

  const handleMarkAllAsRead = async () => {
    try {
      await notificationService.markAllAsRead();
      setNotifications((prev) => prev.map((item) => ({ ...item, isRead: true })));
      setUnreadCount(0);
      toast.success('Đã đánh dấu tất cả là đã đọc.');
    } catch {
      toast.error('Không thể cập nhật thông báo.');
    }
  };

  const getNotificationIcon = (type?: number, title?: string) => {
    const text = (title || '').toLowerCase();
    if (text.includes('lịch hẹn') || text.includes('hẹn xem')) {
      return <FiCalendar className="text-amber-500" size={18} />;
    }
    if (text.includes('cọc') || text.includes('thanh toán') || text.includes('tiền')) {
      return <FiDollarSign className="text-emerald-500" size={18} />;
    }
    if (text.includes('hợp đồng') || text.includes('thuê')) {
      return <FiFileText className="text-blue-500" size={18} />;
    }
    if (text.includes('sự cố') || text.includes('hủy')) {
      return <FiAlertCircle className="text-rose-500" size={18} />;
    }
    return <FiInfo className="text-indigo-500" size={18} />;
  };

  if (!isAuthenticated) return null;

  return (
    <div className="relative" ref={dropdownRef}>
      {/* Bell Button */}
      <button
        type="button"
        onClick={handleToggle}
        className="relative p-2 rounded-full text-gray-600 hover:text-[#0084ff] hover:bg-gray-100 transition-colors cursor-pointer border-none bg-transparent"
        title="Thông báo"
      >
        <FiBell size={20} />
        {unreadCount > 0 && (
          <span className="absolute top-0 right-0 w-5 h-5 rounded-full bg-rose-500 text-white text-[11px] font-bold flex items-center justify-center border-2 border-white animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* Notification Dropdown */}
      {isOpen && (
        <div className="absolute right-0 top-full mt-2 w-80 sm:w-96 bg-white border border-gray-100 rounded-2xl shadow-2xl overflow-hidden z-50 animate-in fade-in slide-in-from-top-2">
          {/* Header */}
          <div className="p-4 bg-slate-50 border-b border-gray-100 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <h3 className="text-sm font-bold text-gray-900">Thông báo</h3>
              {unreadCount > 0 && (
                <span className="bg-blue-100 text-[#0084ff] text-[11px] font-bold px-2 py-0.5 rounded-full">
                  {unreadCount} mới
                </span>
              )}
            </div>
            {unreadCount > 0 && (
              <button
                type="button"
                onClick={handleMarkAllAsRead}
                className="text-xs font-semibold text-[#0084ff] hover:underline flex items-center gap-1 bg-transparent border-none cursor-pointer"
              >
                <FiCheck size={13} />
                Đọc tất cả
              </button>
            )}
          </div>

          {/* List */}
          <div className="max-h-96 overflow-y-auto divide-y divide-gray-50">
            {loading && notifications.length === 0 ? (
              <div className="p-8 text-center text-xs text-gray-400">Đang tải thông báo...</div>
            ) : notifications.length === 0 ? (
              <div className="p-8 text-center text-gray-400 text-xs">
                <FiCheckCircle className="mx-auto text-2xl text-gray-300 mb-1" />
                Bạn không có thông báo nào
              </div>
            ) : (
              notifications.map((noti) => (
                <div
                  key={noti.id}
                  onClick={() => handleNotificationClick(noti)}
                  className={`p-3.5 px-4 flex items-start gap-3 cursor-pointer transition-colors ${
                    noti.isRead ? 'bg-white hover:bg-gray-50/80 opacity-80' : 'bg-blue-50/40 hover:bg-blue-50/70 font-medium'
                  }`}
                >
                  <div className="w-9 h-9 rounded-xl bg-gray-100 flex items-center justify-center shrink-0 mt-0.5">
                    {getNotificationIcon(noti.type, noti.title)}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <p className={`text-xs text-gray-900 line-clamp-1 ${!noti.isRead ? 'font-bold' : 'font-semibold'}`}>
                        {noti.title}
                      </p>
                      {!noti.isRead && <span className="w-2 h-2 rounded-full bg-[#0084ff] shrink-0" />}
                    </div>
                    <p className="text-[12px] text-gray-600 line-clamp-2 mt-0.5">{noti.content}</p>
                    <span className="text-[10px] text-gray-400 mt-1 block">
                      {new Date(noti.createdAt).toLocaleDateString('vi-VN', {
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric',
                        hour: '2-digit',
                        minute: '2-digit',
                      })}
                    </span>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      )}
    </div>
  );
};

export default NotificationBell;
