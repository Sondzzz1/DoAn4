/**
 * User Profile
 */
export interface UserProfile {
  id: number;
  fullName: string;
  email: string;
  phone: string | null;
  avatarUrl: string | null;
  role: string;
  isBlocked: boolean;
  createdAt: string;
}

/**
 * Update Profile Request
 */
export interface UpdateProfileRequest {
  fullName: string;
  phone: string;
  avatarUrl: string | null;
}

/**
 * Change Password Request
 */
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
