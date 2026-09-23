import React, { useEffect, useState } from 'react';
import { FiStar, FiMessageSquare, FiUser } from 'react-icons/fi';
import { rentalService, ReviewDto } from '../../services/rentalService';

interface RoomReviewsProps {
  postId: number;
}

const RoomReviews: React.FC<RoomReviewsProps> = ({ postId }) => {
  const [reviews, setReviews] = useState<ReviewDto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchReviews = async () => {
      setLoading(true);
      try {
        const res = await rentalService.getReviewsByPost(postId);
        setReviews(res.data || []);
      } catch (err) {
        // Silently handle if no reviews or offline
        setReviews([]);
      } finally {
        setLoading(false);
      }
    };

    if (postId) {
      void fetchReviews();
    }
  }, [postId]);

  const avgRating =
    reviews.length > 0
      ? (reviews.reduce((acc, r) => acc + r.soSao, 0) / reviews.length).toFixed(1)
      : null;

  return (
    <div className="bg-white rounded-3xl border border-slate-200/80 p-6 md:p-8 shadow-sm mt-8">
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-5 mb-6">
        <div>
          <h3 className="text-xl font-bold text-slate-900 flex items-center gap-2">
            <FiMessageSquare className="text-[#0084ff]" /> Đánh giá từ người thuê phòng ({reviews.length})
          </h3>
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

      {loading ? (
        <div className="text-center py-8 text-slate-400 text-sm animate-pulse">
          Đang tải đánh giá...
        </div>
      ) : reviews.length === 0 ? (
        <div className="text-center py-8 bg-slate-50 rounded-2xl border border-slate-100">
          <FiStar className="mx-auto text-3xl text-slate-300 mb-2" />
          <p className="text-sm text-slate-600 font-semibold">Chưa có đánh giá nào cho phòng này</p>
          <p className="text-xs text-slate-400 mt-1">
            Người thuê sau khi hoàn thành hợp đồng sẽ có thể để lại đánh giá tại đây.
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

                <div className="flex text-amber-400 text-xs">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <FiStar key={s} className={s <= item.soSao ? 'fill-amber-400' : 'text-slate-200'} />
                  ))}
                </div>
              </div>

              <p className="text-xs text-slate-700 leading-relaxed pl-10">
                {item.nhanXet || 'Người thuê đã đánh giá ' + item.soSao + ' sao.'}
              </p>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

export default RoomReviews;
