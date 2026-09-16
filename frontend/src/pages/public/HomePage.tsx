import React, { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { FiSearch, FiMapPin, FiHeart, FiChevronRight, FiChevronDown } from 'react-icons/fi';

const FEATURED = [
  { 
    id: 1, 
    title: 'Cho thuê phòng Duplex – Gần Etown Cộng Hòa – Gần ĐH Văn Hiến & ĐH Công Thương', 
    price: '5.5 Triệu/tháng', 
    area: '25', 
    address: 'Phường 13, Quận Tân Bình, Tp Hồ Chí Minh', 
    date: '15/09/2026', 
    img: 'https://images.unsplash.com/photo-1560448204-e02f11c3d0e2?w=600&h=400&fit=crop', 
    badge: 'Nổi bật', 
    badgeColor: 'bg-orange-500' 
  },
  { 
    id: 2, 
    title: 'Cho thuê Chung Cư Gia Phúc, Linh Chiểu, Thủ Đức – 2PN 1WC, full nội thất', 
    price: '8 Triệu/tháng', 
    area: '67', 
    address: 'Linh Chiểu, Thủ Đức, Tp Hồ Chí Minh', 
    date: '04/07/2026', 
    img: 'https://images.unsplash.com/photo-1555041469-a586c61ea9bc?w=600&h=400&fit=crop', 
    badge: 'HD', 
    badgeColor: 'bg-[#0084ff]' 
  },
  { 
    id: 3, 
    title: 'Studio full nội thất – Vào ở ngay, vị trí trung tâm tiện đi lại các quận', 
    price: '6.6 Triệu/tháng', 
    area: '25', 
    address: 'Linh Trung, Thủ Đức, Tp Hồ Chí Minh', 
    date: '30/06/2026', 
    img: 'https://images.unsplash.com/photo-1586023492125-27b2c045efd7?w=600&h=400&fit=crop', 
    badge: 'HD', 
    badgeColor: 'bg-[#0084ff]' 
  },
  { 
    id: 4, 
    title: 'Cho thuê phòng 4S LINH ĐÔNG, Full Nội Thất 2PN, 2WC view thoáng mát', 
    price: '3.2 Triệu/tháng', 
    area: '18', 
    address: 'Quận 12, Tp Hồ Chí Minh', 
    date: '12/09/2026', 
    img: 'https://images.unsplash.com/photo-1502672260266-1c1ef2d93688?w=600&h=400&fit=crop', 
    badge: '', 
    badgeColor: '' 
  },
];

const RECENT = [
  { id: 5, title: 'Cho thuê phòng trọ 20m² full nội thất, gần ĐH Kinh Tế, Q.3', price: '4.5 Triệu/tháng', area: '20', address: 'Quận 3, TP.HCM', date: 'Hôm nay', img: 'https://images.unsplash.com/photo-1522708323590-d24dbb6b0267?w=300&h=200&fit=crop' },
  { id: 6, title: 'Nhà nguyên căn 3 phòng ngủ, hẻm xe hơi, Q.Bình Thạnh', price: '12 Triệu/tháng', area: '80', address: 'Bình Thạnh, TP.HCM', date: 'Hôm qua', img: 'https://images.unsplash.com/photo-1484154218962-a197022b5858?w=300&h=200&fit=crop' },
  { id: 7, title: 'Phòng ở ghép nữ sinh, sạch sẽ, an ninh, Gò Vấp', price: '1.8 Triệu/tháng', area: '15', address: 'Gò Vấp, TP.HCM', date: '13/09', img: 'https://images.unsplash.com/photo-1493809842364-78817add7ffb?w=300&h=200&fit=crop' },
  { id: 8, title: 'Căn hộ 1PN cao cấp view đẹp, Vinhomes Grand Park, Q.9', price: '9 Triệu/tháng', area: '45', address: 'Quận 9, TP.HCM', date: '12/09', img: 'https://images.unsplash.com/photo-1571508601891-ca5e7a713859?w=300&h=200&fit=crop' },
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
    <div className="bg-[#f7f8fa] min-h-screen">
      
      {/* ── HERO ── */}
      <section className="home-hero">
        <img
          src="https://images.unsplash.com/photo-1618219908412-a29a1bb7b86e?w=1920&h=700&fit=crop"
          alt="Tìm phòng trọ"
          className="home-hero-image"
        />
        <div className="home-hero-overlay" />

        <div className="home-hero-content">
          <h1 className="home-hero-title">
            Tìm phòng trọ tốt
          </h1>
          <p className="home-hero-subtitle">
            Đăng tin miễn phí — phòng trọ, căn hộ mini trên toàn quốc
          </p>

          <form onSubmit={handleSearch} className="hero-search">
            <div className="hero-search-main">
              <FiSearch className="w-5 h-5 text-gray-400 mr-2 flex-shrink-0" />
              <input
                type="text"
                value={query}
                onChange={e => setQuery(e.target.value)}
                placeholder="Tìm theo khu vực, đường, quận hoặc từ khóa..."
                className="hero-search-input"
              />
              <button type="submit" className="hero-search-button">
                Tìm kiếm
              </button>
            </div>

            <div className="hero-filters">
              {[
                { key: 'province', ph: 'Toàn quốc', opts: ['Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Cần Thơ'] },
                { key: 'price', ph: 'Giá phòng', opts: ['Dưới 2 triệu', '2 – 3 triệu', '3 – 5 triệu', '5 – 10 triệu', 'Trên 10 triệu'] },
                { key: 'area', ph: 'Diện tích', opts: ['Dưới 20 m²', '20 – 30 m²', '30 – 50 m²', 'Trên 50 m²'] },
                { key: 'type', ph: 'Loại phòng', opts: ['Phòng trọ', 'Nhà nguyên căn', 'Căn hộ', 'Ở ghép'] },
              ].map(f => (
                <div key={f.key} className="hero-filter">
                  <select
                    value={(filters as any)[f.key]}
                    onChange={e => setFilters({ ...filters, [f.key]: e.target.value })}
                  >
                    <option value="">{f.ph}</option>
                    {f.opts.map(o => <option key={o} value={o}>{o}</option>)}
                  </select>
                  <FiChevronDown className="hero-filter-icon" />
                </div>
              ))}
            </div>
          </form>
        </div>
      </section>

      {/* ── TIN NỔI BẬT ── */}
      <section className="home-section">
        <div className="section-heading">
          <h2 className="section-title">Tin nổi bật</h2>
          <Link to="/rooms" className="section-more">
            Xem thêm
            <FiChevronRight />
          </Link>
        </div>

        <div className="room-grid">
          {FEATURED.map(room => (
            <div
              key={room.id}
              onClick={() => navigate('/rooms/' + room.id)}
              className="room-card"
            >
              <div className="room-card-image">
                <img src={room.img} alt={room.title} />
                {room.badge && (
                  <span className={'room-card-badge ' + room.badgeColor}>
                    {room.badge}
                  </span>
                )}
                <button
                  className="room-card-heart"
                  onClick={e => e.stopPropagation()}
                >
                  <FiHeart size={16} />
                </button>
              </div>

              <div className="room-card-content">
                <div className="room-card-meta">
                  <span className="room-card-price">{room.price}</span>
                  <span className="room-card-area">{room.area} m²</span>
                </div>

                <h3 className="room-card-title">
                  {room.title}
                </h3>

                <p className="room-card-address">
                  <FiMapPin size={13} className="flex-shrink-0" />
                  <span>{room.address}</span>
                </p>

                <p className="room-card-date">
                  {room.date}
                </p>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* ── TIN MỚI ĐĂNG ── */}
      <section className="home-section">
        <div className="section-heading">
          <h2 className="section-title">Tin mới đăng</h2>
          <Link to="/rooms" className="section-more">
            Xem thêm
            <FiChevronRight />
          </Link>
        </div>

        <div className="recent-grid">
          {RECENT.map(room => (
            <div
              key={room.id}
              onClick={() => navigate('/rooms/' + room.id)}
              className="recent-card"
            >
              <div className="recent-image">
                <img src={room.img} alt={room.title} />
              </div>

              <div className="recent-content">
                <div>
                  <h3 className="recent-title">{room.title}</h3>
                  <div className="recent-price">{room.price}</div>
                </div>

                <div className="recent-footer">
                  <span className="flex items-center gap-1">
                    <FiMapPin size={13} />
                    {room.area} m² - {room.address}
                  </span>
                  <span>{room.date}</span>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

    </div>
  );
};

export default HomePage;
