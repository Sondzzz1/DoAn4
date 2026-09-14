import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { ROUTES } from '../../utils/constants';
import Button from '../../components/common/Button';

const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const [searchQuery, setSearchQuery] = useState('');

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    navigate(`${ROUTES.ROOM_LIST}?search=${searchQuery}`);
  };

  return (
    <div>
      {/* Hero Section */}
      <section className="bg-gradient-to-r from-blue-600 to-blue-800 text-white py-20">
        <div className="container mx-auto px-4">
          <div className="max-w-3xl mx-auto text-center">
            <h1 className="text-4xl md:text-5xl font-bold mb-6">
              Tìm phòng trọ dễ dàng, nhanh chóng
            </h1>
            <p className="text-xl mb-8 text-blue-100">
              Kết nối người tìm trọ và chủ nhà một cách hiệu quả nhất
            </p>

            {/* Search Form */}
            <form onSubmit={handleSearch} className="bg-white rounded-lg shadow-lg p-2 flex">
              <input
                type="text"
                placeholder="Tìm phòng theo khu vực, quận, huyện..."
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                className="flex-1 px-4 py-3 text-gray-900 outline-none"
              />
              <Button type="submit" size="lg">
                🔍 Tìm kiếm
              </Button>
            </form>
          </div>
        </div>
      </section>

      {/* Features Section */}
      <section className="py-16 bg-gray-50">
        <div className="container mx-auto px-4">
          <h2 className="text-3xl font-bold text-center mb-12">Tại sao chọn PhòngTrọ247?</h2>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {/* Feature 1 */}
            <div className="bg-white rounded-lg shadow-md p-8 text-center">
              <div className="text-5xl mb-4">🔍</div>
              <h3 className="text-xl font-semibold mb-3">Tìm kiếm dễ dàng</h3>
              <p className="text-gray-600">
                Hệ thống lọc thông minh giúp bạn tìm phòng phù hợp nhanh chóng
              </p>
            </div>

            {/* Feature 2 */}
            <div className="bg-white rounded-lg shadow-md p-8 text-center">
              <div className="text-5xl mb-4">✅</div>
              <h3 className="text-xl font-semibold mb-3">Tin cậy</h3>
              <p className="text-gray-600">
                Mọi tin đăng đều được kiểm duyệt cẩn thận trước khi công khai
              </p>
            </div>

            {/* Feature 3 */}
            <div className="bg-white rounded-lg shadow-md p-8 text-center">
              <div className="text-5xl mb-4">⚡</div>
              <h3 className="text-xl font-semibold mb-3">Nhanh chóng</h3>
              <p className="text-gray-600">
                Đặt lịch xem phòng trực tuyến, liên hệ chủ nhà ngay lập tức
              </p>
            </div>
          </div>
        </div>
      </section>

      {/* How It Works */}
      <section className="py-16">
        <div className="container mx-auto px-4">
          <h2 className="text-3xl font-bold text-center mb-12">Cách thức hoạt động</h2>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-12">
            {/* For Tenants */}
            <div>
              <h3 className="text-2xl font-semibold mb-6 text-blue-600">
                👤 Dành cho người tìm trọ
              </h3>
              <div className="space-y-4">
                <div className="flex items-start">
                  <span className="bg-blue-100 text-blue-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    1
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Đăng ký tài khoản</h4>
                    <p className="text-gray-600">Tạo tài khoản miễn phí chỉ trong vài giây</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-blue-100 text-blue-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    2
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Tìm kiếm phòng</h4>
                    <p className="text-gray-600">Lọc theo khu vực, giá cả, diện tích</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-blue-100 text-blue-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    3
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Đặt lịch xem phòng</h4>
                    <p className="text-gray-600">Chọn thời gian phù hợp để xem phòng</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-blue-100 text-blue-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    4
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Liên hệ chủ nhà</h4>
                    <p className="text-gray-600">Trao đổi trực tiếp với chủ nhà</p>
                  </div>
                </div>
              </div>
            </div>

            {/* For Landlords */}
            <div>
              <h3 className="text-2xl font-semibold mb-6 text-green-600">
                🏘️ Dành cho chủ nhà
              </h3>
              <div className="space-y-4">
                <div className="flex items-start">
                  <span className="bg-green-100 text-green-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    1
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Đăng ký tài khoản</h4>
                    <p className="text-gray-600">Tạo tài khoản chủ nhà miễn phí</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-green-100 text-green-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    2
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Đăng tin cho thuê</h4>
                    <p className="text-gray-600">Thêm thông tin, hình ảnh phòng trọ</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-green-100 text-green-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    3
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Chờ duyệt tin</h4>
                    <p className="text-gray-600">Admin sẽ kiểm duyệt trong 24h</p>
                  </div>
                </div>

                <div className="flex items-start">
                  <span className="bg-green-100 text-green-600 rounded-full w-8 h-8 flex items-center justify-center font-semibold mr-4 flex-shrink-0">
                    4
                  </span>
                  <div>
                    <h4 className="font-semibold mb-1">Quản lý lịch hẹn</h4>
                    <p className="text-gray-600">Xác nhận lịch xem phòng từ khách</p>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="bg-blue-600 text-white py-16">
        <div className="container mx-auto px-4 text-center">
          <h2 className="text-3xl font-bold mb-4">Bắt đầu ngay hôm nay!</h2>
          <p className="text-xl mb-8 text-blue-100">
            Tham gia cùng hàng ngàn người dùng đang tìm kiếm phòng trọ
          </p>
          <div className="flex justify-center gap-4">
            <Button
              size="lg"
              variant="outline"
              onClick={() => navigate(ROUTES.ROOM_LIST)}
              className="bg-white text-blue-600 hover:bg-gray-100"
            >
              Tìm phòng ngay
            </Button>
            <Button
              size="lg"
              onClick={() => navigate(ROUTES.REGISTER)}
              className="bg-blue-800 hover:bg-blue-900"
            >
              Đăng ký miễn phí
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
};

export default HomePage;
