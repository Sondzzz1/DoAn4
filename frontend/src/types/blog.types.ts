export interface BlogPost {
  id: number;
  title: string;
  slug: string;
  summary?: string | null;
  content: string;
  imageUrl?: string | null;
  authorAccountId: number;
  authorName: string;
  viewCount: number;
  status: number;
  publishedAt?: string | null;
  updatedAt?: string | null;
  commentCount: number;
  comments?: BlogComment[];
}

export interface BlogComment {
  id: number;
  blogPostId: number;
  accountId: number;
  authorName: string;
  authorAvatar?: string | null;
  content: string;
  createdAt: string;
}
