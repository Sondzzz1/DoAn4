import React, { useEffect, useState } from 'react';
import {
  FiFileText,
  FiZap,
  FiDroplet,
  FiDollarSign,
  FiDownload,
  FiPlus,
  FiCheckCircle,
  FiClock,
  FiEdit2,
  FiTrash2,
  FiFilter,
  FiUsers,
  FiHome,
  FiCalendar,
  FiCheck,
  FiAlertCircle,
} from 'react-icons/fi';
import { toast } from 'react-toastify';
import {
  monthlyBillService,
  MonthlyBillDto,
  CreateMonthlyBillDto,
  UpdateMonthlyBillDto,
} from '../../services/monthlyBillService';
import {
  rentalService,
  ContractDto,
  RentalRequestDto,
  DepositDto,
} from '../../services/rentalService';
import { exportService } from '../../services/exportService';
import { formatPrice } from '../../utils/helpers';
import './LandlordContractsPage.css';

const LandlordContractsPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState<'bills' | 'contracts' | 'requests'>('bills');
  const [loading, setLoading] = useState(true);

  // Data states
  const [bills, setBills] = useState<MonthlyBillDto[]>([]);
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [requests, setRequests] = useState<RentalRequestDto[]>([]);
  const [deposits, setDeposits] = useState<DepositDto[]>([]);

  // Filter states for Bills
  const currentYear = new Date().getFullYear();
  const currentMonth = new Date().getMonth() + 1;
  const [filterMonth, setFilterMonth] = useState<number | 'all'>('all');
  const [filterYear, setFilterYear] = useState<number>(currentYear);
  const [filterStatus, setFilterStatus] = useState<number | 'all'>('all');

  // Modal Create Bill State
  const [billModalOpen, setBillModalOpen] = useState(false);
  const [submittingBill, setSubmittingBill] = useState(false);
  const [selectedContractId, setSelectedContractId] = useState<number | ''>('');
  const [billMonth, setBillMonth] = useState<number>(currentMonth);
  const [billYear, setBillYear] = useState<number>(currentYear);
  const [oldElec, setOldElec] = useState<number>(0);
  const [newElec, setNewElec] = useState<number>(0);
  const [priceElec, setPriceElec] = useState<number>(3500);
  const [oldWater, setOldWater] = useState<number>(0);
  const [newWater, setNewWater] = useState<number>(0);
  const [priceWater, setPriceWater] = useState<number>(20000);
  const [roomPrice, setRoomPrice] = useState<number>(0);
  const [otherFees, setOtherFees] = useState<number>(0);
  const [otherFeesNote, setOtherFeesNote] = useState<string>('Wifi, rác, dịch vụ');
  const [billDueDate, setBillDueDate] = useState<string>(
    new Date(Date.now() + 7 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
  );
  const [billNote, setBillNote] = useState<string>('');

  // Modal Edit Bill State
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [editingBill, setEditingBill] = useState<MonthlyBillDto | null>(null);
  const [editOldElec, setEditOldElec] = useState<number>(0);
  const [editNewElec, setEditNewElec] = useState<number>(0);
  const [editPriceElec, setEditPriceElec] = useState<number>(3500);
  const [editOldWater, setEditOldWater] = useState<number>(0);
  const [editNewWater, setEditNewWater] = useState<number>(0);
  const [editPriceWater, setEditPriceWater] = useState<number>(20000);
  const [editRoomPrice, setEditRoomPrice] = useState<number>(0);
  const [editOtherFees, setEditOtherFees] = useState<number>(0);
  const [editOtherFeesNote, setEditOtherFeesNote] = useState<string>('');
  const [editStatus, setEditStatus] = useState<number>(0);

  // Modal Create Contract State
  const [contractModalOpen, setContractModalOpen] = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null);
  const [contractStartDate, setContractStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [contractEndDate, setContractEndDate] = useState(
    new Date(Date.now() + 180 * 24 * 60 * 60 * 1000).toISOString().slice(0, 10)
  );
  const [contractRent, setContractRent] = useState<number>(3000000);
  const [submittingContract, setSubmittingContract] = useState(false);

  // Load all data
  const loadData = async () => {
    setLoading(true);
    try {
      const [billsRes, contractsRes, requestsRes, depositsRes] = await Promise.all([
        monthlyBillService.getMyBills(true),
        rentalService.getMyContracts(),
        rentalService.getMyRentalRequests(),
        rentalService.getMyDeposits(),
      ]);

      setBills(billsRes.data || []);
      setContracts(contractsRes.data || []);
      setRequests(requestsRes.data || []);
      setDeposits(depositsRes.data || []);
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tải dữ liệu.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void loadData();
  }, []);

  // When selected contract changes in create bill modal, autofill room price
  useEffect(() => {
    if (selectedContractId) {
      const found = contracts.find((c) => c.id === Number(selectedContractId));
      if (found) {
        setRoomPrice(found.tienThueHangThang);
      }
    }
  }, [selectedContractId, contracts]);

  // Real-time calculation for Create Modal
  const calculatedElecUsed = Math.max(0, newElec - oldElec);
  const calculatedElecAmount = calculatedElecUsed * priceElec;
  const calculatedWaterUsed = Math.max(0, newWater - oldWater);
  const calculatedWaterAmount = calculatedWaterUsed * priceWater;
  const calculatedTotalAmount = roomPrice + calculatedElecAmount + calculatedWaterAmount + otherFees;

  // Real-time calculation for Edit Modal
  const editCalculatedElecUsed = Math.max(0, editNewElec - editOldElec);
  const editCalculatedElecAmount = editCalculatedElecUsed * editPriceElec;
  const editCalculatedWaterUsed = Math.max(0, editNewWater - editOldWater);
  const editCalculatedWaterAmount = editCalculatedWaterUsed * editPriceWater;
  const editCalculatedTotalAmount = editRoomPrice + editCalculatedElecAmount + editCalculatedWaterAmount + editOtherFees;

  // Handle Create Bill
  const handleCreateBill = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedContractId) {
      toast.warning('Vui lòng chọn hợp đồng thuê.');
      return;
    }
    if (newElec < oldElec) {
      toast.warning('Số điện mới không thể nhỏ hơn số điện cũ.');
      return;
    }
    if (newWater < oldWater) {
      toast.warning('Số nước mới không thể nhỏ hơn số nước cũ.');
      return;
    }

    setSubmittingBill(true);
    try {
      await monthlyBillService.createBill({
        hopDongId: Number(selectedContractId),
        thang: billMonth,
        nam: billYear,
        soDienCu: oldElec,
        soDienMoi: newElec,
        giaDien: priceElec,
        soNuocCu: oldWater,
        soNuocMoi: newWater,
        giaNuoc: priceWater,
        tienPhong: roomPrice,
        chiPhiKhac: otherFees,
        ghiChuChiPhiKhac: otherFeesNote,
        hanThanhToan: billDueDate ? new Date(billDueDate).toISOString() : undefined,
        ghiChu: billNote || undefined,
      });

      toast.success(`Tạo hóa đơn tháng ${billMonth}/${billYear} thành công!`);
      setBillModalOpen(false);
      resetBillForm();
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể tạo hóa đơn.');
    } finally {
      setSubmittingBill(false);
    }
  };

  const resetBillForm = () => {
    setSelectedContractId('');
    setOldElec(0);
    setNewElec(0);
    setOldWater(0);
    setNewWater(0);
    setOtherFees(0);
    setBillNote('');
  };

  // Handle Edit Bill
  const handleOpenEditModal = (bill: MonthlyBillDto) => {
    setEditingBill(bill);
    setEditOldElec(bill.soDienCu);
    setEditNewElec(bill.soDienMoi);
    setEditPriceElec(bill.giaDien);
    setEditOldWater(bill.soNuocCu);
    setEditNewWater(bill.soNuocMoi);
    setEditPriceWater(bill.giaNuoc);
    setEditRoomPrice(bill.tienPhong);
    setEditOtherFees(bill.chiPhiKhac);
    setEditOtherFeesNote(bill.ghiChuChiPhiKhac || '');
    setEditStatus(bill.trangThai);
    setEditModalOpen(true);
  };

  const handleUpdateBill = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!editingBill) return;

    try {
      await monthlyBillService.updateBill(editingBill.id, {
        soDienCu: editOldElec,
        soDienMoi: editNewElec,
        giaDien: editPriceElec,
        soNuocCu: editOldWater,
        soNuocMoi: editNewWater,
        giaNuoc: editPriceWater,
        tienPhong: editRoomPrice,
        chiPhiKhac: editOtherFees,
        ghiChuChiPhiKhac: editOtherFeesNote,
        trangThai: editStatus,
      });

      toast.success('Cập nhật hóa đơn thành công!');
      setEditModalOpen(false);
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể cập nhật hóa đơn.');
    }
  };

  // Handle Mark Bill as Paid
  const handleConfirmBillPaid = async (billId: number) => {
    if (!window.confirm('Xác nhận hóa đơn này đã được khách thuê thanh toán đầy đủ?')) return;
    try {
      await monthlyBillService.payBill(billId, {
        phuongThucThanhToan: 'Chủ trọ xác nhận đã nhận tiền',
      });
      toast.success('Đã ghi nhận thanh toán hóa đơn thành công!');
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể cập nhật trạng thái.');
    }
  };

  // Handle Delete Bill
  const handleDeleteBill = async (billId: number) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa hóa đơn này?')) return;
    try {
      await monthlyBillService.deleteBill(billId);
      toast.success('Xóa hóa đơn thành công!');
      await loadData();
    } catch (error: any) {
      toast.error(error?.response?.data?.message || 'Không thể xóa hóa đơn.');
    }
  };

  // Handle Create Contract from Request
  const handleOpenCreateContract = (req: RentalRequestDto) => {
    setSelectedRequestId(req.id);
    setContractRent(3000000);
    setContractModalOpen(true);
  };

  const handleCreateContract = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!selectedRequestId) return;

    setSubmittingContract(true);
    try {
      const response = await fetch('/api/hop-dong', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${localStorage.getItem('token')}`,
        },
        body: JSON.stringify({
          yeuCauThueId: selectedRequestId,
          ngayBatDau: new Date(contractStartDate).toISOString(),
          ngayKetThuc: new Date(contractEndDate).toISOString(),
          tienThueHangThang: contractRent,
        }),
      });
      const data = await response.json();
      if (!response.ok) throw new Error(data?.message || 'Lỗi khi tạo hợp đồng');

      toast.success('Tạo hợp đồng thuê phòng thành công!');
      setContractModalOpen(false);
      await loadData();
    } catch (error: any) {
      toast.error(error.message || 'Không thể tạo hợp đồng.');
    } finally {
      setSubmittingContract(false);
    }
  };

  // Filter bills
  const filteredBills = bills.filter((b) => {
    if (filterYear && b.nam !== filterYear) return false;
    if (filterMonth !== 'all' && b.thang !== filterMonth) return false;
    if (filterStatus !== 'all' && b.trangThai !== filterStatus) return false;
    return true;
  });

  // Calculate statistics
  const totalRevenuePaid = bills
    .filter((b) => b.trangThai === 1)
    .reduce((acc, curr) => acc + curr.tongTien, 0);
  const totalRevenuePending = bills
    .filter((b) => b.trangThai === 0)
    .reduce((acc, curr) => acc + curr.tongTien, 0);

  return (
    <div className="landlord-contracts-page">
      <div className="landlord-contracts-container">
        {/* Top Header */}
        <div className="contracts-header">
          <div className="contracts-header-content">
            <div className="contracts-header-info">
              <div className="contracts-header-badge">
                <FiZap /> Quản lý PMS & Điện Nước
              </div>
              <h1>Quản Lý Hợp Đồng & Chỉ Số Điện Nước</h1>
              <p>
                Tính toán tự động tiền điện, tiền nước, phụ phí và quản lý hợp đồng pháp lý cho chủ trọ.
              </p>
            </div>

            <div className="contracts-header-actions">
              <button
                onClick={() => exportService.downloadRevenueExcel(filterYear, filterMonth === 'all' ? undefined : filterMonth)}
                className="btn-success"
              >
                <FiDownload /> Xuất Báo Cáo Excel
              </button>
              <button
                onClick={() => {
                  resetBillForm();
                  setBillModalOpen(true);
                }}
                className="btn-primary"
              >
                <FiPlus /> Nhập Số Điện Nước / Tạo Hóa Đơn
              </button>
            </div>
          </div>
        </div>

        {/* KPI Stats Cards */}
        <div className="kpi-stats-grid">
          <div className="kpi-card">
            <div className="kpi-card-header">
              <span className="kpi-card-label">Hợp đồng thuê</span>
              <FiFileText className="kpi-card-icon blue" />
            </div>
            <div className="kpi-card-value">{contracts.length}</div>
            <span className="kpi-card-subtitle">
              {contracts.filter((c) => c.trangThai === 1).length} đang hiệu lực
            </span>
          </div>

          <div className="kpi-card">
            <div className="kpi-card-header">
              <span className="kpi-card-label">Hóa đơn đã thu</span>
              <FiCheckCircle className="kpi-card-icon emerald" />
            </div>
            <div className="kpi-card-value emerald">{formatPrice(totalRevenuePaid)}</div>
            <span className="kpi-card-subtitle">
              {bills.filter((b) => b.trangThai === 1).length} hóa đơn đã xong
            </span>
          </div>

          <div className="kpi-card">
            <div className="kpi-card-header">
              <span className="kpi-card-label">Chờ thanh toán</span>
              <FiClock className="kpi-card-icon amber" />
            </div>
            <div className="kpi-card-value amber">{formatPrice(totalRevenuePending)}</div>
            <span className="kpi-card-subtitle">
              {bills.filter((b) => b.trangThai === 0).length} hóa đơn chưa thu
            </span>
          </div>

          <div className="kpi-card">
            <div className="kpi-card-header">
              <span className="kpi-card-label">Tổng hóa đơn</span>
              <FiZap className="kpi-card-icon purple" />
            </div>
            <div className="kpi-card-value">{bills.length}</div>
            <span className="kpi-card-subtitle">Toàn bộ các kỳ</span>
          </div>
        </div>

        {/* Tabs Switcher */}
        <div className="contracts-tabs">
          <button
            onClick={() => setActiveTab('bills')}
            className={`tab-button ${activeTab === 'bills' ? 'active' : ''}`}
          >
            <FiZap /> Hóa Đơn Điện Nước & Tiền Phòng ({bills.length})
          </button>
          <button
            onClick={() => setActiveTab('contracts')}
            className={`tab-button ${activeTab === 'contracts' ? 'active' : ''}`}
          >
            <FiFileText /> Danh Sách Hợp Đồng ({contracts.length})
          </button>
          <button
            onClick={() => setActiveTab('requests')}
            className={`tab-button ${activeTab === 'requests' ? 'active' : ''}`}
          >
            <FiUsers /> Yêu Cầu Thuê Phòng ({requests.length})
          </button>
        </div>

      {/* Tab 1: BILLS & UTILITY METERS */}
      {activeTab === 'bills' && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="filter-bar">
            <div className="filter-bar-content">
              <div className="filter-controls">
                <span className="filter-label">
                  <FiFilter /> Lọc:
                </span>
                <div className="filter-group">
                  <label className="filter-group-label">Năm</label>
                  <select
                    value={filterYear}
                    onChange={(e) => setFilterYear(Number(e.target.value))}
                    className="filter-select"
                  >
                    {[currentYear, currentYear - 1, currentYear - 2].map((y) => (
                      <option key={y} value={y}>
                        Năm {y}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="filter-group">
                  <label className="filter-group-label">Tháng</label>
                  <select
                    value={filterMonth}
                    onChange={(e) => setFilterMonth(e.target.value === 'all' ? 'all' : Number(e.target.value))}
                    className="filter-select"
                  >
                    <option value="all">Tất cả các tháng</option>
                    {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((m) => (
                      <option key={m} value={m}>
                        Tháng {m}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="filter-group">
                  <label className="filter-group-label">Trạng thái</label>
                  <select
                    value={filterStatus}
                    onChange={(e) => setFilterStatus(e.target.value === 'all' ? 'all' : Number(e.target.value))}
                    className="filter-select"
                  >
                    <option value="all">Tất cả trạng thái</option>
                    <option value={0}>Chờ thanh toán</option>
                    <option value={1}>Đã thanh toán</option>
                  </select>
                </div>
              </div>

              <div className="filter-result-text">
                Hiển thị <strong>{filteredBills.length}</strong> hóa đơn
              </div>
            </div>
          </div>

          {/* Bills List / Table */}
          {loading ? (
            <div className="loading-state">
              Đang tải danh sách hóa đơn...
            </div>
          ) : filteredBills.length === 0 ? (
            <div className="empty-state">
              <FiZap className="empty-state-icon" />
              <h3>Chưa có hóa đơn nào phù hợp với bộ lọc.</h3>
              <p>Bấm "Nhập Số Điện Nước / Tạo Hóa Đơn" để lập hóa đơn mới.</p>
            </div>
          ) : (
            <div className="bills-list">
              {filteredBills.map((bill) => (
                <div key={bill.id} className="bill-card">
                  <div className="bill-card-header">
                    <div className="bill-card-info">
                      <div className="bill-month-badge">
                        T{bill.thang}
                      </div>
                      <div className="bill-details">
                        <div className="bill-title">
                          {bill.tenPhong || `Hợp đồng #${bill.hopDongId}`}
                          <span className={`bill-status-badge ${bill.trangThai === 1 ? 'paid' : 'pending'}`}>
                            {bill.trangThai === 1 ? 'Đã thanh toán' : 'Chờ thanh toán'}
                          </span>
                        </div>
                        <span className="bill-meta">
                          Khách thuê: <strong>{bill.tenNguoiThue}</strong> {bill.sdtNguoiThue ? `(${bill.sdtNguoiThue})` : ''} • Kỳ: Tháng {bill.thang}/{bill.nam}
                        </span>
                      </div>
                    </div>

                    <div className="bill-card-summary">
                      <span className="bill-total-label">Tổng tiền</span>
                      <div className="bill-total-amount">{formatPrice(bill.tongTien)}</div>
                    </div>
                  </div>

                  <div className="bill-card-actions">
                    {bill.trangThai === 0 && (
                      <button
                        onClick={() => handleConfirmBillPaid(bill.id)}
                        className="btn-icon success"
                        title="Xác nhận đã nhận tiền"
                      >
                        <FiCheck /> Đã thu
                      </button>
                    )}
                    <button
                      onClick={() => handleOpenEditModal(bill)}
                      className="btn-icon edit"
                      title="Chỉnh sửa chỉ số"
                    >
                      <FiEdit2 /> Sửa
                    </button>
                    <button
                      onClick={() => handleDeleteBill(bill.id)}
                      className="btn-icon delete"
                      title="Xóa hóa đơn"
                    >
                      <FiTrash2 /> Xóa
                    </button>
                  </div>

                  {/* Meter Breakdown Details */}
                  <div className="meter-breakdown">
                    <div className="meter-item">
                      <span className="meter-label">
                        <FiHome /> Tiền phòng:
                      </span>
                      <strong className="meter-value">{formatPrice(bill.tienPhong)}</strong>
                    </div>

                    <div className="meter-item">
                      <span className="meter-label" style={{ color: '#f59e0b' }}>
                        <FiZap /> Điện ({bill.soDienCu} ➔ {bill.soDienMoi}):
                      </span>
                      <strong className="meter-value">
                        {bill.soDienTieuThu} kWh × {formatPrice(bill.giaDien)} = {formatPrice(bill.tienDien)}
                      </strong>
                    </div>

                    <div className="meter-item">
                      <span className="meter-label" style={{ color: '#0ea5e9' }}>
                        <FiDroplet /> Nước ({bill.soNuocCu} ➔ {bill.soNuocMoi}):
                      </span>
                      <strong className="meter-value">
                        {bill.soNuocTieuThu} m³ × {formatPrice(bill.giaNuoc)} = {formatPrice(bill.tienNuoc)}
                      </strong>
                    </div>

                    <div className="meter-item">
                      <span className="meter-label">Phụ phí & Dịch vụ:</span>
                      <strong className="meter-value">
                        {formatPrice(bill.chiPhiKhac)} {bill.ghiChuChiPhiKhac ? `(${bill.ghiChuChiPhiKhac})` : ''}
                      </strong>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Tab 2: CONTRACTS & PDF EXPORT */}
      {activeTab === 'contracts' && (
        <div className="space-y-4">
          {contracts.length === 0 ? (
            <div className="empty-state">
              Chưa có hợp đồng nào được tạo.
            </div>
          ) : (
            <div className="contracts-grid">
              {contracts.map((con) => (
                <div key={con.id} className="contract-card">
                  <div>
                    <div className="contract-card-header">
                      <span className="contract-id">
                        Hợp đồng HD-{String(con.id).padStart(4, '0')}
                      </span>
                      <span
                        className={`bill-status-badge ${
                          con.trangThai === 1 ? 'paid' : 'pending'
                        }`}
                      >
                        {con.trangThai === 1 ? 'Đang hiệu lực' : 'Chờ xác nhận'}
                      </span>
                    </div>

                    <div className="contract-rent">
                      {formatPrice(con.tienThueHangThang)} <span>/ tháng</span>
                    </div>

                    <div className="contract-details">
                      <div>
                        Thời hạn: <strong>{new Date(con.ngayBatDau).toLocaleDateString('vi-VN')}</strong> đến{' '}
                        <strong>{new Date(con.ngayKetThuc).toLocaleDateString('vi-VN')}</strong>
                      </div>
                      <div>
                        Chủ trọ ký: {con.chuTroDaXacNhan ? '✅ Đã ký điện tử' : '⏳ Chưa xác nhận'}
                      </div>
                      <div>
                        Khách thuê ký: {con.nguoiThueDaXacNhan ? '✅ Đã ký điện tử' : '⏳ Chưa xác nhận'}
                      </div>
                    </div>
                  </div>

                  <div className="contract-card-actions">
                    <button onClick={() => exportService.downloadContractPdf(con.id)}>
                      <FiDownload /> Tải Hợp Đồng PDF
                    </button>
                    <button
                      onClick={() => {
                        setSelectedContractId(con.id);
                        setRoomPrice(con.tienThueHangThang);
                        setBillModalOpen(true);
                      }}
                    >
                      <FiZap /> Lập Hóa Đơn
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Tab 3: RENTAL REQUESTS */}
      {activeTab === 'requests' && (
        <div className="space-y-4">
          {requests.length === 0 ? (
            <div className="empty-state">
              Chưa có yêu cầu thuê phòng nào.
            </div>
          ) : (
            <div className="empty-state">
              <FiUsers className="empty-state-icon" />
              <h3>Tính năng đang được phát triển</h3>
              <p>Quản lý yêu cầu thuê phòng sẽ có sớm.</p>
            </div>
          )}
        </div>
      )}

      {/* MODAL 1: CREATE BILL & CALCULATE UTILITIES */}
      {billModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-2xl w-full p-6 shadow-2xl animate-in fade-in zoom-in-95 max-h-[90vh] overflow-y-auto">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3 mb-4">
              <div>
                <h3 className="text-lg font-black text-slate-900 flex items-center gap-2">
                  <FiZap className="text-amber-500" /> Nhập Chỉ Số Điện, Nước & Tính Hóa Đơn Hàng Tháng
                </h3>
                <p className="text-xs text-slate-500 mt-0.5">
                  Hệ thống tự động tính: (Số mới - Số cũ) × Đơn giá + Tiền phòng + Phụ phí = Tổng tiền.
                </p>
              </div>
              <button
                type="button"
                onClick={() => setBillModalOpen(false)}
                className="w-8 h-8 rounded-full bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-500 cursor-pointer border-none text-sm"
              >
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateBill} className="space-y-4 text-xs">
              {/* Row 1: Contract & Month/Year */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="sm:col-span-1">
                  <label className="block font-bold text-slate-700 mb-1">Chọn Hợp Đồng *</label>
                  <select
                    value={selectedContractId}
                    onChange={(e) => setSelectedContractId(e.target.value ? Number(e.target.value) : '')}
                    required
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs focus:border-[#0084ff] bg-slate-50 font-semibold"
                  >
                    <option value="">-- Chọn hợp đồng --</option>
                    {contracts.map((c) => (
                      <option key={c.id} value={c.id}>
                        HD-{String(c.id).padStart(4, '0')} ({formatPrice(c.tienThueHangThang)}/tháng)
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-bold text-slate-700 mb-1">Tháng *</label>
                  <select
                    value={billMonth}
                    onChange={(e) => setBillMonth(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs focus:border-[#0084ff]"
                  >
                    {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((m) => (
                      <option key={m} value={m}>
                        Tháng {m}
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label className="block font-bold text-slate-700 mb-1">Năm *</label>
                  <input
                    type="number"
                    value={billYear}
                    onChange={(e) => setBillYear(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs focus:border-[#0084ff]"
                  />
                </div>
              </div>

              {/* ELECTRICITY SECTION */}
              <div className="bg-amber-50/60 p-3.5 rounded-2xl border border-amber-200/60 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-amber-800 flex items-center gap-1.5">
                    <FiZap /> CHỈ SỐ ĐIỆN (kWh)
                  </span>
                  <span className="text-amber-900 font-extrabold text-[11px]">
                    Tiêu thụ: {calculatedElecUsed} kWh = {formatPrice(calculatedElecAmount)}
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-2">
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số điện cũ</label>
                    <input
                      type="number"
                      step="any"
                      value={oldElec}
                      onChange={(e) => setOldElec(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số điện mới</label>
                    <input
                      type="number"
                      step="any"
                      value={newElec}
                      onChange={(e) => setNewElec(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white font-bold text-amber-700"
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Đơn giá (VNĐ/kWh)</label>
                    <input
                      type="number"
                      value={priceElec}
                      onChange={(e) => setPriceElec(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                </div>
              </div>

              {/* WATER SECTION */}
              <div className="bg-blue-50/60 p-3.5 rounded-2xl border border-blue-200/60 space-y-2">
                <div className="flex items-center justify-between">
                  <span className="font-bold text-blue-800 flex items-center gap-1.5">
                    <FiDroplet /> CHỈ SỐ NƯỚC (m³)
                  </span>
                  <span className="text-blue-900 font-extrabold text-[11px]">
                    Tiêu thụ: {calculatedWaterUsed} m³ = {formatPrice(calculatedWaterAmount)}
                  </span>
                </div>
                <div className="grid grid-cols-3 gap-2">
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số nước cũ</label>
                    <input
                      type="number"
                      step="any"
                      value={oldWater}
                      onChange={(e) => setOldWater(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số nước mới</label>
                    <input
                      type="number"
                      step="any"
                      value={newWater}
                      onChange={(e) => setNewWater(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white font-bold text-blue-700"
                    />
                  </div>
                  <div>
                    <label className="block text-[10px] font-semibold text-slate-600 mb-0.5">Đơn giá (VNĐ/m³)</label>
                    <input
                      type="number"
                      value={priceWater}
                      onChange={(e) => setPriceWater(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                </div>
              </div>

              {/* ROOM PRICE & OTHER FEES */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold text-slate-700 mb-1">Tiền Phòng (VNĐ) *</label>
                  <input
                    type="number"
                    value={roomPrice}
                    onChange={(e) => setRoomPrice(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs font-bold text-slate-800"
                    required
                  />
                </div>

                <div>
                  <label className="block font-bold text-slate-700 mb-1">Phụ phí / Dịch vụ (VNĐ)</label>
                  <input
                    type="number"
                    value={otherFees}
                    onChange={(e) => setOtherFees(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label className="block font-bold text-slate-700 mb-1">Ghi chú phụ phí</label>
                  <input
                    type="text"
                    value={otherFeesNote}
                    onChange={(e) => setOtherFeesNote(e.target.value)}
                    placeholder="Ví dụ: Tiền rác, tiền vệ sinh, wifi..."
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  />
                </div>
                <div>
                  <label className="block font-bold text-slate-700 mb-1">Hạn đóng tiền</label>
                  <input
                    type="date"
                    value={billDueDate}
                    onChange={(e) => setBillDueDate(e.target.value)}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  />
                </div>
              </div>

              {/* REAL-TIME TOTAL SUMMARY BANNER */}
              <div className="bg-gradient-to-r from-blue-600 to-indigo-600 p-4 rounded-2xl text-white flex items-center justify-between shadow-lg shadow-blue-500/20">
                <div>
                  <span className="text-xs uppercase font-bold text-blue-100 tracking-wider">Tổng tiền hóa đơn</span>
                  <div className="text-2xl font-black">{formatPrice(calculatedTotalAmount)}</div>
                </div>
                <div className="text-right text-[11px] text-blue-100 space-y-0.5">
                  <div>Phòng: {formatPrice(roomPrice)}</div>
                  <div>Điện ({calculatedElecUsed} kWh): {formatPrice(calculatedElecAmount)}</div>
                  <div>Nước ({calculatedWaterUsed} m³): {formatPrice(calculatedWaterAmount)}</div>
                  {otherFees > 0 && <div>Phụ phí: {formatPrice(otherFees)}</div>}
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setBillModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-slate-600 hover:bg-slate-100 font-semibold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submittingBill}
                  className="px-6 py-2 rounded-xl font-bold bg-[#0084ff] hover:bg-[#0073e6] text-white shadow-md cursor-pointer"
                >
                  {submittingBill ? 'Đang tạo...' : 'Lập & Gửi Hóa Đơn'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL 2: EDIT BILL */}
      {editModalOpen && editingBill && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-xl w-full p-6 shadow-2xl animate-in fade-in zoom-in-95 max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-black text-slate-900 mb-3 flex items-center gap-2">
              <FiEdit2 /> Cập Nhật Chỉ Số Hóa Đơn #{editingBill.id} (Tháng {editingBill.thang}/{editingBill.nam})
            </h3>
            <form onSubmit={handleUpdateBill} className="space-y-3 text-xs">
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Số điện cũ</label>
                  <input
                    type="number"
                    value={editOldElec}
                    onChange={(e) => setEditOldElec(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Số điện mới</label>
                  <input
                    type="number"
                    value={editNewElec}
                    onChange={(e) => setEditNewElec(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 font-bold text-amber-600"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Đơn giá điện</label>
                  <input
                    type="number"
                    value={editPriceElec}
                    onChange={(e) => setEditPriceElec(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Số nước cũ</label>
                  <input
                    type="number"
                    value={editOldWater}
                    onChange={(e) => setEditOldWater(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Số nước mới</label>
                  <input
                    type="number"
                    value={editNewWater}
                    onChange={(e) => setEditNewWater(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 font-bold text-blue-600"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Đơn giá nước</label>
                  <input
                    type="number"
                    value={editPriceWater}
                    onChange={(e) => setEditPriceWater(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Tiền phòng (VNĐ)</label>
                  <input
                    type="number"
                    value={editRoomPrice}
                    onChange={(e) => setEditRoomPrice(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 font-bold"
                  />
                </div>
                <div>
                  <label className="block text-[10px] font-bold text-slate-700 mb-1">Phụ phí (VNĐ)</label>
                  <input
                    type="number"
                    value={editOtherFees}
                    onChange={(e) => setEditOtherFees(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
              </div>

              <div>
                <label className="block text-[10px] font-bold text-slate-700 mb-1">Trạng thái thanh toán</label>
                <select
                  value={editStatus}
                  onChange={(e) => setEditStatus(Number(e.target.value))}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 font-bold"
                >
                  <option value={0}>Chờ thanh toán</option>
                  <option value={1}>Đã thanh toán</option>
                  <option value={2}>Đã hủy</option>
                </select>
              </div>

              <div className="bg-slate-50 p-3 rounded-xl border border-slate-200 flex justify-between items-center font-bold">
                <span>Tổng tiền tính lại:</span>
                <span className="text-base text-[#0084ff]">{formatPrice(editCalculatedTotalAmount)}</span>
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setEditModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-slate-600 hover:bg-slate-100 font-semibold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="px-5 py-2 rounded-xl font-bold bg-[#0084ff] hover:bg-[#0073e6] text-white shadow-md cursor-pointer"
                >
                  Lưu thay đổi
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* MODAL 3: CREATE CONTRACT */}
      {contractModalOpen && (
        <div className="fixed inset-0 z-50 bg-slate-900/40 backdrop-blur-sm flex items-center justify-center p-4">
          <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl animate-in fade-in zoom-in-95">
            <h3 className="text-lg font-black text-slate-900 mb-3 flex items-center gap-2">
              <FiFileText className="text-emerald-500" /> Tạo Hợp Đồng Thuê Phòng Mới
            </h3>
            <form onSubmit={handleCreateContract} className="space-y-3 text-xs">
              <div>
                <label className="block font-bold text-slate-700 mb-1">Ngày bắt đầu *</label>
                <input
                  type="date"
                  value={contractStartDate}
                  onChange={(e) => setContractStartDate(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  required
                />
              </div>
              <div>
                <label className="block font-bold text-slate-700 mb-1">Ngày kết thúc *</label>
                <input
                  type="date"
                  value={contractEndDate}
                  onChange={(e) => setContractEndDate(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  required
                />
              </div>
              <div>
                <label className="block font-bold text-slate-700 mb-1">Tiền thuê hàng tháng (VNĐ) *</label>
                <input
                  type="number"
                  value={contractRent}
                  onChange={(e) => setContractRent(Number(e.target.value))}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs font-bold"
                  required
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  onClick={() => setContractModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-slate-600 hover:bg-slate-100 font-semibold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submittingContract}
                  className="px-5 py-2 rounded-xl font-bold bg-emerald-600 hover:bg-emerald-700 text-white shadow-md cursor-pointer"
                >
                  {submittingContract ? 'Đang tạo...' : 'Tạo Hợp Đồng'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
      </div>
    </div>
  );
};

export default LandlordContractsPage;
