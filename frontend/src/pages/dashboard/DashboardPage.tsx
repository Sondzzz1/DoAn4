import React, { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';

import {
  FiActivity,
  FiArrowUpRight,
  FiBarChart2,
  FiBell,
  FiCalendar,
  FiCheckCircle,
  FiChevronRight,
  FiClipboard,
  FiFileText,
  FiGrid,
  FiHome,
  FiLogOut,
  FiMenu,
  FiPieChart,
  FiPlus,
  FiSearch,
  FiSettings,
  FiShield,
  FiUsers,
  FiX,
  FiTag,
} from 'react-icons/fi';

import { useAuth } from '../../hooks/useAuth';
import { ROUTES } from '../../utils/constants';
import {
  dashboardService,
  AdminDashboard,
  LandlordDashboard,
} from '../../services/dashboardService';

import './DashboardPage.css';

type Metric = {
  label: string;
  value: number;
  hint: string;
  icon: React.ReactNode;
  tone: string;
};

/* =========================================================
   MENU CHỦ TRỌ
========================================================= */

const landlordMenu = [
  {
    label: 'Tổng quan',
    icon: FiGrid,
    href: ROUTES.LANDLORD_DASHBOARD,
  },
  {
    label: 'Tin đăng',
    icon: FiFileText,
    href: ROUTES.LANDLORD_POSTS,
  },
  {
    label: 'Phòng trọ',
    icon: FiHome,
    href: '/landlord/rooms',
  },
  {
    label: 'Lịch hẹn xem phòng',
    icon: FiCalendar,
    href: ROUTES.LANDLORD_APPOINTMENTS,
  },
  {
    label: 'Yêu cầu thuê',
    icon: FiClipboard,
    href: '/landlord/rental-requests',
  },
  {
    label: 'Hợp đồng & đặt cọc',
    icon: FiShield,
    href: '/landlord/contracts',
  },
];

/* =========================================================
   MENU ADMIN
========================================================= */

const adminMenu = [
  {
    label: 'Tổng quan',
    icon: FiGrid,
    href: ROUTES.ADMIN_DASHBOARD,
  },
  {
    label: 'Người dùng',
    icon: FiUsers,
    href: ROUTES.ADMIN_USERS,
  },
  {
    label: 'Phòng trọ',
    icon: FiHome,
    href: '/admin/rooms',
  },
  {
    label: 'Tin đăng',
    icon: FiFileText,
    href: ROUTES.ADMIN_POSTS,
  },
  {
    label: 'Duyệt tin',
    icon: FiCheckCircle,
    href: ROUTES.ADMIN_POST_APPROVAL,
  },
  {
    label: 'Danh mục',
    icon: FiTag,
    href: '/admin/categories',
  },
  {
    label: 'Tiện ích',
    icon: FiSettings,
    href: ROUTES.ADMIN_AMENITIES,
  },
  {
    label: 'Báo cáo vi phạm',
    icon: FiActivity,
    href: '/admin/reports',
  },
];

/* =========================================================
   FORMAT NUMBER
========================================================= */

const formatNumber = (value: number) =>
  new Intl.NumberFormat('vi-VN').format(value || 0);

/* =========================================================
   COMPONENT
========================================================= */

const DashboardPage: React.FC = () => {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();

  const [landlord, setLandlord] =
    useState<LandlordDashboard | null>(null);

  const [admin, setAdmin] =
    useState<AdminDashboard | null>(null);

  const [loading, setLoading] = useState(true);
  const [menuOpen, setMenuOpen] = useState(false);

  const menu = isAdmin ? adminMenu : landlordMenu;

  /* =======================================================
     LOAD DASHBOARD
  ======================================================= */

  useEffect(() => {
    const loadDashboard = async () => {
      try {
        if (isAdmin) {
          const response = await dashboardService.getAdmin();
          setAdmin(response.data);
        } else {
          const response = await dashboardService.getLandlord();
          setLandlord(response.data);
        }
      } catch (error) {
        console.error('Không thể tải dữ liệu dashboard:', error);
      } finally {
        setLoading(false);
      }
    };

    loadDashboard();
  }, [isAdmin]);

  /* =======================================================
     LOGOUT
  ======================================================= */

  const handleLogout = () => {
    logout();
    navigate(ROUTES.LOGIN);
  };

  /* =======================================================
     METRICS
  ======================================================= */

  const metrics: Metric[] = isAdmin
    ? [
        {
          label: 'Tổng người dùng',
          value: admin?.totalUsers || 0,
          hint: `${admin?.totalTenants || 0} người thuê`,
          icon: <FiUsers />,
          tone: 'blue',
        },
        {
          label: 'Tổng phòng trọ',
          value: admin?.totalRooms || 0,
          hint: `${admin?.totalLandlords || 0} chủ trọ`,
          icon: <FiHome />,
          tone: 'green',
        },
        {
          label: 'Tin chờ duyệt',
          value: admin?.pendingPosts || 0,
          hint: 'Cần kiểm tra',
          icon: <FiCheckCircle />,
          tone: 'amber',
        },
        {
          label: 'Báo cáo cần xử lý',
          value: admin?.pendingReports || 0,
          hint: `${admin?.totalReports || 0} báo cáo tổng`,
          icon: <FiActivity />,
          tone: 'rose',
        },
      ]
    : [
        {
          label: 'Tổng phòng trọ',
          value: landlord?.totalRooms || 0,
          hint: `${landlord?.availableRooms || 0} phòng còn trống`,
          icon: <FiHome />,
          tone: 'blue',
        },
        {
          label: 'Tin đăng',
          value: landlord?.totalPosts || 0,
          hint: `${landlord?.pendingPosts || 0} tin chờ duyệt`,
          icon: <FiFileText />,
          tone: 'amber',
        },
        {
          label: 'Lịch hẹn',
          value: landlord?.totalAppointments || 0,
          hint: `${landlord?.todayAppointments || 0} lịch hôm nay`,
          icon: <FiCalendar />,
          tone: 'green',
        },
        {
          label: 'Lượt xem',
          value: landlord?.totalViewCount || 0,
          hint: 'Tổng lượt xem tin',
          icon: <FiBarChart2 />,
          tone: 'violet',
        },
      ];

  /* =======================================================
     ADMIN ROOM STATUS
  ======================================================= */

  const adminRoomStats = [
    {
      label: 'Còn trống',
      value: admin?.totalRooms || 0,
      tone: 'green',
    },
    {
      label: 'Đã thuê',
      value: 0,
      tone: 'blue',
    },
    {
      label: 'Đang bảo trì',
      value: 0,
      tone: 'amber',
    },
    {
      label: 'Đã ẩn',
      value: admin?.hiddenPosts || 0,
      tone: 'slate',
    },
  ];

  /* =======================================================
     POST STATUS
  ======================================================= */

  const postStats = isAdmin
    ? [
        {
          label: 'Đã duyệt',
          value: admin?.approvedPosts || 0,
          tone: 'green',
        },
        {
          label: 'Chờ duyệt',
          value: admin?.pendingPosts || 0,
          tone: 'amber',
        },
        {
          label: 'Đã ẩn',
          value: admin?.hiddenPosts || 0,
          tone: 'slate',
        },
      ]
    : [
        {
          label: 'Đã duyệt',
          value: landlord?.approvedPosts || 0,
          tone: 'green',
        },
        {
          label: 'Chờ duyệt',
          value: landlord?.pendingPosts || 0,
          tone: 'amber',
        },
        {
          label: 'Bị từ chối',
          value: landlord?.rejectedPosts || 0,
          tone: 'rose',
        },
      ];

  /* =======================================================
     RENDER
  ======================================================= */

  return (
    <div className="dashboard-shell">

      {/* ===================================================
          SIDEBAR
      =================================================== */}

      <aside
        className={`dashboard-sidebar ${
          menuOpen ? 'is-open' : ''
        }`}
      >

        <div className="dashboard-brand">
          <span className="dashboard-brand-mark">T</span>

          <span>
            Timnhatro<span>.vn</span>
          </span>

          <button
            className="dashboard-close"
            onClick={() => setMenuOpen(false)}
            aria-label="Đóng menu"
          >
            <FiX />
          </button>
        </div>

        <div className="dashboard-role">
          <span className="dashboard-role-dot" />

          {isAdmin
            ? 'Khu vực quản trị viên'
            : 'Khu vực chủ trọ'}
        </div>

        <nav className="dashboard-nav">

          <p className="dashboard-nav-label">
            {isAdmin ? 'Quản trị hệ thống' : 'Quản lý'}
          </p>

          {menu.map((item) => {
            const Icon = item.icon;

            const dashboardRoute = isAdmin
              ? ROUTES.ADMIN_DASHBOARD
              : ROUTES.LANDLORD_DASHBOARD;

            const isActive = item.href === dashboardRoute;

            return (
              <Link
                key={item.href}
                to={item.href}
                className={isActive ? 'is-active' : ''}
                onClick={() => setMenuOpen(false)}
              >
                <Icon />

                <span>{item.label}</span>

                {isActive && (
                  <FiChevronRight className="dashboard-nav-arrow" />
                )}
              </Link>
            );
          })}

        </nav>

        <div className="dashboard-sidebar-bottom">

          <Link to="/">
            <FiArrowUpRight />
            Xem trang chính
          </Link>

          <button onClick={handleLogout}>
            <FiLogOut />
            Đăng xuất
          </button>

        </div>

      </aside>

      {/* ===================================================
          MAIN
      =================================================== */}

      <main className="dashboard-main">

        {/* TOPBAR */}

        <header className="dashboard-topbar">

          <button
            className="dashboard-menu-button"
            onClick={() => setMenuOpen(true)}
            aria-label="Mở menu"
          >
            <FiMenu />
          </button>

          <div className="dashboard-breadcrumb">

            <span>
              {isAdmin ? 'Quản trị hệ thống' : 'Chủ trọ'}
            </span>

            <FiChevronRight />

            <strong>Tổng quan</strong>

          </div>

          <div className="dashboard-top-actions">

            <button
              className="dashboard-icon-button"
              aria-label="Tìm kiếm"
            >
              <FiSearch />
            </button>

            <button
              className="dashboard-icon-button dashboard-notification"
              aria-label="Thông báo"
            >
              <FiBell />
              <i />
            </button>

            <div className="dashboard-user">

              <div className="dashboard-avatar">
                {user?.fullName?.charAt(0) || 'U'}
              </div>

              <div>
                <strong>
                  {user?.fullName || 'Người dùng'}
                </strong>

                <span>
                  {isAdmin
                    ? 'Quản trị viên'
                    : 'Chủ trọ'}
                </span>
              </div>

            </div>

          </div>

        </header>

        {/* CONTENT */}

        <div className="dashboard-content">

          {/* =================================================
              WELCOME
          ================================================= */}

          <section className="dashboard-welcome">

            <div>

              <p className="dashboard-eyebrow">
                {isAdmin
                  ? 'BẢNG ĐIỀU KHIỂN HỆ THỐNG'
                  : 'BẢNG ĐIỀU KHIỂN CHỦ TRỌ'}
              </p>

              <h1>
                Chào{' '}
                {user?.fullName?.split(' ').pop() || 'bạn'}
                <span>!</span>
              </h1>

              <p>
                {isAdmin
                  ? 'Theo dõi và quản lý hoạt động của hệ thống tìm kiếm và cho thuê phòng trọ.'
                  : 'Theo dõi hoạt động và xử lý công việc của bạn trong một nơi.'}
              </p>

            </div>

            {isAdmin ? (
              <Link
                to={ROUTES.ADMIN_POST_APPROVAL}
                className="dashboard-primary-button"
              >
                <FiCheckCircle />
                Duyệt tin mới
              </Link>
            ) : (
              <Link
                to={ROUTES.LANDLORD_CREATE_POST}
                className="dashboard-primary-button"
              >
                <FiPlus />
                Đăng tin mới
              </Link>
            )}

          </section>

          {/* =================================================
              METRICS
          ================================================= */}

          <section className="dashboard-metrics">

            {metrics.map((metric) => (

              <article
                className="dashboard-metric"
                key={metric.label}
              >

                <div
                  className={`dashboard-metric-icon ${metric.tone}`}
                >
                  {metric.icon}
                </div>

                <div>

                  <span>{metric.label}</span>

                  <strong>
                    {loading
                      ? '—'
                      : formatNumber(metric.value)}
                  </strong>

                  <small>{metric.hint}</small>

                </div>

                <FiArrowUpRight className="dashboard-metric-arrow" />

              </article>

            ))}

          </section>

          {/* =================================================
              MAIN GRID
          ================================================= */}

          <section className="dashboard-grid">

            {/* OVERVIEW */}

            <article className="dashboard-panel dashboard-overview-panel">

              <div className="dashboard-panel-heading">

                <div>

                  <h2>
                    {isAdmin
                      ? 'Tổng quan hệ thống'
                      : 'Tình hình vận hành'}
                  </h2>

                  <p>
                    {isAdmin
                      ? 'Các chỉ số chính hiện tại'
                      : 'Tổng hợp phòng trọ và tin đăng'}
                  </p>

                </div>

                <FiPieChart className="dashboard-panel-symbol" />

              </div>

              {isAdmin ? (

                <div className="dashboard-admin-summary">

                  <div className="dashboard-summary-item">
                    <span>Người thuê</span>
                    <strong>
                      {formatNumber(
                        admin?.totalTenants || 0
                      )}
                    </strong>
                  </div>

                  <div className="dashboard-summary-item">
                    <span>Chủ trọ</span>
                    <strong>
                      {formatNumber(
                        admin?.totalLandlords || 0
                      )}
                    </strong>
                  </div>

                  <div className="dashboard-summary-item">
                    <span>Phòng trọ</span>
                    <strong>
                      {formatNumber(
                        admin?.totalRooms || 0
                      )}
                    </strong>
                  </div>

                  <div className="dashboard-summary-item">
                    <span>Tin đăng</span>
                    <strong>
                      {formatNumber(
                        admin?.totalPosts || 0
                      )}
                    </strong>
                  </div>

                </div>

              ) : (

                <div className="dashboard-landlord-summary">

                  <div className="dashboard-summary-big">
                    <span>Phòng còn trống</span>

                    <strong>
                      {formatNumber(
                        landlord?.availableRooms || 0
                      )}
                    </strong>

                    <small>
                      trên tổng số{' '}
                      {formatNumber(
                        landlord?.totalRooms || 0
                      )}{' '}
                      phòng
                    </small>
                  </div>

                  <div className="dashboard-summary-big">
                    <span>Tin đang hiển thị</span>

                    <strong>
                      {formatNumber(
                        landlord?.approvedPosts || 0
                      )}
                    </strong>

                    <small>
                      {formatNumber(
                        landlord?.pendingPosts || 0
                      )}{' '}
                      tin đang chờ duyệt
                    </small>
                  </div>

                </div>

              )}

            </article>

            {/* QUICK ACCESS */}

            <article className="dashboard-panel dashboard-quick-panel">

              <div className="dashboard-panel-heading">

                <div>

                  <h2>Truy cập nhanh</h2>

                  <p>Các tác vụ thường dùng</p>

                </div>

              </div>

              <div className="dashboard-quick-list">

                {(
                  isAdmin
                    ? [
                        {
                          icon: FiCheckCircle,
                          label: 'Duyệt tin đăng',
                          text: `${admin?.pendingPosts || 0} tin đang chờ`,
                          href: ROUTES.ADMIN_POST_APPROVAL,
                        },
                        {
                          icon: FiUsers,
                          label: 'Quản lý người dùng',
                          text: `${admin?.totalUsers || 0} tài khoản`,
                          href: ROUTES.ADMIN_USERS,
                        },
                        {
                          icon: FiHome,
                          label: 'Quản lý phòng',
                          text: `${admin?.totalRooms || 0} phòng`,
                          href: '/admin/rooms',
                        },
                        {
                          icon: FiActivity,
                          label: 'Xử lý báo cáo',
                          text: `${admin?.pendingReports || 0} báo cáo mới`,
                          href: '/admin/reports',
                        },
                      ]
                    : [
                        {
                          icon: FiPlus,
                          label: 'Đăng tin mới',
                          text: 'Tiếp cận người thuê',
                          href: ROUTES.LANDLORD_CREATE_POST,
                        },
                        {
                          icon: FiCalendar,
                          label: 'Xem lịch hẹn',
                          text: `${landlord?.pendingAppointments || 0} lịch cần xác nhận`,
                          href: ROUTES.LANDLORD_APPOINTMENTS,
                        },
                        {
                          icon: FiFileText,
                          label: 'Quản lý tin đăng',
                          text: `${landlord?.approvedPosts || 0} tin đang hiển thị`,
                          href: ROUTES.LANDLORD_POSTS,
                        },
                      ]
                ).map((item) => {

                  const Icon = item.icon;

                  return (
                    <Link
                      to={item.href}
                      key={item.label}
                    >

                      <span className="dashboard-quick-icon">
                        <Icon />
                      </span>

                      <span>

                        <strong>{item.label}</strong>

                        <small>{item.text}</small>

                      </span>

                      <FiChevronRight />

                    </Link>
                  );

                })}

              </div>

            </article>

          </section>

          {/* =================================================
              BOTTOM GRID
          ================================================= */}

          <section className="dashboard-bottom-grid">

            {/* ROOM STATUS ADMIN */}

            {isAdmin ? (

              <article className="dashboard-panel dashboard-status-panel">

                <div className="dashboard-panel-heading">

                  <div>

                    <h2>Tình trạng phòng</h2>

                    <p>
                      Phân bố phòng theo trạng thái
                    </p>

                  </div>

                  <FiHome className="dashboard-panel-symbol" />

                </div>

                <div className="dashboard-status-list">

                  {adminRoomStats.map((item) => (

                    <div
                      className="dashboard-status-row"
                      key={item.label}
                    >

                      <span>

                        <i
                          className={`status-dot ${item.tone}`}
                        />

                        {item.label}

                      </span>

                      <strong>
                        {formatNumber(item.value)}
                      </strong>

                    </div>

                  ))}

                </div>

              </article>

            ) : (

              <article className="dashboard-panel dashboard-status-panel">

                <div className="dashboard-panel-heading">

                  <div>

                    <h2>Trạng thái tin đăng</h2>

                    <p>
                      Cập nhật theo dữ liệu hiện tại
                    </p>

                  </div>

                  <FiPieChart className="dashboard-panel-symbol" />

                </div>

                <div className="dashboard-status-list">

                  {postStats.map((item) => (

                    <div
                      className="dashboard-status-row"
                      key={item.label}
                    >

                      <span>

                        <i
                          className={`status-dot ${item.tone}`}
                        />

                        {item.label}

                      </span>

                      <strong>
                        {formatNumber(item.value)}
                      </strong>

                    </div>

                  ))}

                </div>

              </article>

            )}

            {/* POST STATUS */}

            {isAdmin ? (

              <article className="dashboard-panel dashboard-status-panel">

                <div className="dashboard-panel-heading">

                  <div>

                    <h2>Trạng thái tin đăng</h2>

                    <p>
                      Theo dõi tình trạng kiểm duyệt
                    </p>

                  </div>

                  <FiFileText className="dashboard-panel-symbol" />

                </div>

                <div className="dashboard-status-list">

                  {postStats.map((item) => (

                    <div
                      className="dashboard-status-row"
                      key={item.label}
                    >

                      <span>

                        <i
                          className={`status-dot ${item.tone}`}
                        />

                        {item.label}

                      </span>

                      <strong>
                        {formatNumber(item.value)}
                      </strong>

                    </div>

                  ))}

                </div>

              </article>

            ) : (

              <article className="dashboard-panel dashboard-note-panel">

                <div className="dashboard-note-art">
                  <FiShield />
                </div>

                <div>

                  <p className="dashboard-eyebrow">
                    GỢI Ý
                  </p>

                  <h2>
                    Tin đăng rõ ràng, phòng nhanh đầy
                  </h2>

                  <p>
                    Ảnh sáng, tiêu đề cụ thể và thông
                    tin chi phí minh bạch giúp tăng
                    lượt xem chất lượng.
                  </p>

                  <Link
                    to={ROUTES.LANDLORD_CREATE_POST}
                  >
                    Tối ưu tin đăng
                    <FiArrowUpRight />
                  </Link>

                </div>

              </article>

            )}

          </section>

          {/* =================================================
              ADMIN NOTE
          ================================================= */}

          {isAdmin && (

            <section className="dashboard-admin-note">

              <div className="dashboard-admin-note-icon">
                <FiShield />
              </div>

              <div>

                <p className="dashboard-eyebrow">
                  KIỂM SOÁT HỆ THỐNG
                </p>

                <h2>
                  Duy trì nội dung phòng trọ minh bạch
                </h2>

                <p>
                  Kiểm tra các tin đăng đang chờ duyệt,
                  xử lý báo cáo vi phạm và đảm bảo thông
                  tin phòng trọ trên hệ thống chính xác.
                </p>

              </div>

              <Link
                to={ROUTES.ADMIN_POST_APPROVAL}
                className="dashboard-secondary-button"
              >
                Mở hàng đợi duyệt
                <FiArrowUpRight />
              </Link>

            </section>

          )}

        </div>

      </main>

    </div>
  );
};

export default DashboardPage;