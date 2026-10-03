import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiChevronRight } from 'react-icons/fi';
import BlogCard from '../../components/blog/BlogCard';
import { blogService } from '../../services/blogService';
import { BlogPost } from '../../types/blog.types';
import './BlogPage.css';

const BlogPage: React.FC = () => {
  const [posts, setPosts] = useState<BlogPost[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  async function loadPosts() {
    setLoading(true);
    setError(null);

    try {
      const response = await blogService.getPublishedPosts();
      setPosts(response.data || []);
    } catch {
      setPosts([]);
      setError('Không thể tải danh sách bài viết.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void Promise.resolve().then(loadPosts);
  }, []);

  return (
    <div className="blog-page">
      <section className="blog-hero">
        <div className="blog-hero-overlay" />
        <div className="blog-hero-content">
          <nav className="blog-breadcrumb"><Link to="/">Trang chủ</Link><FiChevronRight /><span>Blog</span></nav>
          <h1 className="blog-hero-title">Blog Tìm Nhà Trọ</h1>
          <p className="blog-hero-subtitle">Chia sẻ kinh nghiệm và kiến thức hữu ích trong quá trình thuê phòng trọ, nhà nguyên căn.</p>
        </div>
      </section>

      <main className="blog-container">
        <div className="blog-list-header">
          <h2>Bài viết mới nhất</h2>
          {!loading && !error && <p>Hiện có {posts.length} bài viết</p>}
        </div>

        {loading ? (
          <div className="blog-grid">
            {Array.from({ length: 6 }, (_, index) => <div key={index} className="h-80 animate-pulse rounded-lg bg-slate-200" />)}
          </div>
        ) : error ? (
          <div className="blog-empty"><p>{error}</p><button type="button" onClick={() => void loadPosts()} className="mt-4 rounded bg-[#0084ff] px-4 py-2 font-semibold text-white">Thử lại</button></div>
        ) : posts.length > 0 ? (
          <div className="blog-grid">{posts.map(post => <BlogCard key={post.id} post={post} />)}</div>
        ) : (
          <div className="blog-empty"><p>Chưa có bài viết nào được xuất bản.</p></div>
        )}
      </main>
    </div>
  );
};

export default BlogPage;
