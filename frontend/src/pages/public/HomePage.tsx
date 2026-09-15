import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { FiSearch, FiMapPin, FiClock, FiHeart, FiChevronRight } from 'react-icons/fi';

const FEATURED = [
  { id: 1, title: 'Cho thuê phòng Duplex – Gần Etown Cộng Hòa, Q.Tân Bình', price: '5.5 Triệu/tháng', area: '25', address: 'Phường 13, Quận Tân Bình, Tp HCM', date: '15/09/2026', img: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=500&h=320&fit=crop', badge: 'Nổi bật', badgeColor: 'bg-orange-500' },
  { id: 2, title: 'Cho thuê Chung Cư Gia Phúc, Linh Chiểu, Thủ Đức – 2PN 1WC, full nội thất', price: '8 Triệu/tháng', area: '67', address: 'Linh Chiểu, Thủ Đức, Tp HCM', date: '04/07/2026', img: 'https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=500&h=320&fit=crop', badge: 'HD', badgeColor: 'bg-blue-500' },
  { id: 3, title: 'Studio full nội thất – Vào ở ngay, Thủ Đức', price: '6.6 Triệu/tháng', area: '25', address: 'Linh Trung, Thủ Đức, Tp HCM', date: '30/06/2026', img: 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=500&h=320&fit=crop', badge: 'HD', badgeColor: 'bg-blue-500' },
  { id: 4, title: 'Cho thuê phòng 4S LINH ĐÔNG, Full Nội Thất 2PN, 2WC', price: '3.2 Triệu/tháng', area: '18', address: 'Quận 12, Tp Hồ Chí Minh', date: '12/09/2026', img: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=500&h=320&fit=crop', badge: '', badgeColor: '' },
];

const RECENT = [
  { id: 5, title: 'Cho thuê phòng trọ 20m² full nội thất, gần ĐH Kinh Tế, Q.3', price: '4.5 Triệu/tháng', area: '20', address: 'Quận 3, TP.HCM', date: '14/09/2026', img: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=300&h=200&fit=crop' },
  { id: 6, title: 'Nhà nguyên căn 3 phòng ngủ, hẻm xe hơi, Q.Bình Thạnh', price: '12 Triệu/tháng', area: '80', address: 'Bình Thạnh, TP.HCM', date: '13/09/2026', img: 'https://images.unsplash.com/photo-1484154218962-a197022b5858?w=300&h=200&fit=crop' },
  { id: 7, title: 'Phòng ở ghép nữ sinh, sạch sẽ, an ninh, Gò Vấp', price: '1.8 Triệu/tháng', area: '15', address: 'Gò Vấp, TP.HCM', date: '13/09/2026', img: 'https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=300&h=200&fit=crop' },
  { id: 8, title: 'Căn hộ 1PN cao cấp view đẹp, Vinhomes Grand Park, Q.9', price: '9 Triệu/tháng', area: '45', address: 'Quận 9, TP.HCM', date: '12/09/2026', img: 'https://images.unsplash.com/photo-1571508601891-ca5e7a713859?w=300&h=200&fit=crop' },
];

const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [filters, setFilters] = useState({ province: '', price: '', area: '', type: '' });

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    const p = new URLSearchParams();
    if (query) p.set('search', query);
    if (filters.province) p.set('province', filters.province);
    navigate('/rooms?' + p.toString());
  };

  return (
    <div className="bg-gray-50 min-h-screen min-w-[1200px]">

      {/* ── HERO ── */}
      <section className="relative overflow-hidden flex items-center justify-center" style={{ height: '520px' }}>
        <img src="https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?w=1920&h=800&fit=crop" alt="hero" className="absolute inset-0 w-full h-full object-cover"/>
        <div className="absolute inset-0 bg-gradient-to-b from-black/50 via-black/40 to-black/60"/>
        <div className="relative z-10 flex flex-col items-center text-center w-full max-w-[1200px] px-4">
          <h1 className="text-[56px] font-extrabold text-white mb-4 drop-shadow-lg tracking-tight leading-tight">
            Tìm phòng trọ tốt
          </h1>
          <p className="text-[22px] text-gray-100 mb-10 drop-shadow-md font-medium">
            Đăng tin miễn phí — phòng trọ, căn hộ mini trên toàn quốc
          </p>

          <form onSubmit={handleSearch} className="w-[1000px] bg-white rounded-2xl shadow-2xl overflow-hidden">
            <div className="flex items-center gap-4 px-6 py-4 border-b border-gray-100">
              <FiSearch className="w-6 h-6 text-gray-400"/>
              <input type="text" value={query} onChange={e => setQuery(e.target.value)} placeholder="Tìm theo khu vực, đường, quận hoặc từ khóa..." className="flex-1 text-[17px] text-gray-800 placeholder-gray-400 outline-none bg-transparent"/>
              <button type="submit" className="bg-blue-600 hover:bg-blue-700 text-white px-10 py-3.5 rounded-xl text-[17px] font-bold transition-colors shadow-md">
                Tìm kiếm
              </button>
            </div>
            <div className="flex divide-x divide-gray-100">
              {[
                { key: 'province', placeholder: 'Toàn quốc', opts: ['Hồ Chí Minh','Hà Nội','Đà Nẵng','Cần Thơ'] },
                { key: 'price', placeholder: 'Giá phòng', opts: ['Dưới 2 triệu','2 – 3 triệu','3 – 5 triệu','5 – 10 triệu','Trên 10 triệu'] },
                { key: 'area', placeholder: 'Diện tích', opts: ['Dưới 20 m²','20 – 30 m²','30 – 50 m²','Trên 50 m²'] },
                { key: 'type', placeholder: 'Loại phòng', opts: ['Phòng trọ','Nhà nguyên căn','Căn hộ','Ở ghép'] },
              ].map(f => (
                <select key={f.key} value={(filters as any)[f.key]} onChange={e => setFilters({ ...filters, [f.key]: e.target.value })}
                  className="flex-1 px-6 py-4 text-[16px] font-medium text-gray-700 outline-none bg-white appearance-none cursor-pointer hover:bg-blue-50 transition-colors">
                  <option value="">{f.placeholder}</option>
                  {f.opts.map(o => <option key={o} value={o}>{o}</option>)}
                </select>
              ))}
            </div>
          </form>
        </div>
      </section>

      {/* ── TIN NỔI BẬT ── */}
      <section className="max-w-[1200px] mx-auto px-4 py-16">
        <div className="flex items-center justify-between mb-8">
          <h2 className="text-3xl font-extrabold text-gray-900">Tin nổi bật</h2>
          <Link to="/rooms" className="flex items-center gap-1 text-blue-600 hover:text-blue-800 text-[17px] font-bold">
            Xem thêm <FiChevronRight className="w-5 h-5"/>
          </Link>
        </div>

        <div className="flex gap-6">
          {FEATURED.map(room => (
            <div key={room.id} onClick={() => navigate('/rooms/' + room.id)}
              className="w-1/4 bg-white rounded-2xl shadow-sm hover:shadow-xl transition-all duration-300 cursor-pointer group overflow-hidden border border-gray-100 hover:-translate-y-1.5">
              <div className="relative h-56 overflow-hidden">
                <img src={room.img} alt={room.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"/>
                {room.badge && (
                  <span className={'absolute top-3 left-3 text-xs font-extrabold px-3 py-1 rounded-md text-white shadow-sm ' + room.badgeColor}>{room.badge}</span>
                )}
                <button className="absolute top-3 right-3 w-9 h-9 bg-white/95 rounded-full flex items-center justify-center hover:bg-red-50 hover:text-red-500 transition-colors shadow-md" onClick={e => e.stopPropagation()}>
                  <FiHeart className="w-5 h-5 text-gray-400 hover:text-red-500"/>
                </button>
              </div>
              <div className="p-5">
                <p className="text-lg font-extrabold text-blue-600 mb-2">{room.price} <span className="text-sm text-gray-400 font-semibold ml-2">{room.area} m²</span></p>
                <h3 className="text-[16px] font-bold text-gray-800 mb-3 line-clamp-2 group-hover:text-blue-600 transition-colors leading-relaxed min-h-[48px]">{room.title}</h3>
                <p className="text-[14px] text-gray-500 flex items-center gap-1.5 mb-1"><FiMapPin className="w-4 h-4 flex-shrink-0 text-gray-400"/><span className="line-clamp-1">{room.address}</span></p>
                <p className="text-[13px] text-gray-400 font-medium">{room.date}</p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── TIN MỚI ĐĂNG ── */}
      <section className="max-w-[1200px] mx-auto px-4 pb-20">
        <div className="flex items-center justify-between mb-8">
          <h2 className="text-3xl font-extrabold text-gray-900">Tin mới đăng</h2>
          <Link to="/rooms" className="flex items-center gap-1 text-blue-600 hover:text-blue-800 text-[17px] font-bold">
            Xem thêm <FiChevronRight className="w-5 h-5"/>
          </Link>
        </div>

        <div className="flex gap-6 flex-wrap">
          {RECENT.map(room => (
            <div key={room.id} onClick={() => navigate('/rooms/' + room.id)}
              className="w-[calc(50%-12px)] bg-white rounded-2xl shadow-sm hover:shadow-lg transition-all duration-300 cursor-pointer group flex overflow-hidden border border-gray-100 hover:-translate-y-1">
              <div className="relative w-48 h-36 flex-shrink-0 overflow-hidden">
                <img src={room.img} alt={room.title} className="w-full h-full object-cover group-hover:scale-110 transition-transform duration-500"/>
              </div>
              <div className="p-5 flex flex-col justify-between flex-1 min-w-0">
                <div>
                  <h3 className="text-[17px] font-bold text-gray-800 line-clamp-2 group-hover:text-blue-600 transition-colors leading-snug mb-2">{room.title}</h3>
                  <p className="text-lg font-extrabold text-blue-600">{room.price}</p>
                </div>
                <div className="flex items-center gap-6 text-[14px] font-medium text-gray-500 mt-2">
                  <span className="flex items-center gap-1.5"><FiMapPin className="w-4 h-4"/>{room.area} m² | {room.address}</span>
                  <span className="flex items-center gap-1.5"><FiClock className="w-4 h-4"/>{room.date}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── STATS ── */}
      <section className="bg-blue-600 py-16">
        <div className="max-w-[1200px] mx-auto px-4">
          <div className="flex justify-between items-center text-center">
            {[{ val: '10,000+', label: 'Phòng trọ' }, { val: '5,000+', label: 'Chủ nhà' }, { val: '15,000+', label: 'Người dùng' }, { val: '63', label: 'Tỉnh/Thành phố' }].map(s => (
              <div key={s.label} className="w-1/4">
                <div className="text-5xl font-extrabold text-white mb-2">{s.val}</div>
                <div className="text-blue-200 text-lg font-semibold uppercase tracking-wider">{s.label}</div>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* ── WHY US ── */}
      <section className="max-w-[1200px] mx-auto px-4 py-20">
        <h2 className="text-3xl font-extrabold text-center text-gray-900 mb-12">Tại sao chọn TroMoi.vn?</h2>
        <div className="flex gap-10">
          {[
            { icon: '🔍', title: 'Tìm kiếm dễ dàng', desc: 'Hệ thống lọc thông minh giúp bạn tìm phòng phù hợp nhanh chóng' },
            { icon: '✅', title: 'Tin cậy & An toàn', desc: 'Mọi tin đăng đều được kiểm duyệt cẩn thận trước khi công khai' },
            { icon: '⚡', title: 'Nhanh chóng', desc: 'Đặt lịch xem phòng trực tuyến, liên hệ chủ nhà ngay lập tức' },
          ].map(f => (
            <div key={f.title} className="w-1/3 text-center bg-white rounded-3xl p-10 shadow-sm border border-gray-100 hover:shadow-xl transition-shadow">
              <div className="text-6xl mb-6">{f.icon}</div>
              <h3 className="text-xl font-extrabold text-gray-900 mb-4">{f.title}</h3>
              <p className="text-[15px] text-gray-600 leading-relaxed font-medium">{f.desc}</p>
            </div>
          ))}
        </div>
      </section>

      {/* ── CTA ── */}
      <section className="bg-gradient-to-r from-blue-600 to-blue-800 py-20">
        <div className="max-w-[1200px] mx-auto px-4 text-center">
          <h2 className="text-4xl font-extrabold text-white mb-4">Bạn có phòng trọ cần cho thuê?</h2>
          <p className="text-blue-100 text-xl font-medium mb-10">Đăng tin miễn phí, tiếp cận hàng ngàn khách hàng tiềm năng</p>
          <button onClick={() => navigate('/register')} className="bg-white text-blue-600 px-12 py-4 rounded-xl font-extrabold text-lg hover:bg-gray-50 transition-colors shadow-xl inline-flex items-center gap-3 hover:scale-105">
            Đăng tin ngay <FiChevronRight className="w-6 h-6"/>
          </button>
        </div>
      </section>
    </div>
  );
};
export default HomePage;