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
  TemporarilyUnavailable = 2,
}

/**
 * Amenity
 */
export interface Amenity {
  id: number;
  name: string;
  icon: string;
  description: string;
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
  thumbnailUrl: string | null;
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
  landlordName: string;
  landlordPhone: string;
  
  // Room Info
  roomId: number;
  area: number;
  maxOccupants: number;
  roomStatus: RoomStatus;
  province: string;
  district: string;
  ward: string;
  address: string;
  
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
  title: string;
  description: string;
  price: number;
  area: number;
  maxOccupants: number;
  province: string;
  district: string;
  ward: string;
  address: string;
  amenityIds: number[];
  imageUrls: string[];
}

/**
 * Update Post Request
 */
export interface UpdatePostRequest {
  title: string;
  description: string;
  price: number;
  area: number;
  maxOccupants: number;
  province: string;
  district: string;
  ward: string;
  address: string;
  amenityIds: number[];
  imageUrls: string[];
}

/**
 * Search/Filter Params
 */
export interface PostSearchParams {
  province?: string;
  district?: string;
  minPrice?: number;
  maxPrice?: number;
  minArea?: number;
  maxArea?: number;
}
