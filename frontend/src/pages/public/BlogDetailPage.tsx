import React, { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { FiArrowLeft, FiCalendar, FiEye } from 'react-icons/fi';
import { blogService } from '../../services/blogService';
import { BlogPost } from '../../types/blog.types';

const formatDate = (date?: string | null) => date
  ? new Date(date).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' })
  : 'Chưa cập nhật';

const BlogDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [post, setPost] = useState<BlogPost | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;
    const postId = Number(id);

    const loadPost = async () => {
      if (!Number.isInteger(postId) || postId <= 0) {
        setError('Bài viết không hợp lệ.');
        setLoading(false);
        return;
      }

      setLoading(true);
      setError(null);
      try {
        const response = await blogService.getPostById(postId);
        if (!active) return;
        setPost(response.data || null);
        if (!response.data) setError('Không tìm thấy bài viết.');
      } catch {
        if (active) setError('Không thể tải bài viết.');
      } finally {
        if (active) setLoading(false);
      }
    };

    void loadPost();
    return () => { active = false; };
  }, [id]);

  if (loading) return <div className="mx-auto max-w-4xl px-4 py-16"><div className="h-8 w-2/3 animate-pulse bg-slate-200" /></div>;

  if (error || !post) {
    return <div className="mx-auto max-w-4xl px-4 py-16"><p className="text-slate-600">{error || 'Không tìm thấy bài viết.'}</p><Link to="/blog" className="mt-5 inline-flex items-center gap-2 font-semibold text-[#0084ff]"><FiArrowLeft />Quay lại Blog</Link></div>;
  }

  return (
    <article className="mx-auto max-w-4xl px-4 py-10 sm:py-14">
      <Link to="/blog" className="mb-6 inline-flex items-center gap-2 text-sm font-semibold text-[#0084ff]"><FiArrowLeft />Tất cả bài viết</Link>
      <h1 className="text-3xl font-bold text-slate-900 sm:text-4xl">{post.title}</h1>
      <div className="mt-4 flex flex-wrap items-center gap-4 text-sm text-slate-600"><span>Bởi {post.authorName}</span><span className="flex items-center gap-1"><FiCalendar />{formatDate(post.publishedAt || post.updatedAt)}</span><span className="flex items-center gap-1"><FiEye />{post.viewCount} lượt xem</span></div>
      {post.imageUrl && <img src={post.imageUrl} alt={post.title} className="mt-8 aspect-[16/9] w-full rounded-lg object-cover" />}
      {post.summary && <p className="mt-8 text-lg leading-8 text-slate-700">{post.summary}</p>}
      <div className="mt-8 whitespace-pre-line leading-8 text-slate-800">{post.content}</div>
    </article>
  );
};

export default BlogDetailPage;
