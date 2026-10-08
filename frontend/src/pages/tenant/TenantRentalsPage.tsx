import React, { useEffect, useState } from 'react';
import Modal from '../../components/common/Modal';
import {
  FiFileText,
  FiDollarSign,
  FiAlertTriangle,
  FiStar,
  FiCheckCircle,
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
import StatusBadge, { StatusTone } from '../../components/common/StatusBadge';
import PageState from '../../components/common/PageState';
import { CONTRACT_STATUS, DEPOSIT_STATUS, MONTHLY_BILL_STATUS, RENTAL_REQUEST_STATUS } from '../../utils/constants';
import { getApiErrorMessage } from '../../utils/apiError';

const requestStatus = (status: number): { label: string; tone: StatusTone } => ({
  0: { label: 'Đang chờ chủ trọ duyệt', tone: 'pending' }, 1: { label: 'Đã chấp nhận', tone: 'success' },
  2: { label: 'Đã từ chối', tone: 'danger' }, 3: { label: 'Đã hủy', tone: 'danger' }, 4: { label: 'Đã tạo hợp đồng', tone: 'info' },
  5: { label: 'Đã hết hạn đặt cọc', tone: 'danger' },
}[status] as { label: string; tone: StatusTone } || { label: 'Không xác định', tone: 'neutral' });
const depositStatus = (status: number): { label: string; tone: StatusTone } => ({
  0: { label: 'Chờ thanh toán', tone: 'pending' }, 1: { label: 'Đã thanh toán - chờ xác nhận', tone: 'info' },
  2: { label: 'Đã xác nhận', tone: 'success' }, 3: { label: 'Đang yêu cầu hoàn', tone: 'pending' },
  4: { label: 'Đã hoàn tiền', tone: 'neutral' }, 5: { label: 'Đã hủy', tone: 'danger' }, 6: { label: 'Đã hết hạn', tone: 'danger' },
}[status] as { label: string; tone: StatusTone } || { label: 'Không xác định', tone: 'neutral' });
const billStatus = (status: number): { label: string; tone: StatusTone } => ({
  0: { label: 'Chưa thanh toán', tone: 'pending' }, 1: { label: 'Đã thanh toán', tone: 'success' },
  2: { label: 'Đã hủy', tone: 'danger' }, 3: { label: 'Đang thanh toán', tone: 'info' },
  4: { label: 'Quá hạn', tone: 'danger' },
}[status] as { label: string; tone: StatusTone } || { label: 'Không xác định', tone: 'neutral' });

const TenantRentalsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'requests' | 'contracts' | 'bills' | 'incidents' | 'reviews'>('bills');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');

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

  async function loadData() {
    setLoading(true);
    setLoadError('');
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
    } catch (error) {
      setLoadError(getApiErrorMessage(error, 'Không thể tải dữ liệu thuê phòng.'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void Promise.resolve().then(loadData);
  }, []);

  const handleConfirmContract = async (contractId: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xác nhận đồng ý hợp đồng này?')) return;

    try {
      await rentalService.confirmContract(contractId);
      toast.success('Xác nhận hợp đồng thành công!');
      await loadData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể xác nhận hợp đồng.'));
    }
  };

  const handlePayBill = async (billId: number) => {
    try {
      const res = await paymentService.createVnPayUrl({
        monthlyBillId: billId,
        orderInfo: `Thanh toan hoa don thang ID ${billId}`,
      });
      if (res.data?.paymentUrl) window.location.assign(res.data.paymentUrl);
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể tạo liên kết thanh toán hóa đơn.'));
    }
  };

  const handleCreateIncident = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingIncident) return;
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
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể gửi báo cáo sự cố.'));
    } finally {
      setSubmittingIncident(false);
    }
  };

  const handleCreateReview = async (e: React.FormEvent) => {
    e.preventDefault();
    if (submittingReview) return;
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
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể gửi đánh giá.'));
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
        window.location.assign(res.data.paymentUrl);
      }
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể tạo liên kết thanh toán VNPay.'));
    }
  };

  const handleCancelRequest = async (requestId: number) => {
    if (!window.confirm('Bạn có chắc muốn hủy yêu cầu thuê phòng này?')) return;
    try { await rentalService.cancelRentalRequest(requestId); toast.success('Đã hủy yêu cầu thuê phòng'); await loadData(); }
    catch (error) { toast.error(getApiErrorMessage(error, 'Không thể hủy yêu cầu thuê phòng.')); }
  };

  if (loadError && !loading) return <div className="tenant-page max-w-6xl mx-auto px-4 py-8"><PageState type="error" message={loadError} onRetry={loadData} /></div>;

  return (
    <div className="tenant-page max-w-6xl mx-auto px-4 py-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-2 text-xs uppercase tracking-widest text-[#0084ff] font-bold mb-1">
          <FiFileText /> Quản lý thuê phòng
        </div>
        <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900">Hợp Đồng & Hóa Đơn Thuê Phòng</h1>
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
      <div className="rental-tabs grid grid-cols-2 sm:flex sm:items-center gap-2 border-b border-slate-200 mb-6 pb-2">
        <button
          onClick={() => setActiveTab('bills')}
          className={`px-3 py-2.5 rounded-lg font-semibold text-xs sm:text-sm transition-all sm:whitespace-nowrap flex items-center justify-center gap-2 cursor-pointer ${
            activeTab === 'bills'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          <FiZap /> Hóa đơn điện nước ({bills.length})
        </button>
        <button
          onClick={() => setActiveTab('contracts')}
          className={`px-3 py-2.5 rounded-lg font-semibold text-xs sm:text-sm transition-all sm:whitespace-nowrap flex items-center justify-center gap-2 cursor-pointer ${
            activeTab === 'contracts'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          <FiFileText /> Hợp đồng ({contracts.length})
        </button>
        <button
          onClick={() => setActiveTab('requests')}
          className={`px-3 py-2.5 rounded-lg font-semibold text-xs sm:text-sm transition-all sm:whitespace-nowrap flex items-center justify-center gap-2 cursor-pointer ${
            activeTab === 'requests'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Yêu cầu & Cọc ({requests.length})
        </button>
        <button
          onClick={() => setActiveTab('incidents')}
          className={`px-3 py-2.5 rounded-lg font-semibold text-xs sm:text-sm transition-all sm:whitespace-nowrap flex items-center justify-center gap-2 cursor-pointer ${
            activeTab === 'incidents'
              ? 'bg-[#0084ff] text-white shadow-md shadow-blue-500/20'
              : 'text-slate-600 hover:bg-slate-100'
          }`}
        >
          Sự cố phòng ({incidents.length})
        </button>
        <button
          onClick={() => setActiveTab('reviews')}
          className={`col-span-2 sm:col-span-1 px-3 py-2.5 rounded-lg font-semibold text-xs sm:text-sm transition-all sm:whitespace-nowrap flex items-center justify-center gap-2 cursor-pointer ${
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
        <PageState type="loading" message="Đang tải dữ liệu thuê phòng..." />
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
                      <div className="flex flex-wrap items-center gap-2">
                        <h3 className="text-lg font-bold text-slate-900">
                          Hóa đơn tiền phòng tháng {bill.thang}/{bill.nam}
                        </h3>
                        <StatusBadge {...billStatus(bill.trangThai)} />
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

                    {(bill.trangThai === MONTHLY_BILL_STATUS.UNPAID || bill.trangThai === MONTHLY_BILL_STATUS.OVERDUE) ? (
                      <button
                        onClick={() => handlePayBill(bill.id)}
                        className="inline-flex items-center gap-2 px-4 py-2.5 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
                      >
                        <FiCreditCard /> Thanh toán VNPay
                      </button>
                    ) : bill.trangThai === MONTHLY_BILL_STATUS.PAID ? (
                      <span className="inline-flex items-center gap-1 text-xs font-bold text-emerald-600 bg-emerald-50 px-3 py-1.5 rounded-xl border border-emerald-100">
                        <FiCheckCircle /> Đã thanh toán {bill.ngayThanhToan ? `(${new Date(bill.ngayThanhToan).toLocaleDateString('vi-VN')})` : ''}
                      </span>
                    ) : bill.trangThai === MONTHLY_BILL_STATUS.PENDING_PAYMENT ? (
                      <span className="text-xs font-semibold text-blue-700">Đang chờ VNPay xác nhận</span>
                    ) : null}
                  </div>
                </div>

                {/* Breakdown Details */}
                <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 text-xs bg-slate-50 p-4 rounded-2xl border border-slate-100">
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
                    <span className="text-slate-400 block mb-1 font-semibold">Phí dịch vụ:</span>
                    <strong className="text-slate-800 text-sm">{formatPrice(bill.phiDichVu || 0)}</strong>
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
                          item.trangThai === CONTRACT_STATUS.ACTIVE ? 'bg-emerald-100 text-emerald-700' : (item.trangThai >= CONTRACT_STATUS.TERMINATED ? 'bg-slate-100 text-slate-700' : 'bg-amber-100 text-amber-700')
                        }`}
                      >
                        {item.trangThai === CONTRACT_STATUS.ACTIVE ? 'Đang hiệu lực' : item.trangThai === CONTRACT_STATUS.TERMINATED ? 'Đã chấm dứt' : item.trangThai === CONTRACT_STATUS.EXPIRED ? 'Đã hết hạn' : item.trangThai === CONTRACT_STATUS.PENDING_START ? 'Chờ ngày bắt đầu' : item.trangThai === CONTRACT_STATUS.CANCELLED ? 'Đã hủy' : 'Chờ 2 bên xác nhận'}
                      </span>
                    </div>
                    <h3 className="text-lg font-bold text-slate-900 mt-1 mb-0.5">
                      {item.tenPhong || `Phòng hợp đồng #${item.id}`}
                    </h3>
                    {item.diaChiPhong && (
                      <p className="text-xs text-slate-500 mb-1">{item.diaChiPhong}</p>
                    )}
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

                    {item.trangThai === CONTRACT_STATUS.PENDING_SIGNATURE && !item.nguoiThueDaXacNhan ? (
                      <button
                        onClick={() => handleConfirmContract(item.id)}
                        className="inline-flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
                      >
                        <FiCheck /> Xác nhận hợp đồng
                      </button>
                    ) : item.nguoiThueDaXacNhan ? (
                      <span className="inline-flex items-center gap-1.5 text-xs text-emerald-600 font-bold bg-emerald-50 px-3 py-1.5 rounded-xl border border-emerald-100">
                        <FiCheckCircle /> Bạn đã xác nhận
                      </span>
                    ) : null}

                    {item.trangThai === CONTRACT_STATUS.ACTIVE && (
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
                      <div className="mt-2"><StatusBadge label={depositStatus(dep.trangThai).label} tone={depositStatus(dep.trangThai).tone} /></div>
                      {dep.hanThanhToan && dep.trangThai === DEPOSIT_STATUS.PENDING && <p className="mt-2 text-xs text-amber-700">Hạn thanh toán: {new Date(dep.hanThanhToan).toLocaleString('vi-VN')}</p>}
                    </div>

                    {dep.trangThai === DEPOSIT_STATUS.PENDING ? (
                      <button
                        onClick={() => handlePayDepositVnPay(dep.id)}
                        className="inline-flex items-center gap-2 px-4 py-2.5 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white text-xs font-bold rounded-xl shadow-md cursor-pointer transition-all border-none"
                      >
                        <FiCreditCard /> Thanh toán VNPay
                      </button>
                    ) : (
                      <div className="text-right text-xs text-emerald-600 font-semibold flex items-center gap-1">
                        <FiCheckCircle /> {dep.trangThai === DEPOSIT_STATUS.CONFIRMED ? 'Đã xác nhận' : 'Đang chờ chủ trọ xác nhận'}
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
                        <StatusBadge label={requestStatus(item.trangThai).label} tone={requestStatus(item.trangThai).tone} />
                      </div>

                      <div className="text-sm text-slate-500 space-y-1">
                        <p className="flex items-center gap-2">
                          <FiCalendar size={14} /> Ngày gửi: {new Date(item.ngayTao).toLocaleDateString('vi-VN')}
                        </p>
                        {item.ghiChu && <p className="text-slate-700 bg-slate-50 p-2.5 rounded-xl border border-slate-100 text-xs">Ghi chú: {item.ghiChu}</p>}
                        {item.lyDoHuy && <p className="text-sm text-rose-700">{item.lyDoHuy}</p>}
                      </div>
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {item.trangThai === RENTAL_REQUEST_STATUS.PENDING && <button onClick={() => handleCancelRequest(item.id)} className="rounded-md border border-rose-200 px-3 py-2 text-xs font-semibold text-rose-700 hover:bg-rose-50">Hủy yêu cầu</button>}
                      {item.trangThai === RENTAL_REQUEST_STATUS.APPROVED && !deposits.some(d => d.yeuCauThueId === item.id) && <p className="text-xs font-medium text-amber-700">Chủ trọ đang thiết lập khoản đặt cọc cho yêu cầu này.</p>}
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
        <Modal isOpen={incidentModalOpen} onClose={() => { if (!submittingIncident) setIncidentModalOpen(false); }} title="Báo cáo sự cố phòng trọ">
            <form onSubmit={handleCreateIncident} className="space-y-4">
              <div>
                <label htmlFor="field-incidentTitle" className="block text-xs font-bold text-slate-700 mb-1">Tiêu đề sự cố *</label>
                <input id="field-incidentTitle"
                  type="text"
                  placeholder="Ví dụ: Hỏng vòi nước, chập bóng đèn..."
                  value={incidentTitle}
                  onChange={(e) => setIncidentTitle(e.target.value)}
                  required
                  className="w-full px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-[#0084ff]"
                />
              </div>
              <div>
                <label htmlFor="field-incidentDesc" className="block text-xs font-bold text-slate-700 mb-1">Mô tả chi tiết *</label>
                <textarea id="field-incidentDesc"
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
        </Modal>
      )}

      {/* Modal Đánh Giá Phòng */}
      {reviewModalOpen && (
        <Modal isOpen={reviewModalOpen} onClose={() => { if (!submittingReview) setReviewModalOpen(false); }} title="Đánh giá trải nghiệm phòng trọ">
            <form onSubmit={handleCreateReview} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-slate-700 mb-2">Số sao đánh giá *</label>
                <div className="flex items-center gap-2 text-2xl text-amber-400">
                  {[1, 2, 3, 4, 5].map((s) => (
                    <button
                      key={s}
                      type="button"
                      aria-label={`Đánh giá ${s} sao`}
                      aria-pressed={reviewRating === s}
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
                <label htmlFor="field-reviewComment" className="block text-xs font-bold text-slate-700 mb-1">Nhận xét chi tiết</label>
                <textarea id="field-reviewComment"
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
        </Modal>
      )}
    </div>
  );
};

export default TenantRentalsPage;
