import React from 'react';
import { Link } from 'react-router-dom';
import { FiClock, FiEye, FiCalendar } from 'react-icons/fi';
import { BlogPost } from '../../types/blog.types';

import './BlogCard.css';

interface BlogCardProps {
  post: BlogPost;
}

const BlogCard: React.FC<BlogCardProps> = ({ post }) => {
  const formatDate = (date: string) => {
    return new Date(date).toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
    });
  };

  const formatViews = (views?: number) => {
    if (!views) return '0';
    if (views >= 1000) {
      return `${(views / 1000).toFixed(1)}K`;
    }
    return views.toString();
  };

  return (
    <Link to={`/blog/${post.id}`} className="blog-card">
      <div className="blog-card-image-wrapper">
        <img
          src={post.imageUrl}
          alt={post.title}
          className="blog-card-image"
        />

        <div className="blog-card-category-badge">
          {post.category}
        </div>
      </div>

      <div className="blog-card-content">
        <h3 className="blog-card-title">{post.title}</h3>

        <p className="blog-card-excerpt">{post.excerpt}</p>

        <div className="blog-card-meta">
          <div className="blog-card-meta-item">
            <FiCalendar />
            <span>{formatDate(post.publishedAt)}</span>
          </div>

          <div className="blog-card-meta-item">
            <FiClock />
            <span>{post.readTime} phút đọc</span>
          </div>

          {post.views && (
            <div className="blog-card-meta-item">
              <FiEye />
              <span>{formatViews(post.views)} lượt xem</span>
            </div>
          )}
        </div>

        <div className="blog-card-footer">
          <span className="blog-card-author">
            Bởi {post.author}
          </span>

          <span className="blog-card-read-more">
            Đọc tiếp →
          </span>
        </div>
      </div>
    </Link>
  );
};

export default BlogCard;
