export interface BlogPost {
  id: number;
  title: string;
  excerpt: string;
  content: string;
  imageUrl: string;
  author: string;
  category: string;
  publishedAt: string;
  readTime: number; // minutes
  views?: number;
}

export interface BlogCategory {
  id: number;
  name: string;
  slug: string;
  description: string;
}
