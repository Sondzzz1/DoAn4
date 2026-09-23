import React, { useEffect, useState } from 'react';
import {
  FiFileText,
  FiDollarSign,
  FiAlertTriangle,
  FiStar,
  FiCheckCircle,
  FiClock,
  FiXCircle,
  FiPlus,
  FiCalendar,
  FiCheck,
} from 'react-icons/fi';
import { toast } from 'react-toastify';
import {
  rentalService,
  RentalRequestDto,
  DepositDto,
  ContractDto,
  IncidentDto,
  ReviewDto,
} from '../../services/rentalService';
import { paymentService } from '../../services/paymentService';
import { formatPrice } from '../../utils/helpers';
import { FiCreditCard } from 'react-icons/fi';

const TenantRentalsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'requests' | 'contracts' | 'incidents' | 'reviews'>('requests');
  const [loading, setLoading] = useState(true);

  const [requests, setRequests] = useState<RentalRequestDto[]>([]);
  const [deposits, setDeposits] = useState<DepositDto[]>([]);
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [incidents, setIncidents] = useState<IncidentDto[]>([]);
  const [reviews, setReviews] = useState<ReviewDto[]>([]);

  // Incident Modal State
  const [incidentModalOpen, setIncidentModalOpen] = useState(false);
  const [selectedContractId, setSelectedContractId] = useState<number | null>(null);
  const [incidentTitle, setIncidentTitle] = useState('');
  const [incidentDesc, setIncidentDesc] = useState('');
  const [submittingIncident, setSubmittingIncident] = useState(false);

  // Review Modal State
  const [reviewModalOpen, setReviewModalOpen] = useState(false);
  const [reviewContractId, setReviewContractId] = useState<number | null>(null);
  const [reviewRating, setReviewRating] = useState(5);
  const [reviewComment, setReviewComment] = useState('');
  const [submittingReview, setSubmittingReview] = useState(false);

  const loadData = async () => {
    setLoading(true);
    try {
      const [reqRes, depRes, conRes, incRes, revRes] = await Promise.all([
        rentalService.getMyRentalRequests(),
        rentalService.getMyDeposits(),
        rentalService.getMyContracts(),
        rentalService.getMyIncidents(),
        rentalService.getMyReviews(),
      ]);

      setRequests(reqRes.data || []);
      setDeposits(depRes.data || []);
      setContracts(conRes.data || []);
      setIncidents(incRes.data || []);
      setReviews(revRes.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải dữ liệu thuê phòng.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  const handleConfirmContract = async (contractId: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xác nhận đồng ý hợp đồng này?')) return;

    try {
      await rentalService.confirmContract(contractId);
      toast.success('Xác nhận hợp đồng thành công!');
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể xác nhận hợp đồng.');
    }
  };

  const handleCreateIncident = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedContractId || !incidentTitle.trim() || !incidentDesc.trim()) {
      toast.warning('Vui lòng điền đầy đủ tiêu đề và nội dung sự cố.');
      return;
    }

    setSubmittingIncident(true);
    try {
      await rentalService.createIncident({
        hopDongId: selectedContractId,
        tieuDe: incidentTitle.trim(),
        moTa: incidentDesc.trim(),
      });
      toast.success('Báo cáo sự cố thành công! Chủ trọ sẽ sớm xử lý.');
      setIncidentModalOpen(false);
      setIncidentTitle('');
      setIncidentDesc('');
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể gửi báo cáo sự cố.');
    } finally {
      setSubmittingIncident(false);
    }
  };

  const handleCreateReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!reviewContractId) return;

    setSubmittingReview(true);
    try {
      await rentalService.createReview({
        hopDongId: reviewContractId,
        soSao: reviewRating,
        nhanXet: reviewComment.trim() || undefined,
      });
      toast.success('Đánh giá phòng thành công! Cảm ơn bạn đã phản hồi.');
      setReviewModalOpen(false);
      setReviewComment('');
      setReviewRating(5);
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể gửi đánh giá.');
    } finally {
      setSubmittingReview(false);
    }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-[#0084ff] font-bold mb-1">
          <FiFileText /> Quản lý thuê phòng
        </div>
        <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">Hợp đồng & Thuê phòng</h1>
        <p className="text-sm text-slate-500 mt-1">
          Theo dõi các yêu cầu thuê phòng, thông tin cọc, hợp đồng thuê và phản ánh sự cố.
        </p>
      </div>

      {/* Stats Summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Yêu cầu gửi đi</span>
          <div className="text-2xl font-bold text-slate-900 mt-1">{requests.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Hợp đồng</span>
          <div className="text-2xl font-bold text-emerald-600 mt-1">{contracts.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Tiền cọc</span>
          <div className="text-2xl font-bold text-blue-600 mt-1">{deposits.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Sự cố báo cáo</span>
          <div className="text-2xl font-bold text-amber-600 mt-1">{incidents.length}</div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-200 mb-6 overflow-x-auto pb-1">
        <button
          onClick={() => setActiveTab('requests')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap ${
            activeTab === 'requests'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Yêu cầu thuê ({requests.length})
        </button>
        <button
          onClick={() => setActiveTab('contracts')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap ${
            activeTab === 'contracts'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Hợp đồng & Cọc ({contracts.length})
        </button>
        <button
          onClick={() => setActiveTab('incidents')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap ${
            activeTab === 'incidents'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Sự cố phòng ({incidents.length})
        </button>
        <button
          onClick={() => setActiveTab('reviews')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap ${
            activeTab === 'reviews'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Đánh giá của tôi ({reviews.length})
        </button>
      </div>

      {/* Tab Content */}
      {loading ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 animate-pulse">
          Đang tải dữ liệu...
        </div>
      ) : activeTab === 'requests' ? (
        /* TAB 1: RENTAL REQUESTS */
        requests.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
            Bạn chưa gửi yêu cầu thuê phòng nào.
          </div>
        ) : (
          <div className="space-y-4">
            {requests.map((item) => (
              <div
                key={item.id}
                className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm flex flex-col md:flex-row md:items-center justify-between gap-4"
              >
                <div>
                  <div className="flex items-center gap-2 mb-2">
                    <h3 className="text-lg font-bold text-slate-900">{item.tieuDeBaiDang || 'Bài đăng thuê phòng'}</h3>
                    <span
                      className={`px-3 py-1 rounded-full text-xs font-bold ${
                        item.trangThai === 0
                          ? 'bg-amber-100 text-amber-700'
                          : item.trangThai === 1
                          ? 'bg-emerald-100 text-emerald-700'
                          : 'bg-rose-100 text-rose-700'
                      }`}
                    >
                      {item.trangThai === 0 ? 'Đang chờ chủ trọ duyệt' : item.trangThai === 1 ? 'Chủ trọ đã chấp nhận' : 'Bị từ chối'}
                    </span>
                  </div>

                  <div className="text-sm text-slate-500 space-y-1">
                    <p className="flex items-center gap-2">
                      <FiCalendar size={14} /> Ngày gửi: {new Date(item.ngayTao).toLocaleDateString('vi-VN')}
                    </p>
                    {item.ghiChu && <p className="text-slate-700 bg-slate-50 p-2.5 rounded-xl border border-slate-100 text-xs">Ghi chú: {item.ghiChu}</p>}
                  </div>
                </div>
              </div>
            ))}
          </div>
        )
      ) : activeTab === 'contracts' ? (
        /* TAB 2: CONTRACTS & DEPOSITS */
        contracts.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
            Chưa có hợp đồng thuê phòng nào được tạo.
          </div>
        ) : (
          <div className="space-y-6">
            {contracts.map((item) => (
              <div key={item.id} className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm">
                <div className="flex flex-col md:flex-row md:items-start justify-between gap-4 border-b border-slate-100 pb-4 mb-4">
                  <div>
                    <div className="flex items-center gap-2 mb-1">
                      <span className="text-xs font-bold uppercase tracking-wider text-[#0084ff]">Hợp đồng #{item.id}</span>
                      <span
                        className={`px-2.5 py-0.5 rounded-full text-xs font-bold ${
                          item.trangThai === 1 ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'
                        }`}
                      >
                        {item.trangThai === 1 ? 'Đang hiệu lực' : 'Chờ 2 bên xác nhận'}
                      </span>
                    </div>
                    <div className="text-2xl font-black text-slate-900">{formatPrice(item.tienThueHangThang)}/tháng</div>
                  </div>

                  <div className="flex items-center gap-2">
                    {!item.nguoiThueDaXacNhan ? (
                      <button
                        onClick={() => handleConfirmContract(item.id)}
                        className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all"
                      >
                        <FiCheck /> Xác nhận hợp đồng
                      </button>
                    ) : (
                      <span className="inline-flex items-center gap-1.5 text-xs text-emerald-600 font-bold bg-emerald-50 px-3 py-1.5 rounded-xl border border-emerald-100">
                        <FiCheckCircle /> Bạn đã xác nhận
                      </span>
                    )}

                    {item.trangThai === 1 && (
                      <>
                        <button
                          onClick={() => {
                            setSelectedContractId(item.id);
                            setIncidentModalOpen(true);
                          }}
                          className="inline-flex items-center gap-1.5 px-3 py-2 bg-amber-50 hover:bg-amber-100 text-amber-700 text-xs font-bold rounded-xl border border-amber-200 cursor-pointer transition-all"
                        >
                          <FiAlertTriangle /> Báo sự cố
                        </button>
                        <button
                          onClick={() => {
                            setReviewContractId(item.id);
                            setReviewModalOpen(true);
                          }}
                          className="inline-flex items-center gap-1.5 px-3 py-2 bg-blue-50 hover:bg-blue-100 text-[#0084ff] text-xs font-bold rounded-xl border border-blue-200 cursor-pointer transition-all"
                        >
                          <FiStar /> Đánh giá phòng
                        </button>
                      </>
                    )}
                  </div>
                </div>

                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-xs text-slate-600">
                  <div>
                    <span className="text-slate-400 block mb-0.5">Ngày bắt đầu</span>
                    <span className="font-bold text-slate-800">{new Date(item.ngayBatDau).toLocaleDateString('vi-VN')}</span>
                  </div>
                  <div>
                    <span className="text-slate-400 block mb-0.5">Ngày kết thúc</span>
                    <span className="font-bold text-slate-800">{new Date(item.ngayKetThuc).toLocaleDateString('vi-VN')}</span>
                  </div>
                  <div>
                    <span className="text-slate-400 block mb-0.5">Chủ trọ xác nhận</span>
                    <span className={`font-bold ${item.chuTroDaXacNhan ? 'text-emerald-600' : 'text-amber-600'}`}>
                      {item.chuTroDaXacNhan ? 'Đã xác nhận' : 'Chưa xác nhận'}
                    </span>
                  </div>
                  <div>
                    <span className="text-slate-400 block mb-0.5">Người thuê xác nhận</span>
                    <span className={`font-bold ${item.nguoiThueDaXacNhan ? 'text-emerald-600' : 'text-amber-600'}`}>
                      {item.nguoiThueDaXacNhan ? 'Đã xác nhận' : 'Chưa xác nhận'}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )
      ) : activeTab === 'incidents' ? (
        /* TAB 3: INCIDENTS */
        incidents.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
            Chưa có sự cố nào được báo cáo.
          </div>
        ) : (
          <div className="space-y-4">
            {incidents.map((item) => (
              <div key={item.id} className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm">
                <div className="flex items-center justify-between gap-4 mb-2">
                  <h3 className="text-base font-bold text-slate-900">{item.tieuDe}</h3>
                  <span
                    className={`px-2.5 py-0.5 rounded-full text-xs font-bold ${
                      item.trangThai === 0
                        ? 'bg-amber-100 text-amber-700'
                        : item.trangThai === 1
                        ? 'bg-blue-100 text-blue-700'
                        : 'bg-emerald-100 text-emerald-700'
                    }`}
                  >
                    {item.trangThai === 0 ? 'Chờ xử lý' : item.trangThai === 1 ? 'Đang xử lý' : 'Đã giải quyết'}
                  </span>
                </div>
                <p className="text-sm text-slate-600 mb-3">{item.moTa}</p>
                {item.huongXuLy && (
                  <div className="bg-emerald-50 text-emerald-800 p-3 rounded-xl text-xs border border-emerald-100 font-medium">
                    Phản hồi từ chủ trọ: {item.huongXuLy}
                  </div>
                )}
              </div>
            ))}
          </div>
        )
      ) : (
        /* TAB 4: REVIEWS */
        reviews.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
            Bạn chưa viết đánh giá phòng trọ nào.
          </div>
        ) : (
          <div className="space-y-4">
            {reviews.map((item) => (
              <div key={item.id} className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm">
                <div className="flex items-center gap-2 mb-2">
                  <div className="flex text-amber-400">
                    {[1, 2, 3, 4, 5].map((s) => (
                      <FiStar key={s} className={s <= item.soSao ? 'fill-amber-400' : 'text-slate-200'} />
                    ))}
                  </div>
                  <span className="text-xs text-slate-400">
                    {new Date(item.ngayTao).toLocaleDateString('vi-VN')}
                  </span>
                </div>
                <p className="text-sm text-slate-700">{item.nhanXet || 'Không có nhận xét chi tiết.'}</p>
              </div>
            ))}
          </div>
        )
      )}

      {/* Modal Báo Sự Cố */}
      {incidentModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl animate-in fade-in zoom-in-95">
            <h3 className="text-xl font-bold text-slate-900 mb-4 flex items-center gap-2">
              <FiAlertTriangle className="text-amber-500" /> Báo cáo sự cố phòng trọ
            </h3>
            <form onSubmit={handleCreateIncident} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">Tiêu đề sự cố *</label>
                <input
                  type="text"
                  placeholder="Ví dụ: Hỏng vòi nước, chập bóng đèn..."
                  value={incidentTitle}
                  onChange={(e) => setIncidentTitle(e.target.value)}
                  required
                  className="w-full px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-[#0084ff]"
                />
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">Mô tả chi tiết *</label>
                <textarea
                  rows={4}
                  placeholder="Mô tả cụ thể tình trạng sự cố để chủ trọ kịp thời xử lý..."
                  value={incidentDesc}
                  onChange={(e) => setIncidentDesc(e.target.value)}
                  required
                  className="w-full px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-[#0084ff]"
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setIncidentModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-600 hover:bg-slate-100"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submittingIncident}
                  className="px-5 py-2 rounded-xl text-xs font-bold bg-amber-500 hover:bg-amber-600 text-white shadow-md cursor-pointer"
                >
                  {submittingIncident ? 'Đang gửi...' : 'Gửi báo cáo'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Modal Đánh Giá Phòng */}
      {reviewModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl animate-in fade-in zoom-in-95">
            <h3 className="text-xl font-bold text-slate-900 mb-4 flex items-center gap-2">
              <FiStar className="text-amber-400" /> Đánh giá trải nghiệm phòng trọ
            </h3>
            <form onSubmit={handleCreateReview} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-2">Số sao đánh giá *</label>
                <div className="flex items-center gap-2 text-2xl text-amber-400">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <button
                      key={s}
                      type="button"
                      onClick={() => setReviewRating(s)}
                      className="cursor-pointer hover:scale-110 transition-transform bg-transparent border-none"
                    >
                      <FiStar className={s <= reviewRating ? 'fill-amber-400' : 'text-slate-200'} />
                    </button>
                  ))}
                  <span className="text-sm font-bold text-slate-700 ml-2">{reviewRating} / 5 sao</span>
                </div>
              </div>
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-1">Nhận xét chi tiết</label>
                <textarea
                  rows={4}
                  placeholder="Chia sẻ trải nghiệm về phòng trọ, sự hỗ trợ của chủ trọ, an ninh khu vực..."
                  value={reviewComment}
                  onChange={(e) => setReviewComment(e.target.value)}
                  className="w-full px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-[#0084ff]"
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setReviewModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-xs font-semibold text-slate-600 hover:bg-slate-100"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submittingReview}
                  className="px-5 py-2 rounded-xl text-xs font-bold bg-[#0084ff] hover:bg-[#0073e6] text-white shadow-md cursor-pointer"
                >
                  {submittingReview ? 'Đang gửi...' : 'Gửi đánh giá'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default TenantRentalsPage;
