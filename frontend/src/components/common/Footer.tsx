
import React from 'react';
import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import {
  FiPhone,
  FiMail,
  FiMapPin,
  FiFacebook,
  FiArrowUp,
  FiChevronRight,
  FiClock,
  FiShield,
} from 'react-icons/fi';

const Footer: React.FC = () => {
  const toTop = () => {
    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    });
  };

  return (
    <footer className="site-footer">

      {/* =========================
          FOOTER MAIN
      ========================== */}
      <div className="site-footer-container">

        <div className="footer-grid">

          {/* =========================
              CỘT 1 - THƯƠNG HIỆU
          ========================== */}
          <div className="footer-brand">

            <Link
              to={ROUTES.HOME}
              className="footer-logo"
            >
              TRỌ MỚI
            </Link>

            <p className="footer-brand-description">
              Nền tảng tìm kiếm và cho thuê phòng trọ trực tuyến,
              giúp người thuê dễ dàng tìm được nơi ở phù hợp và
              kết nối nhanh chóng với chủ trọ.
            </p>

            {/* Download App */}
            <div className="footer-download">

              <h4 className="footer-subtitle">
                Trải nghiệm trên điện thoại
              </h4>

              <div className="footer-apps">

                {/* QR */}
                <div className="footer-qr">
                  QR
                </div>

                <div className="footer-app-buttons">

                  <a
                    href="#"
                    className="footer-app-button"
                  >
                    <svg
                      className="footer-app-icon"
                      fill="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z" />
                    </svg>

                    <span>
                      <small>Tải về trên</small>
                      <strong>App Store</strong>
                    </span>
                  </a>

                  <a
                    href="#"
                    className="footer-app-button"
                  >
                    <svg
                      className="footer-app-icon"
                      fill="currentColor"
                      viewBox="0 0 24 24"
                    >
                      <path d="M3.18 23.76c.33.18.7.24 1.06.18l12.5-12.5-2.83-2.83-10.73 15.15zm17.37-11.86L17.44 10l-3.06 3.06 3.06 3.06 3.15-1.86c.9-.53.9-1.86-.04-2.36zM1.77.36C1.49.65 1.33 1.1 1.33 1.68v20.64c0 .58.16 1.03.44 1.32l.07.06 11.56-11.56v-.27L1.84.3l-.07.06zm9.69 10.1L1.9 1.03l-.07.06 9.56 9.56.07-.19z" />
                    </svg>

                    <span>
                      <small>Tải trên</small>
                      <strong>Google Play</strong>
                    </span>
                  </a>

                </div>
              </div>
            </div>

          </div>


          {/* =========================
              CỘT 2 - KHÁM PHÁ
          ========================== */}
          <div className="footer-column">

            <h3 className="footer-title">
              Khám phá
            </h3>

            <Link
              to={ROUTES.HOME}
              className="footer-link"
            >
              <FiChevronRight />
              Trang chủ
            </Link>

            <Link
              to={ROUTES.ROOM_LIST || '/rooms'}
              className="footer-link"
            >
              <FiChevronRight />
              Tìm phòng trọ
            </Link>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Khu vực nổi bật
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Tin đăng mới
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Phòng trọ nổi bật
            </a>

          </div>


          {/* =========================
              CỘT 3 - HỖ TRỢ
          ========================== */}
          <div className="footer-column">

            <h3 className="footer-title">
              Hỗ trợ
            </h3>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Hướng dẫn tìm phòng
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Hướng dẫn đăng tin
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Câu hỏi thường gặp
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Điều khoản sử dụng
            </a>

            <a href="#" className="footer-link">
              <FiChevronRight />
              Chính sách bảo mật
            </a>

          </div>


          {/* =========================
              CỘT 4 - LIÊN HỆ
          ========================== */}
          <div className="footer-column footer-contact">

            <h3 className="footer-title">
              Liên hệ với chúng tôi
            </h3>

            <div className="footer-contact-list">

              <div className="footer-contact-item">
                <div className="footer-contact-icon">
                  <FiPhone />
                </div>

                <div>
                  <span>Hotline</span>
                  <strong>033 266 1579</strong>
                </div>
              </div>


              <div className="footer-contact-item">
                <div className="footer-contact-icon">
                  <FiMail />
                </div>

                <div>
                  <span>Email</span>
                  <strong>info@tromoi.com</strong>
                </div>
              </div>


              <div className="footer-contact-item">
                <div className="footer-contact-icon">
                  <FiMapPin />
                </div>

                <div>
                  <span>Địa chỉ</span>
                  <strong>
                    Hưng Yên, Việt Nam
                  </strong>
                </div>
              </div>


              <div className="footer-contact-item">
                <div className="footer-contact-icon">
                  <FiClock />
                </div>

                <div>
                  <span>Thời gian hỗ trợ</span>
                  <strong>
                    08:00 - 22:00
                  </strong>
                </div>
              </div>

            </div>


            {/* Social */}
            <div className="footer-social">

              <span>
                Kết nối với chúng tôi
              </span>

              <div className="footer-social-list">

                <a
                  href="#"
                  aria-label="Facebook"
                  className="footer-social-button"
                >
                  <FiFacebook />
                </a>

                <a
                  href="#"
                  aria-label="Zalo"
                  className="footer-social-button footer-zalo"
                >
                  Z
                </a>

              </div>

            </div>

          </div>

        </div>


        {/* =========================
            FOOTER TRUST
        ========================== */}
        <div className="footer-trust">

          <div className="footer-trust-item">
            <div className="footer-trust-icon">
              <FiShield />
            </div>

            <div>
              <strong>Thông tin minh bạch</strong>
              <span>
                Hỗ trợ người dùng tìm kiếm phòng phù hợp
              </span>
            </div>
          </div>


          <div className="footer-trust-item">

            <div className="footer-trust-icon">
              <FiPhone />
            </div>

            <div>
              <strong>Hỗ trợ nhanh chóng</strong>
              <span>
                Đội ngũ hỗ trợ luôn sẵn sàng giải đáp
              </span>
            </div>

          </div>


          <div className="footer-trust-item">

            <div className="footer-trust-icon">
              <FiMapPin />
            </div>

            <div>
              <strong>Tìm phòng dễ dàng</strong>
              <span>
                Tìm kiếm phòng theo khu vực và nhu cầu
              </span>
            </div>

          </div>

        </div>

      </div>


      {/* =========================
          FOOTER BOTTOM
      ========================== */}
      <div className="footer-bottom">

        <div className="footer-bottom-inner">

          <p>
            © {new Date().getFullYear()} Trọ Mới. All rights reserved.
          </p>

          <div className="footer-bottom-links">
            <a href="#">
              Điều khoản
            </a>

            <a href="#">
              Bảo mật
            </a>

            <a href="#">
              Liên hệ
            </a>
          </div>

        </div>

      </div>


      {/* =========================
          BACK TO TOP
      ========================== */}
      <button
        onClick={toTop}
        title="Lên đầu trang"
        aria-label="Lên đầu trang"
        className="footer-back-top"
      >
        <FiArrowUp />
      </button>

    </footer>
  );
};

export default Footer;

