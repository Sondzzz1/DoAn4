import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { FiStar, FiMessageSquare, FiUser } from 'react-icons/fi';
import { rentalService, ReviewDto } from '../../services/rentalService';
import { useAuth } from '../../hooks/useAuth';
import { ROLES, ROUTES } from '../../utils/constants';
import { getApiErrorMessage } from '../../utils/apiError';

interface RoomReviewsProps {
  postId: number;
}

const RoomReviews: React.FC<RoomReviewsProps> = ({ postId }) => {
  const { isAuthenticated, user } = useAuth();
  const [reviews, setReviews] = useState<ReviewDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [revision, setRevision] = useState(0);

  useEffect(() => {
    let active = true;
    const fetchReviews = async () => {
      if (!active) return;
      setLoading(true);
      setError('');
      try {
        const res = await rentalService.getReviewsByPost(postId);
        if (active) setReviews(res.data || []);
      } catch (error) {
        if (!active) return;
        setReviews([]);
        setError(getApiErrorMessage(error, 'Không thể tải đánh giá. Vui lòng thử lại.'));
      } finally {
        if (active) setLoading(false);
      }
    };

    if (postId) {
      void Promise.resolve().then(fetchReviews);
    }
    return () => { active = false; };
  }, [postId, revision]);

  const avgRating =
    reviews.length > 0
      ? (reviews.reduce((acc, r) => acc + r.soSao, 0) / reviews.length).toFixed(1)
      : null;

  return (
    <section id="room-reviews" aria-labelledby="room-reviews-title" className="scroll-mt-24 bg-white rounded-3xl border border-slate-200/80 p-4 sm:p-6 md:p-8 shadow-sm mt-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-5 mb-6">
        <div>
          <h2 id="room-reviews-title" className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <FiMessageSquare className="text-[#0084ff]" /> Đánh giá từ người thuê phòng ({reviews.length})
          </h2>
          <p className="text-xs text-slate-500 mt-1">
            Những đánh giá chân thực từ khách thuê đã từng ở tại phòng trọ này.
          </p>
        </div>

        {avgRating && (
          <div className="flex items-center gap-3 bg-amber-50 border border-amber-100 px-4 py-2 rounded-2xl">
            <div className="text-2xl font-black text-amber-500">{avgRating}</div>
            <div>
              <div className="flex text-amber-400 text-sm">
                {[1, 2, 3, 4, 5].map((s) => (
                  <FiStar
                    key={s}
                    className={s <= Math.round(Number(avgRating)) ? 'fill-amber-400' : 'text-slate-200'}
                  />
                ))}
              </div>
              <span className="text-[10px] text-slate-400 block mt-0.5">Dựa trên {reviews.length} đánh giá</span>
            </div>
          </div>
        )}
      </div>

      {(!isAuthenticated || user?.role === ROLES.TENANT) && (
        <div className="mb-6 flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-2xl bg-blue-50 p-4">
          <p className="text-sm text-slate-600">Đang thuê hoặc đã thuê phòng/căn hộ? Chọn hợp đồng của bạn để chia sẻ trải nghiệm.</p>
          <Link to={isAuthenticated ? `${ROUTES.TENANT_RENTALS}?tab=reviews` : ROUTES.LOGIN}
            className="inline-flex shrink-0 items-center justify-center gap-2 rounded-xl bg-[#0084ff] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#0073e6]">
            <FiStar /> {isAuthenticated ? 'Viết đánh giá' : 'Đăng nhập để đánh giá'}
          </Link>
        </div>
      )}

      {loading ? (
        <div className="text-center py-8 text-slate-400 text-sm animate-pulse">
          Đang tải đánh giá...
        </div>
      ) : error ? (
        <div className="rounded-2xl border border-red-100 bg-red-50 p-4">
          <p role="alert" className="text-sm text-red-700">{error}</p>
          <button type="button" onClick={() => setRevision(previous => previous + 1)} className="mt-3 text-sm font-semibold text-[#0084ff]">Thử lại</button>
        </div>
      ) : reviews.length === 0 ? (
        <div className="text-center py-8 bg-slate-50 rounded-2xl border border-slate-100">
          <FiStar className="mx-auto text-3xl text-slate-300 mb-2" />
          <p className="text-sm text-slate-600 font-semibold">Chưa có đánh giá nào cho phòng này</p>
          <p className="text-xs text-slate-400 mt-1">
            Người đang thuê hoặc đã thuê có thể viết đánh giá trong mục Thuê phòng → Đánh giá.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          {reviews.map((item) => (
            <div
              key={item.id}
              className="bg-slate-50/70 border border-slate-100 rounded-2xl p-4 transition-all hover:bg-slate-50"
            >
              <div className="flex items-center justify-between mb-2">
                <div className="flex items-center gap-2">
                  <div className="w-8 h-8 rounded-full bg-blue-100 text-[#0084ff] flex items-center justify-center font-bold text-xs">
                    <FiUser />
                  </div>
                  <div>
                    <span className="font-bold text-slate-800 text-xs">Khách thuê phòng</span>
                    <span className="text-[10px] text-slate-400 block">
                      {new Date(item.ngayTao).toLocaleDateString('vi-VN')}
                    </span>
                  </div>
                </div>

                <div className="flex text-amber-400 text-xs" role="img" aria-label={`${item.soSao} / 5 sao`}>
                  {[1, 2, 3, 4, 5].map((s) => (
                    <FiStar key={s} className={s <= item.soSao ? 'fill-amber-400' : 'text-slate-200'} />
                  ))}
                </div>
              </div>

              <p className="break-words whitespace-pre-line text-sm text-slate-700 leading-relaxed sm:pl-10">
                {item.nhanXet || 'Người thuê đã đánh giá ' + item.soSao + ' sao.'}
              </p>
            </div>
          ))}
        </div>
      )}
    </section>
  );
};

export default RoomReviews;
