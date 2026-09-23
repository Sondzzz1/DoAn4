import React, { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiChevronRight, FiMapPin, FiNavigation, FiSliders, FiList, FiMap, FiSearch } from 'react-icons/fi';
import { toast } from 'react-toastify';
import { postService, PostQueryParams } from '../../services/postService';
import { PostListItem } from '../../types/post.types';
import RoomListCard from '../../components/room/RoomListCard';
import RoomSidebar from '../../components/room/RoomSidebar';
import LeafletMap from '../../components/map/LeafletMap';
import { formatPrice } from '../../utils/helpers';
import './RoomListPage.css';

// Preset locations for quick radius search
const PRESET_LOCATIONS = [
  { name: 'ĐH Bách Khoa HN', lat: 21.0071, lng: 105.8431 },
  { name: 'ĐH Quốc Gia HN (Cầu Giấy)', lat: 21.0378, lng: 105.7828 },
  { name: 'ĐH Kinh Tế Quốc Dân', lat: 21.0003, lng: 105.8427 },
  { name: 'Hồ Hoàn Kiếm', lat: 21.0285, lng: 105.8542 },
  { name: 'Quận 1 (TP.HCM)', lat: 10.7769, lng: 106.7009 },
  { name: 'ĐH Bách Khoa TP.HCM (Q.10)', lat: 10.7721, lng: 106.6579 },
];

const RoomListPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();

  const [posts, setPosts] = useState<PostListItem[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [viewMode, setViewMode] = useState<'list' | 'map'>('list');

  // Filter states
  const [keyword, setKeyword] = useState<string>(searchParams.get('keyword') || '');
  const [sortBy, setSortBy] = useState<string>(searchParams.get('sortBy') || '');
  
  // Radius map filter states
  const [enableRadiusSearch, setEnableRadiusSearch] = useState<boolean>(false);
  const [selectedLat, setSelectedLat] = useState<number>(21.0285);
  const [selectedLng, setSelectedLng] = useState<number>(105.8542);
  const [radiusKm, setRadiusKm] = useState<number>(5);
  const [locationName, setLocationName] = useState<string>('Hà Nội');

  const fetchPosts = async () => {
    setLoading(true);
    try {
      const params: PostQueryParams = {
        keyword: keyword.trim() || undefined,
        sortBy: sortBy || undefined,
      };

      if (enableRadiusSearch) {
        params.latitude = selectedLat;
        params.longitude = selectedLng;
        params.radiusInKm = radiusKm;
      }

      const res = await postService.getPosts(params);
      setPosts(res.data || []);
    } catch (error: any) {
      toast.error('Không thể tải danh sách phòng trọ.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void fetchPosts();
  }, [sortBy, enableRadiusSearch, selectedLat, selectedLng, radiusKm]);

  const handleSearchSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    void fetchPosts();
  };

  const handleSelectPreset = (preset: typeof PRESET_LOCATIONS[0]) => {
    setSelectedLat(preset.lat);
    setSelectedLng(preset.lng);
    setLocationName(preset.name);
    setEnableRadiusSearch(true);
  };

  const handleGetCurrentLocation = () => {
    if (!navigator.geolocation) {
      toast.warning('Trình duyệt của bạn không hỗ trợ định vị GPS.');
      return;
    }

    navigator.geolocation.getCurrentPosition(
      (pos) => {
        setSelectedLat(pos.coords.latitude);
        setSelectedLng(pos.coords.longitude);
        setLocationName('Vị trí hiện tại của bạn');
        setEnableRadiusSearch(true);
        toast.success('Đã lấy tọa độ vị trí hiện tại của bạn!');
      },
      (err) => {
        toast.error('Không thể lấy vị trí. Vui lòng cho phép quyền truy cập vị trí trên trình duyệt.');
      }
    );
  };

  return (
    <div className="room-list-page">
      {/* Top Search Banner */}
      <div className="bg-gradient-to-r from-blue-700 via-blue-600 to-indigo-700 text-white py-10 px-4">
        <div className="max-w-6xl mx-auto">
          <h1 className="text-3xl font-extrabold mb-2">Tìm kiếm phòng trọ, nhà nguyên căn, căn hộ</h1>
          <p className="text-blue-100 text-sm mb-6">
            Khám phá hàng ngàn phòng trọ phù hợp ngân sách và vị trí của bạn
          </p>

          <form onSubmit={handleSearchSubmit} className="flex flex-col md:flex-row gap-3 bg-white p-2.5 rounded-3xl shadow-xl">
            <div className="flex-1 flex items-center gap-3 px-4">
              <FiSearch className="text-slate-400 text-lg" />
              <input
                type="text"
                placeholder="Nhập địa chỉ, quận huyện, tên đường, trường đại học..."
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                className="w-full text-slate-800 text-sm focus:outline-none"
              />
            </div>

            <button
              type="submit"
              className="px-8 py-3.5 bg-[#0084ff] hover:bg-[#0073e6] text-white font-bold text-sm rounded-2xl transition-all shadow-md shadow-blue-500/30 cursor-pointer border-none"
            >
              Tìm kiếm
            </button>
          </form>
        </div>
      </div>

      <main className="max-w-6xl mx-auto px-4 py-8">
        {/* Breadcrumb & Controls */}
        <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-6">
          <nav className="flex items-center gap-2 text-xs text-slate-500">
            <Link to="/" className="hover:text-[#0084ff]">Trang chủ</Link>
            <FiChevronRight />
            <span className="font-semibold text-slate-800">Danh sách phòng trọ</span>
          </nav>

          <div className="flex items-center gap-3">
            {/* Radius search toggle button */}
            <button
              type="button"
              onClick={() => setEnableRadiusSearch(!enableRadiusSearch)}
              className={`flex items-center gap-2 px-4 py-2 rounded-2xl text-xs font-bold transition-all cursor-pointer border ${
                enableRadiusSearch
                  ? 'bg-blue-50 border-blue-200 text-[#0084ff]'
                  : 'bg-white border-slate-200 text-slate-700 hover:bg-slate-50'
              }`}
            >
              <FiMapPin className={enableRadiusSearch ? 'text-[#0084ff]' : 'text-slate-400'} />
              {enableRadiusSearch ? 'Đang lọc bán kính: ' + radiusKm + ' km' : 'Tìm theo bán kính & bản đồ'}
            </button>

            {/* View Mode Toggle */}
            <div className="flex bg-slate-100 p-1 rounded-2xl border border-slate-200">
              <button
                type="button"
                onClick={() => setViewMode('list')}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer border-none ${
                  viewMode === 'list' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-800 bg-transparent'
                }`}
              >
                <FiList /> Danh sách
              </button>
              <button
                type="button"
                onClick={() => setViewMode('map')}
                className={`flex items-center gap-1.5 px-3 py-1.5 rounded-xl text-xs font-bold transition-all cursor-pointer border-none ${
                  viewMode === 'map' ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-800 bg-transparent'
                }`}
              >
                <FiMap /> Bản đồ
              </button>
            </div>

            {/* Sort Select */}
            <select
              value={sortBy}
              onChange={(e) => setSortBy(e.target.value)}
              className="px-3 py-2 rounded-2xl border border-slate-200 text-xs font-semibold bg-white text-slate-700 focus:outline-none"
            >
              <option value="">Mới nhất</option>
              {enableRadiusSearch && <option value="distance">Gần nhất trước</option>}
              <option value="price_asc">Giá tăng dần</option>
              <option value="price_desc">Giá giảm dần</option>
              <option value="area_asc">Diện tích nhỏ đến lớn</option>
              <option value="area_desc">Diện tích lớn đến nhỏ</option>
            </select>
          </div>
        </div>

        {/* Radius Search Filter Panel */}
        {enableRadiusSearch && (
          <div className="bg-white rounded-3xl border border-blue-100 p-6 shadow-md mb-8 animate-in fade-in slide-in-from-top-4">
            <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-6 pb-4 border-b border-slate-100">
              <div>
                <h3 className="text-base font-bold text-slate-900 flex items-center gap-2">
                  <FiMapPin className="text-[#0084ff]" /> Tìm phòng theo tâm điểm & bán kính
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Đang quét khu vực quanh: <b className="text-slate-800">{locationName}</b>
                </p>
              </div>

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={handleGetCurrentLocation}
                  className="flex items-center gap-1.5 px-3.5 py-2 bg-slate-100 hover:bg-slate-200 text-slate-700 text-xs font-bold rounded-xl transition-all cursor-pointer border-none"
                >
                  <FiNavigation className="text-[#0084ff]" /> Vị trí của tôi
                </button>
              </div>
            </div>

            {/* Presets & Slider */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-4 items-center">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-2">Điểm mốc phổ biến:</label>
                <div className="flex flex-wrap gap-2">
                  {PRESET_LOCATIONS.map((preset) => (
                    <button
                      key={preset.name}
                      type="button"
                      onClick={() => handleSelectPreset(preset)}
                      className={`px-3 py-1.5 rounded-xl text-xs font-semibold border transition-all cursor-pointer ${
                        locationName === preset.name
                          ? 'bg-[#0084ff] border-[#0084ff] text-white shadow-sm'
                          : 'bg-slate-50 border-slate-200 text-slate-600 hover:bg-slate-100'
                      }`}
                    >
                      {preset.name}
                    </button>
                  ))}
                </div>
              </div>

              <div className="bg-slate-50 p-4 rounded-2xl border border-slate-100">
                <div className="flex items-center justify-between text-xs font-bold text-slate-700 mb-2">
                  <span>Bán kính quét:</span>
                  <span className="text-base font-extrabold text-[#0084ff]">{radiusKm} km</span>
                </div>
                <input
                  type="range"
                  min="1"
                  max="20"
                  step="1"
                  value={radiusKm}
                  onChange={(e) => setRadiusKm(Number(e.target.value))}
                  className="w-full accent-[#0084ff] cursor-pointer"
                />
                <div className="flex justify-between text-[10px] text-slate-400 mt-1">
                  <span>1 km (đi bộ)</span>
                  <span>5 km (xe máy 10p)</span>
                  <span>10 km</span>
                  <span>20 km</span>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* View Content */}
        {viewMode === 'map' ? (
          /* Map View Mode */
          <div className="space-y-6">
            <LeafletMap
              height="550px"
              center={[selectedLat, selectedLng]}
              rooms={posts}
              radiusCircle={
                enableRadiusSearch
                  ? { center: [selectedLat, selectedLng], radiusInKm: radiusKm }
                  : undefined
              }
              zoom={enableRadiusSearch ? 13 : 12}
            />

            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              {posts.slice(0, 6).map((post) => (
                <div key={post.id} className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
                  <h4 className="font-bold text-slate-900 text-sm line-clamp-1 mb-1">{post.title}</h4>
                  <div className="text-[#0084ff] font-bold text-sm mb-2">{formatPrice(post.price)}/tháng</div>
                  {post.distanceInKm && (
                    <span className="text-xs font-semibold text-emerald-600 bg-emerald-50 px-2 py-0.5 rounded-lg border border-emerald-100">
                      📍 Cách bạn: {post.distanceInKm} km
                    </span>
                  )}
                  <Link to={`/rooms/${post.id}`} className="text-xs font-bold text-[#0084ff] block mt-2 hover:underline">
                    Xem chi tiết →
                  </Link>
                </div>
              ))}
            </div>
          </div>
        ) : (
          /* Standard List View Mode */
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
            {/* Main column */}
            <div className="lg:col-span-2">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-lg font-bold text-slate-900">
                  {enableRadiusSearch
                    ? `Phòng trong bán kính ${radiusKm}km (${posts.length} kết quả)`
                    : `Danh sách phòng (${posts.length} kết quả)`}
                </h2>
              </div>

              {loading ? (
                <div className="space-y-4">
                  {[1, 2, 3, 4].map((i) => (
                    <div key={i} className="bg-white rounded-3xl border border-slate-200 p-4 animate-pulse flex gap-4">
                      <div className="w-48 h-32 bg-slate-100 rounded-2xl" />
                      <div className="flex-1 space-y-2 py-2">
                        <div className="h-5 bg-slate-100 rounded w-3/4" />
                        <div className="h-4 bg-slate-100 rounded w-1/2" />
                        <div className="h-6 bg-slate-100 rounded w-1/3 mt-4" />
                      </div>
                    </div>
                  ))}
                </div>
              ) : posts.length === 0 ? (
                <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
                  <FiMapPin className="mx-auto text-4xl text-slate-300 mb-2" />
                  <h3 className="text-base font-bold text-slate-800 mb-1">Không tìm thấy phòng trọ phù hợp</h3>
                  <p className="text-xs text-slate-400">Hãy thử mở rộng bán kính tìm kiếm hoặc thay đổi từ khóa.</p>
                </div>
              ) : (
                <div className="space-y-4">
                  {posts.map((post) => (
                    <div
                      key={post.id}
                      className="bg-white rounded-3xl border border-slate-200/80 p-4 shadow-sm hover:shadow-md transition-all flex flex-col sm:flex-row gap-4 group"
                    >
                      {/* Image */}
                      <div className="sm:w-56 h-40 rounded-2xl bg-slate-100 overflow-hidden relative shrink-0">
                        {post.thumbnailUrl ? (
                          <img
                            src={post.thumbnailUrl}
                            alt={post.title}
                            className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300"
                          />
                        ) : (
                          <div className="w-full h-full flex items-center justify-center text-slate-400">🏠</div>
                        )}
                        <div className="absolute top-2 left-2 bg-[#0084ff] text-white text-[11px] font-bold px-2.5 py-1 rounded-full shadow">
                          {formatPrice(post.price)}/tháng
                        </div>
                      </div>

                      {/* Content */}
                      <div className="flex-1 flex flex-col justify-between py-1">
                        <div>
                          <h3 className="font-bold text-slate-900 text-base mb-1.5 line-clamp-2 hover:text-[#0084ff] transition-colors">
                            <Link to={`/rooms/${post.id}`}>{post.title}</Link>
                          </h3>

                          <p className="text-xs text-slate-500 flex items-center gap-1.5 mb-2 line-clamp-1">
                            <FiMapPin className="text-slate-400 shrink-0" />
                            <span>{post.address}, {post.ward}, {post.district}, {post.province}</span>
                          </p>

                          {post.distanceInKm && (
                            <span className="inline-flex items-center gap-1 text-xs font-bold text-emerald-600 bg-emerald-50 px-2.5 py-0.5 rounded-full border border-emerald-100 mb-2">
                              📍 Cách tâm điểm: {post.distanceInKm} km
                            </span>
                          )}
                        </div>

                        <div className="flex items-center justify-between pt-3 border-t border-slate-100 text-xs text-slate-600">
                          <span>📐 {post.area} m² · 👥 {post.maxOccupants} người</span>
                          <Link
                            to={`/rooms/${post.id}`}
                            className="text-[#0084ff] font-bold hover:underline"
                          >
                            Xem chi tiết →
                          </Link>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              )}
            </div>

            {/* Sidebar */}
            <div className="lg:col-span-1">
              <RoomSidebar latestRooms={posts.map(p => ({
                id: p.id,
                title: p.title,
                price: p.price,
                area: p.area,
                imageUrl: p.thumbnailUrl || '',
                address: p.address || '',
                district: p.district,
                province: p.province,
                createdAt: p.createdAt ? String(p.createdAt) : '',
                category: 'room'
              }))} />
            </div>
          </div>
        )}
      </main>
    </div>
  );
};

export default RoomListPage;
