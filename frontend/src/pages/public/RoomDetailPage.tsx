import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { postService } from '../../services/postService';
import { Post } from '../../types/post.types';
import { formatPrice, formatDate, getFullAddress } from '../../utils/helpers';
import { ROOM_STATUS_LABELS } from '../../utils/constants';
import Spinner from '../../components/common/Spinner';
import Button from '../../components/common/Button';
import { toast } from 'react-toastify';
import { FiHeart, FiShare2, FiMapPin, FiChevronLeft, FiChevronRight, FiPhone, FiCalendar, FiMaximize2, FiUsers, FiHome } from 'react-icons/fi';

const SIMILAR = [
  { id: 10, title: 'Cho thuê Chung Cư Gia Phúc, Linh Chiểu, Thủ Đức – 2PN 1WC...', price: '8 Triệu/tháng', area: 67, date: '04/07/2026', img: 'https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=160&h=120&fit=crop' },
  { id: 11, title: 'Cho thuê Chung Cư Gia Phúc, Linh Chiểu, Thủ Đức – 2PN 1WC...', price: '8 Triệu/tháng', area: 70, date: '04/07/2026', img: 'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=160&h=120&fit=crop' },
  { id: 12, title: 'Studio full nội thất – Vào ở ngay', price: '6.6 Triệu/tháng', area: 25, date: '30/06/2026', img: 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=160&h=120&fit=crop' },
  { id: 13, title: 'Cho thuê Chung Cư 4S LINH ĐÔNG, Full Nội Thất 2PN, 2WC...', price: '7.5 Triệu/tháng', area: 55, date: '28/06/2026', img: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=160&h=120&fit=crop' },
];

const DEMO_IMGS = [
  'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=1200&h=700&fit=crop',
  'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=1200&h=700&fit=crop',
  'https://images.unsplash.com/photo-1556020685-ae41abfc9365?w=1200&h=700&fit=crop',
  'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=1200&h=700&fit=crop',
];

const RoomDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [post, setPost] = useState<Post | null>(null);
  const [loading, setLoading] = useState(true);
  const [imgIdx, setImgIdx] = useState(0);
  const [showPhone, setShowPhone] = useState(false);

  useEffect(() => { if (id) fetchPost(parseInt(id)); }, [id]);

  const fetchPost = async (pid: number) => {
    setLoading(true);
    try { const r = await postService.getPostById(pid); setPost(r.data); }
    catch { toast.error('Không thể tải thông tin phòng'); navigate('/rooms'); }
    finally { setLoading(false); }
  };

  if (loading) return <Spinner fullScreen/>;
  if (!post) return null;

  const imgs = post.imageUrls?.length ? post.imageUrls : DEMO_IMGS;
  const addr = getFullAddress(post.address, post.ward, post.district, post.province);
  const prev = () => setImgIdx(i => (i === 0 ? imgs.length - 1 : i - 1));
  const next = () => setImgIdx(i => (i === imgs.length - 1 ? 0 : i + 1));

  return (
    <div className="bg-gray-50 min-h-screen min-w-[1200px]">
      {/* Breadcrumb */}
      <div className="bg-white border-b border-gray-100">
        <div className="max-w-[1200px] mx-auto px-4 py-3.5">
          <nav className="flex items-center gap-2 text-[14px] text-gray-500 font-medium">
            {[['Trang chủ','/'],['Cho thuê căn hộ','/rooms'],['Hồ Chí Minh',''],['Tân Bình','']].map(([label, href], i) => (
              <React.Fragment key={i}>
                {i > 0 && <FiChevronRight className="w-4 h-4 text-gray-400"/>}
                {href ? <button onClick={() => navigate(href)} className="hover:text-blue-600 transition-colors">{label}</button> : <span>{label}</span>}
              </React.Fragment>
            ))}
            <FiChevronRight className="w-4 h-4 text-gray-400"/>
            <span className="text-orange-600 font-bold uppercase truncate max-w-[400px]">{post.title}</span>
          </nav>
        </div>
      </div>

      <div className="max-w-[1200px] mx-auto px-4 py-8">
        <div className="flex gap-8">

          {/* ── MAIN (Trái) ── */}
          <div className="w-[800px] flex-shrink-0 flex flex-col gap-6">
            {/* Gallery */}
            <div className="bg-white rounded-3xl overflow-hidden shadow-sm border border-gray-100 p-2">
              <div className="relative bg-black rounded-2xl overflow-hidden" style={{ height: '500px' }}>
                <img src={imgs[imgIdx]} alt={post.title} className="w-full h-full object-cover"/>
                <div className="absolute top-4 right-4 flex gap-3">
                  <button className="w-12 h-12 bg-white/95 rounded-full flex items-center justify-center shadow-lg hover:bg-red-50 hover:text-red-500 transition-all"><FiHeart className="w-6 h-6 text-gray-600 hover:text-red-500"/></button>
                  <button className="w-12 h-12 bg-white/95 rounded-full flex items-center justify-center shadow-lg hover:bg-blue-50 transition-all"><FiShare2 className="w-6 h-6 text-gray-600"/></button>
                </div>
                {imgs.length > 1 && (<>
                  <button onClick={prev} className="absolute left-4 top-1/2 -translate-y-1/2 w-12 h-12 bg-white/90 hover:bg-white rounded-full flex items-center justify-center shadow-xl transition-all"><FiChevronLeft className="w-7 h-7 text-gray-800"/></button>
                  <button onClick={next} className="absolute right-4 top-1/2 -translate-y-1/2 w-12 h-12 bg-white/90 hover:bg-white rounded-full flex items-center justify-center shadow-xl transition-all"><FiChevronRight className="w-7 h-7 text-gray-800"/></button>
                </>)}
                <div className="absolute bottom-4 left-1/2 -translate-x-1/2 flex gap-2.5">
                  {imgs.map((_,i) => <button key={i} onClick={() => setImgIdx(i)} className={'h-2.5 rounded-full transition-all shadow-sm ' + (i === imgIdx ? 'w-8 bg-white' : 'w-2.5 bg-white/60')}/>)}
                </div>
              </div>
              <div className="flex gap-3 mt-3 px-1">
                {imgs.slice(0, 5).map((img, i) => (
                  <div key={i} onClick={() => setImgIdx(i)} className={'rounded-xl overflow-hidden cursor-pointer border-[3px] transition-all h-[100px] flex-1 ' + (imgIdx === i ? 'border-blue-500 shadow-md opacity-100' : 'border-transparent hover:border-gray-300 opacity-70 hover:opacity-100')}>
                    <img src={img} alt="" className="w-full h-full object-cover"/>
                  </div>
                ))}
              </div>
            </div>

            {/* Info */}
            <div className="bg-white rounded-3xl p-8 shadow-sm border border-gray-100">
              <h1 className="text-2xl font-extrabold text-gray-900 uppercase leading-normal mb-3">{post.title}</h1>
              <p className="text-[16px] text-blue-600 font-semibold flex items-center gap-2 mb-8">
                <FiMapPin className="w-5 h-5 flex-shrink-0"/>{addr}
              </p>
              <div className="flex justify-between gap-5 mb-8">
                {[{ icon: <FiMaximize2 className="w-6 h-6 text-blue-600"/>, label: 'Diện tích', val: post.area + ' m²' },
                  { icon: <FiUsers className="w-6 h-6 text-blue-600"/>, label: 'Số người', val: post.maxOccupants + ' người' },
                  { icon: <FiHome className="w-6 h-6 text-blue-600"/>, label: 'Trạng thái', val: ROOM_STATUS_LABELS[post.roomStatus] }].map(info => (
                  <div key={info.label} className="flex-1 bg-blue-50/70 rounded-2xl p-5 text-center border border-blue-100">
                    <div className="flex justify-center mb-2.5">{info.icon}</div>
                    <div className="text-sm text-gray-500 font-medium mb-1">{info.label}</div>
                    <div className="text-[17px] font-extrabold text-gray-900">{info.val}</div>
                  </div>
                ))}
              </div>
              <div className="pt-6 border-t border-gray-100 flex items-end">
                <span className="text-4xl font-extrabold text-blue-600 tracking-tight">{formatPrice(post.price)}</span>
                <span className="text-lg text-gray-500 font-semibold ml-2 mb-1">/tháng</span>
              </div>
            </div>

            {/* Desc */}
            <div className="bg-white rounded-3xl p-8 shadow-sm border border-gray-100">
              <h2 className="text-xl font-extrabold text-gray-900 mb-5">Mô tả chi tiết</h2>
              <p className="text-[16px] text-gray-700 whitespace-pre-line leading-loose font-medium">{post.description}</p>
            </div>

            {/* Amenities */}
            {post.amenities?.length > 0 && (
              <div className="bg-white rounded-3xl p-8 shadow-sm border border-gray-100">
                <h2 className="text-xl font-extrabold text-gray-900 mb-6">Tiện ích</h2>
                <div className="flex flex-wrap gap-4">
                  {post.amenities.map(a => (
                    <div key={a.id} className="w-[calc(33.333%-11px)] flex items-center gap-3 bg-gray-50 rounded-xl px-4 py-3.5 border border-gray-100">
                      <span className="text-2xl">{a.icon}</span>
                      <span className="text-[15px] text-gray-800 font-bold">{a.name}</span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* ── SIDEBAR (Phải) ── */}
          <div className="flex-1 flex flex-col gap-6">
            <div className="bg-white rounded-3xl shadow-sm border border-gray-100 p-7 sticky top-28">
              <h3 className="text-xl font-extrabold text-gray-900 mb-6">Liên hệ ngay</h3>
              <div className="flex items-center gap-4 mb-6 p-4 bg-gray-50/80 rounded-2xl border border-gray-100">
                <div className="w-14 h-14 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0 border-2 border-white shadow-sm">
                  <span className="text-blue-600 font-extrabold text-2xl">{post.landlordName?.charAt(0) || 'C'}</span>
                </div>
                <div>
                  <div className="text-[17px] font-extrabold text-gray-900 mb-1">{post.landlordName || 'Chủ nhà'}</div>
                  <div className="text-sm text-green-500 font-bold flex items-center gap-1.5"><span className="w-2 h-2 bg-green-500 rounded-full inline-block"/>Đang hoạt động</div>
                </div>
              </div>
              <button onClick={() => setShowPhone(true)} className="w-full flex items-center justify-center gap-2.5 bg-green-500 hover:bg-green-600 text-white py-4 rounded-xl font-extrabold text-[17px] transition-all mb-4 shadow-lg hover:shadow-xl hover:-translate-y-0.5">
                <FiPhone className="w-5 h-5"/>
                {showPhone ? (post.landlordPhone || '0948909999') : (post.landlordPhone || '0948909***').slice(0,7) + '*** Hiện số'}
              </button>
              <button onClick={() => toast.info('Tính năng đang phát triển')} className="w-full flex items-center justify-center gap-2.5 border-2 border-blue-600 text-blue-600 hover:bg-blue-50 py-3.5 rounded-xl font-extrabold text-[16px] transition-colors">
                <FiCalendar className="w-5 h-5"/> Đặt lịch xem phòng
              </button>
              <p className="mt-5 text-[14px] text-gray-500 text-center leading-relaxed font-medium">Vui lòng cho chủ nhà biết bạn tìm thấy phòng này trên Timnhatro.vn</p>
              <div className="mt-6 pt-5 border-t border-gray-100 flex justify-between text-[14px] text-gray-500 font-medium">
                <span>Mã tin: <strong className="text-gray-800">#{post.id}</strong></span>
                <span>{formatDate(post.createdAt)}</span>
              </div>
            </div>

            <div className="bg-white rounded-3xl shadow-sm border border-gray-100 p-7">
              <h3 className="text-xl font-extrabold text-gray-900 mb-5">Phòng tương tự</h3>
              <div className="flex flex-col gap-5">
                {SIMILAR.map(r => (
                  <div key={r.id} onClick={() => navigate('/rooms/' + r.id)} className="flex gap-4 cursor-pointer group">
                    <div className="w-[110px] h-[80px] rounded-xl overflow-hidden flex-shrink-0">
                      <img src={r.img} alt={r.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"/>
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-[14px] text-gray-800 font-bold line-clamp-2 group-hover:text-blue-600 transition-colors leading-snug mb-1">{r.title}</p>
                      <p className="text-[15px] font-extrabold text-blue-600 mb-0.5">{r.price}</p>
                      <p className="text-[13px] text-gray-500 font-medium">{r.area} m² • {r.date}</p>
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
export default RoomDetailPage;