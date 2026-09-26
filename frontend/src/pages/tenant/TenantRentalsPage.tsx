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
  FiZap,
  FiDroplet,
  FiDownload,
  FiCreditCard,
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
import {
  monthlyBillService,
  MonthlyBillDto,
} from '../../services/monthlyBillService';
import { exportService } from '../../services/exportService';
import { paymentService } from '../../services/paymentService';
import { formatPrice } from '../../utils/helpers';

const TenantRentalsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'requests' | 'contracts' | 'bills' | 'incidents' | 'reviews'>('bills');
  const [loading, setLoading] = useState(true);

  const [requests, setRequests] = useState<RentalRequestDto[]>([]);
  const [deposits, setDeposits] = useState<DepositDto[]>([]);
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [bills, setBills] = useState<MonthlyBillDto[]>([]);
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
      const [reqRes, depRes, conRes, billsRes, incRes, revRes] = await Promise.all([
        rentalService.getMyRentalRequests(),
        rentalService.getMyDeposits(),
        rentalService.getMyContracts(),
        monthlyBillService.getMyBills(false),
        rentalService.getMyIncidents(),
        rentalService.getMyReviews(),
      ]);

      setRequests(reqRes.data || []);
      setDeposits(depRes.data || []);
      setContracts(conRes.data || []);
      setBills(billsRes.data || []);
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

  const handlePayBill = async (billId: number) => {
    if (!window.confirm('Xác nhận bạn đã chuyển khoản hoặc thanh toán hóa đơn này?')) return;
    try {
      await monthlyBillService.payBill(billId, {
        phuongThucThanhToan: 'Người thuê xác nhận đã chuyển khoản/thanh toán',
      });
      toast.success('Xác nhận thanh toán hóa đơn thành công!');
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể xác nhận thanh toán.');
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

  const handlePayDepositVnPay = async (depositId: number) => {
    try {
      const res = await paymentService.createVnPayUrl({
        depositId: depositId,
        orderInfo: `Thanh toan tien coc phong dat coc ID ${depositId}`,
      });
      if (res.data?.paymentUrl) {
        toast.info('Đang chuyển hướng sang cổng thanh toán VNPay Sandbox...');
        window.location.href = res.data.paymentUrl;
      }
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tạo liên kết thanh toán VNPay.');
    }
  };

  return (
    <div className="max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-[#0084ff] font-bold mb-1">
          <FiFileText /> Quản lý thuê phòng
        </div>
        <h1 className="text-3xl font-extrabold text-slate-900 tracking-tight">Hợp Đồng & Hóa Đơn Thuê Phòng</h1>
        <p className="text-sm text-slate-500 mt-1">
          Xem và thanh toán hóa đơn điện nước hàng tháng, quản lý hợp đồng thuê, đặt cọc và phản ánh sự cố.
        </p>
      </div>

      {/* Stats Summary */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Hóa đơn điện nước</span>
          <div className="text-2xl font-bold text-[#0084ff] mt-1">{bills.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Hợp đồng thuê</span>
          <div className="text-2xl font-bold text-emerald-600 mt-1">{contracts.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Yêu cầu & Đặt cọc</span>
          <div className="text-2xl font-bold text-slate-900 mt-1">{requests.length}</div>
        </div>
        <div className="bg-white rounded-2xl border border-slate-200 p-4 shadow-sm">
          <span className="text-xs text-slate-400 font-semibold uppercase">Sự cố phòng</span>
          <div className="text-2xl font-bold text-amber-600 mt-1">{incidents.length}</div>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex items-center gap-2 border-b border-slate-200 mb-6 overflow-x-auto pb-1">
        <button
          onClick={() => setActiveTab('bills')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
            activeTab === 'bills'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          <FiZap /> Hóa đơn điện nước ({bills.length})
        </button>
        <button
          onClick={() => setActiveTab('contracts')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
            activeTab === 'contracts'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          <FiFileText /> Hợp đồng ({contracts.length})
        </button>
        <button
          onClick={() => setActiveTab('requests')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
            activeTab === 'requests'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Yêu cầu & Cọc ({requests.length})
        </button>
        <button
          onClick={() => setActiveTab('incidents')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
            activeTab === 'incidents'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Sự cố phòng ({incidents.length})
        </button>
        <button
          onClick={() => setActiveTab('reviews')}
          className={`px-4 py-2.5 rounded-xl font-semibold text-sm transition-all whitespace-nowrap flex items-center gap-2 cursor-pointer ${
            activeTab === 'reviews'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Đánh giá ({reviews.length})
        </button>
      </div>

      {/* Tab Content */}
      {loading ? (
        <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 animate-pulse">
          Đang tải dữ liệu...
        </div>
      ) : activeTab === 'bills' ? (
        /* TAB: BILLS */
        bills.length === 0 ? (
          <div className="bg-white rounded-3xl border border-slate-200 p-12 text-center text-slate-500 shadow-sm">
            <FiZap className="mx-auto text-4xl text-slate-300 mb-2" />
            <p className="font-semibold text-slate-700">Chưa có hóa đơn tiền phòng nào được gửi.</p>
            <p className="text-xs text-slate-400 mt-1">Khi chủ trọ lập chỉ số điện nước hàng tháng, hóa đơn sẽ hiển thị tại đây.</p>
          </div>
        ) : (
          <div className="space-y-4">
            {bills.map((bill) => (
              <div
                key={bill.id}
                className="bg-white rounded-3xl border border-slate-200/80 p-6 shadow-sm hover:shadow-md transition-all"
              >
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-slate-100 pb-4 mb-4">
                  <div className="flex items-center gap-3">
                    <div className="w-12 h-12 rounded-2xl bg-blue-50 text-[#0084ff] flex items-center justify-center font-black text-lg">
                      T{bill.thang}
                    </div>
                    <div>
                      <div className="flex items-center gap-2">
                        <h3 className="text-lg font-bold text-slate-900">
                          Hóa đơn tiền phòng tháng {bill.thang}/{bill.nam}
                        </h3>
                        <span
                          className={`px-3 py-1 rounded-full text-xs font-bold ${
                            bill.trangThai === 1
                              ? 'bg-emerald-100 text-emerald-700'
                              : 'bg-amber-100 text-amber-700'
                          }`}
                        >
                          {bill.trangThai === 1 ? 'Đã thanh toán' : 'Chờ thanh toán'}
                        </span>
                      </div>
                      <p className="text-xs text-slate-500 mt-0.5">
                        {bill.tenPhong} • Chủ trọ: {bill.tenChuTro} {bill.sdtChuTro ? `(${bill.sdtChuTro})` : ''}
                      </p>
                    </div>
                  </div>

                  <div className="flex items-center gap-3">
                    <div className="text-right">
                      <span className="text-xs text-slate-400 font-semibold block">Tổng thanh toán</span>
                      <div className="text-2xl font-black text-[#0084ff]">{formatPrice(bill.tongTien)}</div>
                    </div>

                    {bill.trangThai === 0 ? (
                      <button
                        onClick={() => handlePayBill(bill.id)}
                        className="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
                      >
                        <FiCheckCircle /> Xác nhận thanh toán
                      </button>
                    ) : (
                      <span className="inline-flex items-center gap-1 text-xs font-bold text-emerald-600 bg-emerald-50 px-3 py-1.5 rounded-xl border border-emerald-100">
                        <FiCheckCircle /> Đã thanh toán {bill.ngayThanhToan ? `(${new Date(bill.ngayThanhToan).toLocaleDateString('vi-VN')})` : ''}
                      </span>
                    )}
                  </div>
                </div>

                {/* Breakdown Details */}
                <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-xs bg-slate-50 p-4 rounded-2xl border border-slate-100">
                  <div>
                    <span className="text-slate-400 block mb-1 font-semibold flex items-center gap-1">
                      <FiDollarSign /> Tiền thuê phòng:
                    </span>
                    <strong className="text-slate-800 text-sm">{formatPrice(bill.tienPhong)}</strong>
                  </div>

                  <div>
                    <span className="text-amber-600 block mb-1 font-semibold flex items-center gap-1">
                      <FiZap /> Điện: {bill.soDienCu} ➔ {bill.soDienMoi}
                    </span>
                    <strong className="text-slate-800 text-sm">
                      {bill.soDienTieuThu} kWh ({formatPrice(bill.tienDien)})
                    </strong>
                  </div>

                  <div>
                    <span className="text-blue-600 block mb-1 font-semibold flex items-center gap-1">
                      <FiDroplet /> Nước: {bill.soNuocCu} ➔ {bill.soNuocMoi}
                    </span>
                    <strong className="text-slate-800 text-sm">
                      {bill.soNuocTieuThu} m³ ({formatPrice(bill.tienNuoc)})
                    </strong>
                  </div>

                  <div>
                    <span className="text-slate-400 block mb-1 font-semibold">Phụ phí & Dịch vụ:</span>
                    <strong className="text-slate-800 text-sm">
                      {formatPrice(bill.chiPhiKhac)} {bill.ghiChuChiPhiKhac ? `(${bill.ghiChuChiPhiKhac})` : ''}
                    </strong>
                  </div>
                </div>

                {bill.hanThanhToan && (
                  <div className="text-right text-[11px] text-slate-400 mt-2">
                    Hạn thanh toán: <strong>{new Date(bill.hanThanhToan).toLocaleDateString('vi-VN')}</strong>
                  </div>
                )}
              </div>
            ))}
          </div>
        )
      ) : activeTab === 'contracts' ? (
        /* TAB: CONTRACTS */
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
                      <span className="text-xs font-bold uppercase tracking-wider text-[#0084ff]">Hợp đồng HD-{String(item.id).padStart(4, '0')}</span>
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

                  <div className="flex items-center gap-2 flex-wrap">
                    {/* PDF Download Button */}
                    <button
                      onClick={() => exportService.downloadContractPdf(item.id)}
                      className="inline-flex items-center gap-1.5 px-3.5 py-2 bg-emerald-50 hover:bg-emerald-100 text-emerald-700 text-xs font-bold rounded-xl border border-emerald-200 cursor-pointer transition-all"
                    >
                      <FiDownload /> Tải Hợp Đồng (PDF)
                    </button>

                    {!item.nguoiThueDaXacNhan ? (
                      <button
                        onClick={() => handleConfirmContract(item.id)}
                        className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
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
      ) : activeTab === 'requests' ? (
        /* TAB: REQUESTS & DEPOSITS */
        <div className="space-y-6">
          {deposits.length > 0 && (
            <div>
              <h3 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
                <FiDollarSign className="text-[#0084ff]" /> Các khoản tiền đặt cọc ({deposits.length})
              </h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {deposits.map((dep) => (
                  <div key={dep.id} className="bg-white rounded-3xl border border-slate-200/80 p-5 shadow-sm flex items-center justify-between gap-4">
                    <div>
                      <span className="text-xs text-slate-400 font-semibold block">Tiền đặt cọc #{dep.id}</span>
                      <div className="text-xl font-black text-slate-900 mt-0.5">{formatPrice(dep.soTien)}</div>
                      <span className={`inline-block px-2.5 py-0.5 rounded-full text-xs font-bold mt-2 ${dep.trangThai === 1 ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                        {dep.trangThai === 1 ? 'Đã thanh toán cọc' : 'Chờ thanh toán'}
                      </span>
                    </div>

                    {dep.trangThai === 0 ? (
                      <button
                        onClick={() => handlePayDepositVnPay(dep.id)}
                        className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
                      >
                        <FiCreditCard /> Thanh toán VNPay
                      </button>
                    ) : (
                      <div className="text-right text-xs text-emerald-600 font-semibold flex items-center gap-1">
                        <FiCheckCircle /> Đã thanh toán
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          )}

          <div>
            <h3 className="text-lg font-bold text-slate-900 mb-4 flex items-center gap-2">
              <FiCalendar className="text-[#0084ff]" /> Yêu cầu thuê phòng đã gửi ({requests.length})
            </h3>
            {requests.length === 0 ? (
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
            )}
          </div>
        </div>
      ) : activeTab === 'incidents' ? (
        /* TAB: INCIDENTS */
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
        /* TAB: REVIEWS */
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
