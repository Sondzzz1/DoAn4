import React from 'react';
import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import { FiPhone, FiMail, FiMapPin, FiFacebook, FiArrowUp } from 'react-icons/fi';

const Footer: React.FC = () => {
  const toTop = () => window.scrollTo({ top: 0, behavior: 'smooth' });

  return (
    <footer className="site-footer">
      <div className="site-footer-container">
        <div className="footer-grid">
          
          {/* Cột 1: Thương hiệu & Tải App */}
          <div>
            <Link to={ROUTES.HOME} className="site-logo block mb-4">
              TRỌ MỚI
            </Link>
            <p className="text-[13px] font-bold text-[#0084ff] tracking-wider mb-3 uppercase">
              Tải App Trọ Mới Ngay
            </p>
            <div className="flex items-start gap-3 mb-6">
              <div className="w-[72px] h-[72px] bg-gray-100 rounded-lg flex items-center justify-center text-xs text-gray-400 font-bold border border-gray-200 flex-shrink-0">
                QR CODE
              </div>
              <div className="flex flex-col gap-2">
                <a href="#" className="flex items-center gap-2 bg-black text-white px-3 py-1.5 rounded-lg hover:bg-gray-800 transition-colors">
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/></svg>
                  <div>
                    <div className="text-gray-400 text-[9px] leading-none">Tải về trên</div>
                    <div className="font-bold text-[13px] leading-tight">App Store</div>
                  </div>
                </a>
                <a href="#" className="flex items-center gap-2 bg-black text-white px-3 py-1.5 rounded-lg hover:bg-gray-800 transition-colors">
                  <svg className="w-5 h-5" fill="currentColor" viewBox="0 0 24 24"><path d="M3.18 23.76c.33.18.7.24 1.06.18l12.5-12.5-2.83-2.83-10.73 15.15zm17.37-11.86L17.44 10l-3.06 3.06 3.06 3.06 3.15-1.86c.9-.53.9-1.86-.04-2.36zM1.77.36C1.49.65 1.33 1.1 1.33 1.68v20.64c0 .58.16 1.03.44 1.32l.07.06 11.56-11.56v-.27L1.84.3l-.07.06zm9.69 10.1L1.9 1.03l-.07.06 9.56 9.56.07-.19z"/></svg>
                  <div>
                    <div className="text-gray-400 text-[9px] leading-none">Tải trên</div>
                    <div className="font-bold text-[13px] leading-tight">Google Play</div>
                  </div>
                </a>
              </div>
            </div>
            <p className="text-[13px] text-gray-500 mb-2">
              Thành viên của <a href="#" className="text-[#0084ff] font-bold hover:underline">ohi.vn</a>
            </p>
            <div className="flex flex-wrap gap-1.5">
              {['ohdidi', 'ohbeauty', 'ohdental', 'nhadepdat', 'phongkhamnow'].map(b => (
                <span key={b} className="border border-gray-200 text-gray-400 text-[11px] px-2 py-0.5 rounded font-medium">
                  {b}
                </span>
              ))}
            </div>
          </div>

          {/* Cột 2: Hệ thống & Thông tin */}
          <div className="grid grid-cols-2 gap-6">
            <div>
              <h4 className="footer-title">Hệ thống</h4>
              <a href="#" className="footer-link">Blog</a>
              <a href="#" className="footer-link">Hướng dẫn</a>
              <a href="#" className="footer-link">Liên hệ</a>

              <h4 className="footer-title mt-6">Dành cho chủ trọ</h4>
              <a href="#" className="footer-link">Giới thiệu Trọ Mới Pro</a>
              <a href="#" className="footer-link">Gói xác thực Verify</a>
              <a href="#" className="footer-link">Truy cập Quản lý</a>
            </div>

            <div>
              <h4 className="footer-title">Thông tin</h4>
              <a href="#" className="footer-link">Điều khoản & Cam kết</a>
              <a href="#" className="footer-link">Quy chế hoạt động</a>
              <a href="#" className="footer-link">Chính sách bảo mật</a>
            </div>
          </div>

          {/* Cột 3: Liên hệ */}
          <div>
            <h4 className="footer-title">Kết nối với chúng tôi</h4>
            <div className="space-y-2.5 text-[14px]">
              <div className="flex items-center gap-2.5 text-gray-700 font-medium">
                <FiPhone className="w-4 h-4 text-[#0084ff] flex-shrink-0"/>
                <span>033.266.1579</span>
              </div>
              <div className="flex items-center gap-2.5 text-gray-700 font-medium">
                <span className="bg-[#0084ff] text-white text-[10px] font-bold px-1.5 py-0.5 rounded">Zalo</span>
                <span>0332661579</span>
              </div>
              <div className="flex items-center gap-2.5 text-gray-700 font-medium">
                <FiMail className="w-4 h-4 text-[#0084ff] flex-shrink-0"/>
                <span>info@tromoi.com</span>
              </div>
              <div className="flex items-start gap-2.5 text-gray-600 text-[13px]">
                <FiMapPin className="w-4 h-4 text-[#0084ff] flex-shrink-0 mt-0.5"/>
                <span>VP Huế: 4/16 Đoàn Hữu Trung, TP. Huế</span>
              </div>
              <div className="flex items-start gap-2.5 text-gray-600 text-[13px]">
                <FiMapPin className="w-4 h-4 text-[#0084ff] flex-shrink-0 mt-0.5"/>
                <span>VP HCM: Tòa nhà E-Tunnel, 107 Trần Nguyên Đán, P. Gia Định, TP.HCM</span>
              </div>
            </div>

            <div className="mt-4 flex flex-wrap gap-2">
              {['tromoitoanquoc', 'tromoihue', 'host.tromoi'].map(label => (
                <a key={label} href="#" className="flex items-center gap-1 text-[12px] text-[#0084ff] hover:underline font-semibold">
                  <FiFacebook className="w-3.5 h-3.5"/>{label}
                </a>
              ))}
            </div>
          </div>

        </div>
      </div>

      <div className="footer-bottom">
        &copy; {new Date().getFullYear()} Timnhatro.vn. All rights reserved.
      </div>

      <button 
        onClick={toTop} 
        title="Lên đầu trang"
        className="fixed bottom-6 right-6 w-11 h-11 bg-white hover:bg-gray-50 text-[#0084ff] border border-gray-200 rounded-full flex items-center justify-center shadow-lg transition-all hover:scale-105 z-50 cursor-pointer"
      >
        <FiArrowUp className="w-5 h-5"/>
      </button>
    </footer>
  );
};

export default Footer;
