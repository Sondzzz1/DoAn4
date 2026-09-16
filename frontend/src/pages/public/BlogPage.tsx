import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { FiChevronRight } from 'react-icons/fi';

import BlogCard from '../../components/blog/BlogCard';
import { BlogPost } from '../../types/blog.types';

import './BlogPage.css';

// Demo data
const DEMO_POSTS: BlogPost[] = [
  {
    id: 1,
    title: '10 Kinh nghiệm thuê phòng trọ an toàn cho sinh viên mới',
    excerpt:
      'Hướng dẫn chi tiết về cách tìm kiếm, xem phòng và ký hợp đồng thuê phòng trọ an toàn, tránh các rủi ro thường gặp.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=800',
    author: 'Admin',
    category: 'Kinh nghiệm',
    publishedAt: '2026-08-10',
    readTime: 5,
    views: 1234,
  },

  {
    id: 2,
    title: 'Hướng dẫn ký hợp đồng thuê nhà: Những điều cần lưu ý',
    excerpt:
      'Các điều khoản quan trọng trong hợp đồng thuê nhà, quyền lợi của người thuê và cách bảo vệ quyền lợi của mình.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1450101499163-c8848c66ca85?w=800',
    author: 'Admin',
    category: 'Pháp lý',
    publishedAt: '2026-08-08',
    readTime: 7,
    views: 982,
  },

  {
    id: 3,
    title: 'Top 5 khu vực cho thuê phòng trọ sinh viên giá rẻ tại Hà Nội',
    excerpt:
      'Khám phá những khu vực có giá thuê phòng trọ phù hợp với túi tiền sinh viên, gần trường học và đầy đủ tiện ích.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=800',
    author: 'Admin',
    category: 'Địa điểm',
    publishedAt: '2026-08-05',
    readTime: 6,
    views: 1567,
  },

  {
    id: 4,
    title: 'Cách trang trí phòng trọ đẹp với chi phí thấp',
    excerpt:
      'Những ý tưởng trang trí phòng trọ đơn giản, tiết kiệm nhưng vẫn tạo không gian sống ấm cúng và đẹp mắt.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=800',
    author: 'Admin',
    category: 'Trang trí',
    publishedAt: '2026-08-03',
    readTime: 8,
    views: 2103,
  },

  {
    id: 5,
    title: 'Quyền lợi của người thuê nhà theo quy định pháp luật',
    excerpt:
      'Tìm hiểu về các quyền lợi hợp pháp của người thuê nhà, nghĩa vụ của chủ nhà và cách giải quyết tranh chấp.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1589829545856-d10d557cf95f?w=800',
    author: 'Admin',
    category: 'Pháp lý',
    publishedAt: '2026-08-01',
    readTime: 10,
    views: 876,
  },

  {
    id: 6,
    title: 'Checklist xem phòng trọ: Những điều cần kiểm tra',
    excerpt:
      'Danh sách chi tiết những gì cần kiểm tra khi đi xem phòng trọ để tránh những bất ngờ không mong muốn.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1600585154340-be6161a56a0c?w=800',
    author: 'Admin',
    category: 'Kinh nghiệm',
    publishedAt: '2026-07-28',
    readTime: 5,
    views: 1456,
  },

  {
    id: 7,
    title: 'So sánh giá thuê phòng trọ giữa các quận tại TP.HCM',
    excerpt:
      'Phân tích và so sánh mức giá thuê phòng trọ ở các quận khác nhau tại Thành phố Hồ Chí Minh.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=800',
    author: 'Admin',
    category: 'Địa điểm',
    publishedAt: '2026-07-25',
    readTime: 9,
    views: 1789,
  },

  {
    id: 8,
    title: 'Mẹo tiết kiệm chi phí sinh hoạt khi thuê trọ',
    excerpt:
      'Những cách thức đơn giản giúp bạn tiết kiệm tiền điện, nước và các chi phí sinh hoạt khác khi thuê phòng trọ.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1554224155-8d04cb21cd6c?w=800',
    author: 'Admin',
    category: 'Kinh nghiệm',
    publishedAt: '2026-07-20',
    readTime: 6,
    views: 1324,
  },

  {
    id: 9,
    title: 'Xu hướng thiết kế phòng trọ hiện đại 2026',
    excerpt:
      'Khám phá những xu hướng thiết kế nội thất phòng trọ được ưa chuộng nhất trong năm 2026.',
    content: '',
    imageUrl:
      'https://images.unsplash.com/photo-1616486338812-3dadae4b4ace?w=800',
    author: 'Admin',
    category: 'Trang trí',
    publishedAt: '2026-07-18',
    readTime: 7,
    views: 2456,
  },
];

const CATEGORIES = [
  'Tất cả',
  'Kinh nghiệm',
  'Pháp lý',
  'Địa điểm',
  'Trang trí',
];

const BlogPage: React.FC = () => {
  const [selectedCategory, setSelectedCategory] = useState('Tất cả');

  const filteredPosts =
    selectedCategory === 'Tất cả'
      ? DEMO_POSTS
      : DEMO_POSTS.filter((post) => post.category === selectedCategory);

  return (
    <div className="blog-page">
      {/* =========================================
          HERO SECTION
      ========================================= */}
      <section className="blog-hero">
        <div className="blog-hero-overlay" />

        <div className="blog-hero-content">
          <nav className="blog-breadcrumb">
            <Link to="/">Trang chủ</Link>
            <FiChevronRight />
            <span>Blog</span>
          </nav>

          <h1 className="blog-hero-title">Blog Tìm Nhà Trọ</h1>

          <p className="blog-hero-subtitle">
            Chia sẻ kinh nghiệm, kiến thức và mẹo hữu ích về thuê phòng trọ,
            nhà nguyên căn
          </p>
        </div>
      </section>

      {/* =========================================
          CATEGORIES
      ========================================= */}
      <div className="blog-categories-wrapper">
        <div className="blog-categories">
          {CATEGORIES.map((cat) => (
            <button
              key={cat}
              className={`blog-category-btn ${
                selectedCategory === cat ? 'active' : ''
              }`}
              onClick={() => setSelectedCategory(cat)}
            >
              {cat}
            </button>
          ))}
        </div>
      </div>

      {/* =========================================
          BLOG LIST
      ========================================= */}
      <main className="blog-container">
        <div className="blog-list-header">
          <h2>
            {selectedCategory === 'Tất cả'
              ? 'Tất cả bài viết'
              : `Danh mục: ${selectedCategory}`}
          </h2>

          <p>Hiện có {filteredPosts.length} bài viết</p>
        </div>

        <div className="blog-grid">
          {filteredPosts.length > 0 ? (
            filteredPosts.map((post) => <BlogCard key={post.id} post={post} />)
          ) : (
            <div className="blog-empty">
              <p>Chưa có bài viết nào trong danh mục này.</p>
            </div>
          )}
        </div>
      </main>
    </div>
  );
};

export default BlogPage;
