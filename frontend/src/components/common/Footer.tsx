import React from 'react';
import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import { FiPhone, FiMail, FiMapPin, FiFacebook, FiArrowUp } from 'react-icons/fi';

const Footer: React.FC = () => {
  const toTop = () => window.scrollTo({ top: 0, behavior: 'smooth' });

  return (
    <footer className="bg-white border-t border-gray-200 min-w-[1200px]">
      <div className="max-w-[1200px] mx-auto px-4 py-14">
        <div className="flex justify-between gap-12">
          {/* Cột 1 */}
          <div className="w-1/3">
            <Link to={ROUTES.HOME} className="flex items-center gap-2 mb-6">
              <svg className="w-10 h-10 text-blue-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9.75L12 3l9 6.75V21a.75.75 0 01-.75.75H3.75A.75.75 0 013 21V9.75z"/>
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 21V12h6v9"/>
              </svg>
              <span className="text-2xl font-extrabold text-blue-600 tracking-tight">TRỌ MỚI</span>
            </Link>
            <p className="text-sm font-bold text-blue-600 tracking-widest mb-4 uppercase">Tải App Trọ Mới Ngay</p>
            <div className="flex items-start gap-4 mb-6">
              <div className="w-20 h-20 bg-gray-100 rounded-xl flex items-center justify-center text-sm text-gray-400 font-bold border border-gray-200">QR</div>
              <div className="flex flex-col gap-2.5">
                <a href="#" className="flex items-center gap-2.5 bg-black text-white px-4 py-2 rounded-xl hover:bg-gray-800 transition-colors">
                  <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24"><path d="M18.71 19.5c-.83 1.24-1.71 2.45-3.05 2.47-1.34.03-1.77-.79-3.29-.79-1.53 0-2 .77-3.27.82-1.31.05-2.3-1.32-3.14-2.53C4.25 17 2.94 12.45 4.7 9.39c.87-1.52 2.43-2.48 4.12-2.51 1.28-.02 2.5.87 3.29.87.78 0 2.26-1.07 3.8-.91.65.03 2.47.26 3.64 1.98-.09.06-2.17 1.28-2.15 3.81.03 3.02 2.65 4.03 2.68 4.04-.03.07-.42 1.44-1.38 2.83M13 3.5c.73-.83 1.94-1.46 2.94-1.5.13 1.17-.34 2.35-1.04 3.19-.69.85-1.83 1.51-2.95 1.42-.15-1.15.41-2.35 1.05-3.11z"/></svg>
                  <div><div className="text-gray-400 text-[10px]">Tải về trên</div><div className="font-bold text-[15px] leading-none">App Store</div></div>
                </a>
                <a href="#" className="flex items-center gap-2.5 bg-black text-white px-4 py-2 rounded-xl hover:bg-gray-800 transition-colors">
                  <svg className="w-6 h-6" fill="currentColor" viewBox="0 0 24 24"><path d="M3.18 23.76c.33.18.7.24 1.06.18l12.5-12.5-2.83-2.83-10.73 15.15zm17.37-11.86L17.44 10l-3.06 3.06 3.06 3.06 3.15-1.86c.9-.53.9-1.86-.04-2.36zM1.77.36C1.49.65 1.33 1.1 1.33 1.68v20.64c0 .58.16 1.03.44 1.32l.07.06 11.56-11.56v-.27L1.84.3l-.07.06zm9.69 10.1L1.9 1.03l-.07.06 9.56 9.56.07-.19z"/></svg>
                  <div><div className="text-gray-400 text-[10px]">Tải trên</div><div className="font-bold text-[15px] leading-none">Google Play</div></div>
                </a>
              </div>
            </div>
            <p className="text-[15px] text-gray-600 mb-3">Thành viên của <a href="#" className="text-blue-600 hover:underline font-bold">ohi.vn</a></p>
            <div className="flex flex-wrap gap-2">
              {['ohdidi','ohbeauty','ohdental','nhadepdat','phongkhamnow'].map(b => (
                <span key={b} className="border border-gray-300 text-gray-500 text-xs px-2.5 py-1 rounded-md font-semibold">{b}</span>
              ))}
            </div>
          </div>

          {/* Cột 2 */}
          <div className="w-1/3 flex flex-col gap-8">
            <div>
              <h4 className="text-sm font-bold text-gray-900 tracking-widest uppercase mb-4">Hệ thống</h4>
              <ul className="space-y-3">
                {['Blog','Hướng dẫn','Liên hệ'].map(t => (
                  <li key={t}><a href="#" className="text-[15px] text-gray-700 hover:text-blue-600 transition-colors font-medium">{t}</a></li>
                ))}
              </ul>
            </div>
            <div>
              <h4 className="text-sm font-bold text-gray-900 tracking-widest uppercase mb-4">Dành cho chủ trọ</h4>
              <ul className="space-y-3">
                {['Giới thiệu Trọ Mới Pro','Gói xác thực Trọ Mới Verify','Truy cập Quản lý Trọ Mới'].map(t => (
                  <li key={t}><a href="#" className="text-[15px] text-gray-700 hover:text-blue-600 transition-colors font-medium">{t}</a></li>
                ))}
              </ul>
            </div>
            <div>
              <h4 className="text-sm font-bold text-gray-900 tracking-widest uppercase mb-4">Thông tin</h4>
              <ul className="space-y-3">
                {['Điều khoản & Cam kết','Quy chế hoạt động','Chính sách bảo mật'].map(t => (
                  <li key={t}><a href="#" className="text-[15px] text-gray-700 hover:text-blue-600 transition-colors font-medium">{t}</a></li>
                ))}
              </ul>
            </div>
          </div>

          {/* Cột 3 */}
          <div className="w-1/3">
            <h4 className="text-sm font-bold text-gray-900 tracking-widest uppercase mb-5">Kết nối với chúng tôi</h4>
            <ul className="space-y-4">
              <li className="flex items-start gap-3 text-[15px] text-gray-800 font-medium">
                <FiPhone className="w-5 h-5 mt-0.5 text-blue-600 flex-shrink-0"/>
                <span>033.266.1579</span>
              </li>
              <li className="flex items-start gap-3 text-[15px] text-gray-800 font-medium">
                <span className="bg-blue-600 text-white text-[11px] font-bold px-1.5 py-0.5 rounded mt-0.5 flex-shrink-0">Zalo</span>
                <span>0332661579</span>
              </li>
              <li className="flex items-start gap-3 text-[15px] text-gray-800 font-medium">
                <FiMail className="w-5 h-5 mt-0.5 text-blue-600 flex-shrink-0"/>
                <span>info@tromoi.com</span>
              </li>
              <li className="flex items-start gap-3 text-[15px] text-gray-800 font-medium leading-relaxed">
                <FiMapPin className="w-5 h-5 mt-1 text-blue-600 flex-shrink-0"/>
                <span>VP Huế: 4/16 Đoàn Hữu Trung, TP. Huế</span>
              </li>
              <li className="flex items-start gap-3 text-[15px] text-gray-800 font-medium leading-relaxed">
                <FiMapPin className="w-5 h-5 mt-1 text-blue-600 flex-shrink-0"/>
                <span>VP HCM: Tòa nhà E-Tunnel, 107 Trần Nguyên Đán, P. Gia Định, TP.HCM</span>
              </li>
            </ul>
            <div className="mt-6 flex flex-wrap gap-x-5 gap-y-3">
              {[
                ['tromoitoanquoc','https://facebook.com'],
                ['tromoihue','https://facebook.com'],
                ['host.tromoi','https://facebook.com'],
                ['@tromoi.com','#'],
                ['@tromoi.hcm','#'],
                ['@@tromoi','#'],
              ].map(([label, href]) => (
                <a key={label} href={href} className="flex items-center gap-1.5 text-sm text-blue-600 font-bold hover:underline">
                  <FiFacebook className="w-4 h-4"/>{label}
                </a>
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="border-t border-gray-200 py-6 bg-gray-50">
        <p className="text-center text-sm font-medium text-gray-500">&copy; {new Date().getFullYear()} TroMoi.vn. All rights reserved.</p>
      </div>

      <button onClick={toTop} title="Lên đầu trang"
        className="fixed bottom-8 right-8 w-12 h-12 bg-white hover:bg-gray-100 text-blue-600 border border-gray-200 rounded-full flex items-center justify-center shadow-xl transition-all hover:scale-110 z-50">
        <FiArrowUp className="w-6 h-6"/>
      </button>
    </footer>
  );
};
export default Footer;