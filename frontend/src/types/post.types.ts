/**
 * Post Status
 */
export enum PostStatus {
  Pending = 0,
  Approved = 1,
  Rejected = 2,
  Hidden = 3,
  Expired = 4,
}

/**
 * Room Status
 */
export enum RoomStatus {
  Available = 0,
  Rented = 1,
  Reserved = 2,
  TemporarilyUnavailable = 3,
}

/**
 * Amenity
 */
export interface Amenity {
  id: number;
  name: string;
  icon?: string | null;
  description?: string | null;
  isActive?: boolean;
}

/**
 * Post List Item (rút gọn)
 */
export interface PostListItem {
  id: number;
  title: string;
  price: number;
  status: PostStatus;
  area: number;
  maxOccupants: number;
  roomStatus: RoomStatus;
  province: string;
  district: string;
  ward: string;
  address?: string;
  latitude?: number;
  longitude?: number;
  distanceInKm?: number;
  thumbnailUrl: string | null;
  categoryId: number;
  categoryName: string;
  landlordName?: string;
  landlordPhone?: string | null;
  createdAt: string;
  updatedAt: string;
}

/**
 * Post Detail (đầy đủ)
 */
export interface Post {
  id: number;
  title: string;
  description: string;
  price: number;
  status: PostStatus;
  rejectionReason: string | null;
  
  // Landlord Info
  landlordId: number;
  landlordAccountId?: number;
  landlordName: string;
  landlordPhone: string;
  
  // Room Info
  roomId: number;
  categoryId: number;
  area: number;
  maxOccupants: number;
  roomStatus: RoomStatus;
  province: string;
  district: string;
  ward: string;
  address: string;
  latitude?: number;
  longitude?: number;
  distanceInKm?: number;
  
  // Amenities & Images
  amenities: Amenity[];
  imageUrls: string[];
  
  createdAt: string;
  updatedAt: string;
}

/**
 * Create Post Request
 */
export interface CreatePostRequest {
  roomId: number;
  title: string;
  description: string;
}

export interface UpdatePostRequest {
  title: string;
  description: string;
}

/**
 * Search/Filter Params
 */
export interface PostSearchParams {
  keyword?: string;
  province?: string;
  district?: string;
  ward?: string;
  minPrice?: number;
  maxPrice?: number;
  minArea?: number;
  maxArea?: number;
  categoryId?: number;
  latitude?: number;
  longitude?: number;
  radiusInKm?: number;
  sortBy?: string;
  isDescending?: boolean;
  pageIndex?: number;
  pageNumber?: number;
  maxOccupants?: number;
  amenityIds?: number[];
  pageSize?: number;
}

export type PostQueryParams = PostSearchParams;

export interface PostSearchResult {
  items: PostListItem[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}
