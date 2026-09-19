// API Base URL
export const API_BASE_URL = 'http://localhost:5000/api';

// Role Names
export const ROLES = {
  TENANT: 'Tenant',
  LANDLORD: 'Landlord',
  ADMIN: 'Admin',
} as const;

// Post Status
export const POST_STATUS = {
  PENDING: 0,
  APPROVED: 1,
  REJECTED: 2,
  HIDDEN: 3,
  EXPIRED: 4,
} as const;

// Room Status
export const ROOM_STATUS = {
  AVAILABLE: 0,
  RENTED: 1,
  TEMPORARILY_UNAVAILABLE: 2,
} as const;

// Appointment Status
export const APPOINTMENT_STATUS = {
  PENDING: 0,
  APPROVED: 1,
  REJECTED: 2,
  CANCELLED: 3,
  COMPLETED: 4,
} as const;

// Status Labels (Vietnamese)
export const POST_STATUS_LABELS = {
  [POST_STATUS.PENDING]: 'Chờ duyệt',
  [POST_STATUS.APPROVED]: 'Đã duyệt',
  [POST_STATUS.REJECTED]: 'Bị từ chối',
  [POST_STATUS.HIDDEN]: 'Đã ẩn',
  [POST_STATUS.EXPIRED]: 'Hết hạn',
} as const;

export const ROOM_STATUS_LABELS = {
  [ROOM_STATUS.AVAILABLE]: 'Còn trống',
  [ROOM_STATUS.RENTED]: 'Đã cho thuê',
  [ROOM_STATUS.TEMPORARILY_UNAVAILABLE]: 'Tạm không available',
} as const;

export const APPOINTMENT_STATUS_LABELS = {
  [APPOINTMENT_STATUS.PENDING]: 'Chờ xác nhận',
  [APPOINTMENT_STATUS.APPROVED]: 'Đã xác nhận',
  [APPOINTMENT_STATUS.REJECTED]: 'Đã từ chối',
  [APPOINTMENT_STATUS.CANCELLED]: 'Đã hủy',
  [APPOINTMENT_STATUS.COMPLETED]: 'Đã hoàn thành',
} as const;

// Local Storage Keys
export const STORAGE_KEYS = {
  TOKEN: 'room_rental_token',
  USER: 'room_rental_user',
} as const;

// Routes
export const ROUTES = {
  HOME: '/',
  LOGIN: '/login',
  REGISTER: '/register',
  ROOM_LIST: '/rooms',
  ROOM_DETAIL: '/rooms', // Base path, append /:id for router or /id for links
  
  // Tenant
  TENANT_PROFILE: '/tenant/profile',
  TENANT_FAVORITES: '/tenant/favorites',
  TENANT_APPOINTMENTS: '/tenant/appointments',
  
  // Landlord
  LANDLORD_DASHBOARD: '/landlord/dashboard',
  LANDLORD_POSTS: '/landlord/posts',
  LANDLORD_CREATE_POST: '/landlord/posts/create',
  LANDLORD_EDIT_POST: '/landlord/posts/:id/edit',
  LANDLORD_APPOINTMENTS: '/landlord/appointments',
  
  // Admin
  ADMIN_DASHBOARD: '/admin/dashboard',
  ADMIN_USERS: '/admin/users',
  ADMIN_POSTS: '/admin/posts',
  ADMIN_POST_APPROVAL: '/admin/posts/approval',
  ADMIN_AMENITIES: '/admin/amenities',
  ADMIN_ROOMS: '/admin/rooms',
  ADMIN_CATEGORIES: '/admin/categories',
  ADMIN_REPORTS: '/admin/reports',
} as const;
