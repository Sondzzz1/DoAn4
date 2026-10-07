import React from 'react';
import { Link } from 'react-router-dom';
import { FiArrowUp, FiArrowUpRight, FiMail, FiMapPin, FiPhone } from 'react-icons/fi';

const Footer: React.FC = () => (
  <footer className="site-footer">
    <div className="site-footer-container">
      <div className="footer-grid">
        <div className="footer-brand">
          <Link to="/" className="site-logo">Timnhatro<span className="site-logo-green">.vn</span></Link>
          <p className="footer-brand-description">Tìm một nơi ở phù hợp. Kết nối trực tiếp với chủ trọ, từ buổi xem phòng đến ngày chuyển vào.</p>
        </div>
        <div className="footer-column">
          <h3 className="footer-title">Khám phá</h3>
          <Link to="/rooms" className="footer-link">Phòng cho thuê <FiArrowUpRight /></Link>
          <Link to="/rooms?sortBy=views" className="footer-link">Phòng nổi bật <FiArrowUpRight /></Link>
          <Link to="/rooms?sortBy=new" className="footer-link">Tin mới đăng <FiArrowUpRight /></Link>
          <Link to="/blog" className="footer-link">Góc người thuê <FiArrowUpRight /></Link>
        </div>
        <div className="footer-column">
          <h3 className="footer-title">Khu vực</h3>
          {['Hồ Chí Minh', 'Hà Nội', 'Đà Nẵng', 'Cần Thơ'].map(province => <Link key={province} to={'/rooms?province=' + encodeURIComponent(province)} className="footer-link">{province}<FiArrowUpRight /></Link>)}
        </div>
        <div className="footer-column footer-contact">
          <h3 className="footer-title">Liên hệ</h3>
          <a href="tel:0332661579" className="footer-link"><FiPhone />033 266 1579</a>
          <a href="mailto:info@tromoi.com" className="footer-link"><FiMail />info@tromoi.com</a>
          <p className="footer-link"><FiMapPin />Hưng Yên, Việt Nam</p>
          <p className="footer-support-hours">08:00 - 22:00</p>
        </div>
      </div>
    </div>
    <div className="footer-bottom"><div className="footer-bottom-inner"><p>© {new Date().getFullYear()} Timnhatro.vn</p><span>Hệ thống tìm kiếm và cho thuê phòng trọ trực tuyến</span></div></div>
    <button type="button" onClick={() => window.scrollTo({ top: 0, behavior: 'smooth' })} title="Lên đầu trang" aria-label="Lên đầu trang" className="footer-back-top"><FiArrowUp /></button>
  </footer>
);

export default Footer;
