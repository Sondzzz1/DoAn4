import React from 'react';
import { Link } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';

const Footer: React.FC = () => {
  return (
    <footer className="bg-gray-800 text-white mt-auto">
      <div className="container mx-auto px-4 py-8">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {/* About */}
          <div>
            <h3 className="text-lg font-semibold mb-4">Về PhòngTrọ247</h3>
            <p className="text-gray-400 text-sm">
              Hệ thống tìm kiếm và cho thuê phòng trọ trực tuyến, kết nối người tìm trọ và chủ nhà một cách nhanh chóng, tiện lợi.
            </p>
          </div>

          {/* Quick Links */}
          <div>
            <h3 className="text-lg font-semibold mb-4">Liên kết</h3>
            <ul className="space-y-2 text-sm">
              <li>
                <Link to={ROUTES.HOME} className="text-gray-400 hover:text-white transition-colors">
                  Trang chủ
                </Link>
              </li>
              <li>
                <Link to={ROUTES.ROOM_LIST} className="text-gray-400 hover:text-white transition-colors">
                  Tìm phòng
                </Link>
              </li>
              <li>
                <Link to={ROUTES.LOGIN} className="text-gray-400 hover:text-white transition-colors">
                  Đăng nhập
                </Link>
              </li>
              <li>
                <Link to={ROUTES.REGISTER} className="text-gray-400 hover:text-white transition-colors">
                  Đăng ký
                </Link>
              </li>
            </ul>
          </div>

          {/* Contact */}
          <div>
            <h3 className="text-lg font-semibold mb-4">Liên hệ</h3>
            <ul className="space-y-2 text-sm text-gray-400">
              <li>📧 Email: support@phongtro247.com</li>
              <li>📞 Hotline: 1900 1234</li>
              <li>📍 TP. Hồ Chí Minh, Việt Nam</li>
            </ul>
          </div>
        </div>

        <div className="border-t border-gray-700 mt-8 pt-8 text-center text-sm text-gray-400">
          <p>&copy; {new Date().getFullYear()} PhòngTrọ247. All rights reserved.</p>
        </div>
      </div>
    </footer>
  );
};

export default Footer;
