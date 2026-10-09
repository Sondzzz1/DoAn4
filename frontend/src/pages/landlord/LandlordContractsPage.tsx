import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import {
  FiFileText,
  FiZap,
  FiDroplet,
  FiDownload,
  FiPlus,
  FiCheckCircle,
  FiClock,
  FiEdit2,
  FiTrash2,
  FiFilter,
  FiUsers,
  FiHome,
  FiCheck,
  FiAlertCircle,
  FiSearch,
} from 'react-icons/fi';
import { toast } from 'react-toastify';
import {
  monthlyBillService,
  MonthlyBillDto,
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
import StatusBadge, { StatusTone } from '../../components/common/StatusBadge';
import Modal from '../../components/landlord/LandlordModal';
import useLandlordAction from '../../components/landlord/useLandlordAction';
import PageState from '../../components/common/PageState';
import { CONTRACT_STATUS, DEPOSIT_STATUS, MONTHLY_BILL_STATUS, RENTAL_REQUEST_STATUS } from '../../utils/constants';
import { getApiErrorMessage } from '../../utils/apiError';

const requestStatus = (status: number): { label: string; tone: StatusTone } => ({
  0: { label: 'Chờ duyệt', tone: 'pending' }, 1: { label: 'Đã duyệt', tone: 'success' }, 2: { label: 'Đã từ chối', tone: 'danger' },
  3: { label: 'Đã hủy', tone: 'danger' }, 4: { label: 'Đã tạo hợp đồng', tone: 'info' }, 5: { label: 'Đã hết hạn cọc', tone: 'danger' },
}[status] as { label: string; tone: StatusTone } || { label: 'Không xác định', tone: 'neutral' });

const dateInputAfterDays = (days: number) => {
  const date = new Date();
  date.setDate(date.getDate() + days);
  return date.toISOString().slice(0, 10);
};

const dateTimeInputAfterHours = (hours: number) => {
  const date = new Date(Date.now() + hours * 60 * 60 * 1000);
  date.setMinutes(date.getMinutes() - date.getTimezoneOffset());
  return date.toISOString().slice(0, 16);
};

type LandlordTab = 'bills' | 'contracts' | 'requests';

const getSelectedTab = (tab: string | null): LandlordTab => (
  tab === 'bills' || tab === 'contracts' || tab === 'requests' ? tab : 'requests'
);

const LandlordContractsPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = getSelectedTab(searchParams.get('tab'));
  const depositSection = activeTab === 'requests' && searchParams.get('section') === 'deposits';
  const { openAction, dialog } = useLandlordAction();
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [formErrors, setFormErrors] = useState<Partial<Record<'bill' | 'edit' | 'deposit' | 'contract', string>>>({});

  // Data states
  const [bills, setBills] = useState<MonthlyBillDto[]>([]);
  const [contracts, setContracts] = useState<ContractDto[]>([]);
  const [requests, setRequests] = useState<RentalRequestDto[]>([]);
  const [deposits, setDeposits] = useState<DepositDto[]>([]);
  const [query, setQuery] = useState('');
  const matchesQuery = (...values: (string | number | undefined)[]) => values.some(value => String(value ?? '').toLowerCase().includes(query.trim().toLowerCase()));

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
  const [billDueDate, setBillDueDate] = useState<string>(() => dateInputAfterDays(7));
  const [billNote, setBillNote] = useState<string>('');

  // Modal Edit Bill State
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [submittingEdit, setSubmittingEdit] = useState(false);
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

  // Modal Create Contract State
  const [contractModalOpen, setContractModalOpen] = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState<number | null>(null);
  const [contractStartDate, setContractStartDate] = useState(() => dateInputAfterDays(0));
  const [contractEndDate, setContractEndDate] = useState(() => dateInputAfterDays(180));
  const [contractRent, setContractRent] = useState<number>(3000000);
  const [submittingContract, setSubmittingContract] = useState(false);

  // Deposit setup state, used both for new approvals and legacy approved requests.
  const [depositModalOpen, setDepositModalOpen] = useState(false);
  const [depositAction, setDepositAction] = useState<'approve' | 'setup'>('approve');
  const [depositRequest, setDepositRequest] = useState<RentalRequestDto | null>(null);
  const [depositAmount, setDepositAmount] = useState(0);
  const [depositDueAt, setDepositDueAt] = useState(() => dateTimeInputAfterHours(24));
  const [submittingDeposit, setSubmittingDeposit] = useState(false);

  const selectTab = (tab: LandlordTab) => {
    setSearchParams({ tab });
  };

  // Load all data
  async function loadData() {
    setLoading(true);
    setLoadError('');
    try {
      const [billsRes, contractsRes, requestsRes, depositsRes] = await Promise.all([
        monthlyBillService.getMyBills(true),
        rentalService.getMyContracts(),
        rentalService.getMyRentalRequests(true),
        rentalService.getMyDeposits(),
      ]);

      setBills(billsRes.data || []);
      setContracts(contractsRes.data || []);
      setRequests(requestsRes.data || []);
      setDeposits(depositsRes.data || []);
    } catch (error) {
      setLoadError(getApiErrorMessage(error, 'Không thể tải dữ liệu.'));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    void Promise.resolve().then(loadData);
  }, []);

  const selectBillContract = (contractId: number | '') => {
    setSelectedContractId(contractId);
    const found = contracts.find((contract) => contract.id === Number(contractId));
    if (!found) return;

    setRoomPrice(found.tienThueHangThang);
    setPriceElec(found.giaDien || 0);
    setPriceWater(found.giaNuoc || 0);
  };

  // Real-time calculation for Create Modal
  const calculatedElecUsed = Math.max(0, newElec - oldElec);
  const calculatedElecAmount = calculatedElecUsed * priceElec;
  const calculatedWaterUsed = Math.max(0, newWater - oldWater);
  const calculatedWaterAmount = calculatedWaterUsed * priceWater;
  const selectedContract = contracts.find((c) => c.id === Number(selectedContractId));
  const serviceFee = selectedContract?.phiDichVu || 0;
  const calculatedTotalAmount = roomPrice + calculatedElecAmount + calculatedWaterAmount + serviceFee + otherFees;

  // Real-time calculation for Edit Modal
  const editCalculatedElecUsed = Math.max(0, editNewElec - editOldElec);
  const editCalculatedElecAmount = editCalculatedElecUsed * editPriceElec;
  const editCalculatedWaterUsed = Math.max(0, editNewWater - editOldWater);
  const editCalculatedWaterAmount = editCalculatedWaterUsed * editPriceWater;
  const editCalculatedTotalAmount = editRoomPrice + editCalculatedElecAmount + editCalculatedWaterAmount + (editingBill?.phiDichVu || 0) + editOtherFees;

  // Handle Create Bill
  const handleCreateBill = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors(current => ({ ...current, bill: undefined }));
    if (submittingBill) return;
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
        phiDichVu: serviceFee,
        chiPhiKhac: otherFees,
        ghiChuChiPhiKhac: otherFeesNote,
        hanThanhToan: billDueDate ? new Date(billDueDate).toISOString() : undefined,
        ghiChu: billNote || undefined,
      });

      toast.success(`Tạo hóa đơn tháng ${billMonth}/${billYear} thành công!`);
      setBillModalOpen(false);
      resetBillForm();
      await loadData();
    } catch (error) {
      setFormErrors(current => ({ ...current, bill: getApiErrorMessage(error, 'Không thể tạo hóa đơn.') }));
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
    setEditModalOpen(true);
  };

  const handleUpdateBill = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors(current => ({ ...current, edit: undefined }));
    if (!editingBill || submittingEdit) return;
    if (editOldElec < 0 || editNewElec < editOldElec || editOldWater < 0 || editNewWater < editOldWater || editOtherFees < 0) {
      toast.warning('Chỉ số mới phải bằng hoặc lớn hơn chỉ số cũ; số tiền và chỉ số không được âm.');
      return;
    }
    setSubmittingEdit(true);
    try {
      await monthlyBillService.updateBill(editingBill.id, {
        soDienCu: editOldElec,
        soDienMoi: editNewElec,
        giaDien: editPriceElec,
        soNuocCu: editOldWater,
        soNuocMoi: editNewWater,
        giaNuoc: editPriceWater,
        tienPhong: editRoomPrice,
        phiDichVu: editingBill.phiDichVu,
        chiPhiKhac: editOtherFees,
        ghiChuChiPhiKhac: editOtherFeesNote,
      });

      toast.success('Cập nhật hóa đơn thành công!');
      setEditModalOpen(false);
      await loadData();
    } catch (error) {
      setFormErrors(current => ({ ...current, edit: getApiErrorMessage(error, 'Không thể cập nhật hóa đơn.') }));
    } finally {
      setSubmittingEdit(false);
    }
  };

  // Handle Mark Bill as Paid
  const handleConfirmBillPaid = async (billId: number) => {
    try {
      await monthlyBillService.payBill(billId, {
        phuongThucThanhToan: 'Chủ trọ xác nhận đã nhận tiền',
      });
      toast.success('Đã ghi nhận thanh toán hóa đơn thành công!');
      await loadData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể cập nhật trạng thái.'));
      return false;
    }
  };

  // Handle Delete Bill
  const handleDeleteBill = async (billId: number) => {
    try {
      await monthlyBillService.deleteBill(billId);
      toast.success('Hủy hóa đơn thành công!');
      await loadData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể hủy hóa đơn.'));
      return false;
    }
  };

  const openDepositModal = (request: RentalRequestDto, action: 'approve' | 'setup') => {
    setDepositRequest(request);
    setDepositAction(action);
    setDepositAmount(request.giaThue || 0);
    setDepositDueAt(dateTimeInputAfterHours(24));
    setDepositModalOpen(true);
  };

  const handleDepositSetup = async (event: React.FormEvent) => {
    event.preventDefault();
    if (submittingDeposit) return;
    setFormErrors(current => ({ ...current, deposit: undefined }));
    if (!depositRequest || depositAmount <= 0 || !depositDueAt) {
      toast.warning('Vui lòng nhập số tiền cọc và hạn thanh toán hợp lệ.');
      return;
    }

    setSubmittingDeposit(true);
    try {
      const dueAt = new Date(depositDueAt).toISOString();
      if (depositAction === 'approve') {
        await rentalService.updateRentalRequestStatus(depositRequest.id, RENTAL_REQUEST_STATUS.APPROVED, undefined, {
          soTienDatCoc: depositAmount,
          hanThanhToanCoc: dueAt,
        });
        toast.success('Đã duyệt yêu cầu và tạo khoản cọc cho khách thuê.');
      } else {
        await rentalService.repairLegacyDeposit(depositRequest.id, { soTien: depositAmount, hanThanhToan: dueAt });
        toast.success('Đã thiết lập khoản cọc cho khách thuê.');
      }
      setDepositModalOpen(false);
      await loadData();
    } catch (error) {
      setFormErrors(current => ({ ...current, deposit: getApiErrorMessage(error, 'Không thể thiết lập khoản cọc.') }));
    } finally {
      setSubmittingDeposit(false);
    }
  };

  const handleRejectRequest = async (requestId: number, reason: string) => {
    try {
      await rentalService.updateRentalRequestStatus(requestId, RENTAL_REQUEST_STATUS.REJECTED, reason.trim() || 'Yêu cầu chưa phù hợp.');
      toast.info('Đã từ chối yêu cầu thuê phòng.');
      await loadData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể từ chối yêu cầu.'));
      return false;
    }
  };

  const handleConfirmDeposit = async (depositId: number) => {
    try { await rentalService.confirmDeposit(depositId); toast.success('Đã xác nhận khoản cọc.'); await loadData(); }
    catch (error) { toast.error(getApiErrorMessage(error, 'Không thể xác nhận khoản cọc.')); return false; }
  };

  // Handle Create Contract from Request
  const handleOpenCreateContract = (req: RentalRequestDto) => {
    setSelectedRequestId(req.id);
    setContractRent(req.giaThue || 0);
    setContractModalOpen(true);
  };

  const handleCreateContract = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormErrors(current => ({ ...current, contract: undefined }));
    if (submittingContract) return;
    if (contractEndDate < contractStartDate || contractRent <= 0) {
      toast.warning('Ngày thuê cuối cùng không được trước ngày bắt đầu và tiền thuê phải lớn hơn 0.');
      return;
    }
    if (!selectedRequestId) return;

    setSubmittingContract(true);
    try {
      await rentalService.createContract({
        yeuCauThueId: selectedRequestId,
        ngayBatDau: contractStartDate,
        ngayKetThuc: contractEndDate,
        tienThueHangThang: contractRent,
      });

      toast.success('Tạo hợp đồng thuê phòng thành công!');
      setContractModalOpen(false);
      await loadData();
    } catch (error) {
      setFormErrors(current => ({ ...current, contract: getApiErrorMessage(error, 'Không thể tạo hợp đồng.') }));
    } finally {
      setSubmittingContract(false);
    }
  };

  // Handle Terminate Contract
  const handleTerminateContract = async (contractId: number, reason: string) => {

    try {
      await rentalService.terminateContract(contractId, reason || undefined);
      toast.success('Đã chấm dứt hợp đồng thành công! Trạng thái phòng đã được chuyển về Còn trống.');
      await loadData();
    } catch (error) {
      toast.error(getApiErrorMessage(error, 'Không thể chấm dứt hợp đồng.'));
      return false;
    }
  };

  const handleConfirmContract = async (contractId: number) => {
    try { await rentalService.confirmContract(contractId); toast.success('Đã xác nhận hợp đồng.'); await loadData(); }
    catch (error) { toast.error(getApiErrorMessage(error, 'Không thể xác nhận hợp đồng.')); return false; }
  };

  // Filter bills
  const filteredBills = bills.filter((b) => {
    if (!matchesQuery(b.id, b.tenPhong, b.tenNguoiThue, b.diaChiPhong)) return false;
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
  const pendingRequestCount = requests.filter((request) => request.trangThai === RENTAL_REQUEST_STATUS.PENDING).length;
  const pendingDepositCount = deposits.filter((deposit) => deposit.trangThai === DEPOSIT_STATUS.PENDING).length;
  const paidDepositCount = deposits.filter((deposit) => deposit.trangThai === DEPOSIT_STATUS.PAID).length;
  const confirmedDepositCount = deposits.filter((deposit) => deposit.trangThai === DEPOSIT_STATUS.CONFIRMED).length;
  const isRequestTab = activeTab === 'requests';

  if (loadError && !loading) return <PageState type="error" message={loadError} onRetry={loadData} />;

  return (
    <div className="landlord-contracts-page">
      <div className="landlord-contracts-container">
        {/* Top Header */}
        <div className="contracts-header">
          <div className="contracts-header-content">
            <div className="contracts-header-info">
              <div className="contracts-header-badge">
                {isRequestTab ? <FiUsers /> : <FiZap />} {isRequestTab ? 'Quản lý khách thuê' : 'Quản lý PMS & Điện Nước'}
              </div>
              <h1>{isRequestTab ? depositSection ? 'Quản lý cọc' : 'Yêu cầu thuê phòng' : activeTab === 'contracts' ? 'Hợp đồng thuê' : 'Hóa đơn điện nước & tiền phòng'}</h1>
            </div>

            {!isRequestTab && <div className="contracts-header-actions">
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
            </div>}
          </div>
        </div>

        {/* KPI Stats Cards */}
        <div className="kpi-stats-grid">
          {isRequestTab ? <>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Yêu cầu chờ duyệt</span><FiUsers className="kpi-card-icon blue" /></div><div className="kpi-card-value">{pendingRequestCount}</div><span className="kpi-card-subtitle">Cần chủ trọ xử lý</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Cọc chờ thanh toán</span><FiClock className="kpi-card-icon amber" /></div><div className="kpi-card-value amber">{pendingDepositCount}</div><span className="kpi-card-subtitle">Khách chưa thanh toán</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Cọc chờ xác nhận</span><FiCheckCircle className="kpi-card-icon emerald" /></div><div className="kpi-card-value emerald">{paidDepositCount}</div><span className="kpi-card-subtitle">Cần xác nhận đã nhận tiền</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Cọc đã xác nhận</span><FiFileText className="kpi-card-icon purple" /></div><div className="kpi-card-value">{confirmedDepositCount}</div><span className="kpi-card-subtitle">Có thể tạo hợp đồng</span></div>
          </> : <>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Hợp đồng thuê</span><FiFileText className="kpi-card-icon blue" /></div><div className="kpi-card-value">{contracts.length}</div><span className="kpi-card-subtitle">{contracts.filter((contract) => contract.trangThai === CONTRACT_STATUS.ACTIVE).length} đang hiệu lực</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Hóa đơn đã thu</span><FiCheckCircle className="kpi-card-icon emerald" /></div><div className="kpi-card-value emerald">{formatPrice(totalRevenuePaid)}</div><span className="kpi-card-subtitle">{bills.filter((bill) => bill.trangThai === MONTHLY_BILL_STATUS.PAID).length} hóa đơn đã xong</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Chờ thanh toán</span><FiClock className="kpi-card-icon amber" /></div><div className="kpi-card-value amber">{formatPrice(totalRevenuePending)}</div><span className="kpi-card-subtitle">{bills.filter((bill) => bill.trangThai === MONTHLY_BILL_STATUS.UNPAID).length} hóa đơn chưa thu</span></div>
            <div className="kpi-card"><div className="kpi-card-header"><span className="kpi-card-label">Tổng hóa đơn</span><FiZap className="kpi-card-icon purple" /></div><div className="kpi-card-value">{bills.length}</div><span className="kpi-card-subtitle">Toàn bộ các kỳ</span></div>
          </>}
        </div>

        {/* Tabs Switcher */}
        <div className="contracts-tabs">
          <button
            onClick={() => selectTab('bills')}
            className={`tab-button ${activeTab === 'bills' ? 'active' : ''}`}
          >
            <FiZap /> Hóa Đơn Điện Nước & Tiền Phòng ({bills.length})
          </button>
          <button
            onClick={() => selectTab('contracts')}
            className={`tab-button ${activeTab === 'contracts' ? 'active' : ''}`}
          >
            <FiFileText /> Danh Sách Hợp Đồng ({contracts.length})
          </button>
          <button
            onClick={() => selectTab('requests')}
            className={`tab-button ${activeTab === 'requests' && !depositSection ? 'active' : ''}`}
          >
            <FiUsers /> Yêu Cầu Thuê Phòng ({requests.length})
          </button>
          <button onClick={() => setSearchParams({ tab: 'requests', section: 'deposits' })} className={`tab-button ${depositSection ? 'active' : ''}`}><FiCheckCircle /> Quản lý cọc ({deposits.length})</button>
        </div>

      {/* Tab 1: BILLS & UTILITY METERS */}
      <div className="landlord-toolbar"><label className="landlord-search"><FiSearch /><input aria-label="Tìm khách thuê hoặc phòng" placeholder="Tìm khách thuê, phòng hoặc mã..." value={query} onChange={event => setQuery(event.target.value)} /></label></div>
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
                    <option value={2}>Đã hủy</option>
                    <option value={3}>Đang thanh toán</option>
                    <option value={4}>Quá hạn</option>
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
                          <StatusBadge label={bill.trangThai === 1 ? 'Đã thanh toán' : bill.trangThai === 2 ? 'Đã hủy' : bill.trangThai === 3 ? 'Đang thanh toán' : bill.trangThai === 4 ? 'Quá hạn' : 'Chưa thanh toán'} tone={bill.trangThai === 1 ? 'success' : bill.trangThai === 3 ? 'info' : bill.trangThai === 2 || bill.trangThai === 4 ? 'danger' : 'pending'} />
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
                    {(bill.trangThai === MONTHLY_BILL_STATUS.UNPAID || bill.trangThai === MONTHLY_BILL_STATUS.OVERDUE || bill.trangThai === MONTHLY_BILL_STATUS.PENDING_PAYMENT) && (
                      <button
                        onClick={() => openAction({ title: 'Xác nhận thanh toán hóa đơn', message: 'Xác nhận hóa đơn này đã được khách thuê thanh toán đầy đủ?', run: () => handleConfirmBillPaid(bill.id) })}
                        className="btn-icon success"
                        title="Xác nhận đã nhận tiền"
                      >
                        <FiCheck /> Đã thu
                      </button>
                    )}
                    {(bill.trangThai === MONTHLY_BILL_STATUS.UNPAID || bill.trangThai === MONTHLY_BILL_STATUS.OVERDUE) && <button
                      onClick={() => handleOpenEditModal(bill)}
                      className="btn-icon edit"
                      title="Chỉnh sửa chỉ số"
                    >
                      <FiEdit2 /> Sửa
                    </button>}
                    {(bill.trangThai === MONTHLY_BILL_STATUS.UNPAID || bill.trangThai === MONTHLY_BILL_STATUS.OVERDUE) && <button
                      onClick={() => openAction({ title: 'Hủy hóa đơn', message: 'Bạn có chắc chắn muốn hủy hóa đơn này? Hóa đơn vẫn được lưu trong lịch sử.', run: () => handleDeleteBill(bill.id) })}
                      className="btn-icon delete"
                      title="Hủy hóa đơn"
                    >
                      <FiTrash2 /> Hủy
                    </button>}
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
                      <span className="meter-label">Phí dịch vụ:</span>
                      <strong className="meter-value">{formatPrice(bill.phiDichVu || 0)}</strong>
                    </div>

                    <div className="meter-item">
                      <span className="meter-label">Phụ phí khác:</span>
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
              <FiFileText className="empty-state-icon" />
              <h3>Chưa có hợp đồng nào được tạo.</h3>
              <p>Chuyển qua tab "Yêu Cầu Thuê Phòng" để duyệt yêu cầu và tạo hợp đồng mới.</p>
            </div>
          ) : (
            <div className="contracts-grid">
              {contracts.filter(con => matchesQuery(con.id, con.tenPhong, con.tenNguoiThue, con.diaChiPhong)).map((con) => (
                <div key={con.id} className="contract-card">
                  <div>
                    <div className="contract-card-header">
                      <span className="contract-id">
                        Hợp đồng HD-{String(con.id).padStart(4, '0')}
                      </span>
                      <span
                        className={`bill-status-badge ${
                          con.trangThai === CONTRACT_STATUS.ACTIVE ? 'paid' : (con.trangThai >= CONTRACT_STATUS.TERMINATED ? 'expired' : 'pending')
                        }`}
                      >
                        {con.trangThai === CONTRACT_STATUS.ACTIVE ? 'Đang hiệu lực' : con.trangThai === CONTRACT_STATUS.TERMINATED ? 'Đã chấm dứt' : con.trangThai === CONTRACT_STATUS.EXPIRED ? 'Đã hết hạn' : con.trangThai === CONTRACT_STATUS.PENDING_START ? 'Chờ ngày bắt đầu' : con.trangThai === CONTRACT_STATUS.CANCELLED ? 'Đã hủy' : 'Chờ xác nhận'}
                      </span>
                    </div>

                    <h3 className="text-base font-bold text-slate-900 mt-2 mb-1">
                      {con.tenPhong || `Phòng hợp đồng #${con.id}`}
                    </h3>

                    <div className="contract-rent">
                      {formatPrice(con.tienThueHangThang)} <span>/ tháng</span>
                    </div>

                    <div className="contract-details space-y-1 text-xs text-slate-600 mt-3 bg-slate-50 p-3 rounded-xl">
                      <div>
                        Khách thuê: <strong>{con.tenNguoiThue}</strong> {con.sdtNguoiThue ? `(${con.sdtNguoiThue})` : ''}
                      </div>
                      {con.diaChiPhong && (
                        <div className="line-clamp-1">
                          Địa chỉ: <span>{con.diaChiPhong}</span>
                        </div>
                      )}
                      <div>
                        Thời hạn: <strong>{new Date(con.ngayBatDau).toLocaleDateString('vi-VN')}</strong> đến{' '}
                        <strong>{new Date(con.ngayKetThuc).toLocaleDateString('vi-VN')}</strong>
                      </div>
                      <div className="flex items-center gap-3 pt-1 border-t border-slate-200">
                        <span>Chủ trọ: {con.chuTroDaXacNhan ? '✅ Đã ký' : '⏳ Chưa ký'}</span>
                        <span>Khách thuê: {con.nguoiThueDaXacNhan ? '✅ Đã ký' : '⏳ Chưa ký'}</span>
                      </div>
                    </div>
                  </div>

                  <div className="contract-card-actions mt-4 flex items-center gap-2 flex-wrap">
                    {con.trangThai === CONTRACT_STATUS.PENDING_SIGNATURE && !con.chuTroDaXacNhan && <button type="button" onClick={() => openAction({ title: 'Ký hợp đồng', message: `Xác nhận ký hợp đồng #${con.id}?`, run: () => handleConfirmContract(con.id) })} className="btn-primary"><FiCheck /> Ký hợp đồng</button>}
                    <button
                      type="button"
                      onClick={() => exportService.downloadContractPdf(con.id)}
                      className="btn-success"
                    >
                      <FiDownload /> Tải HĐ (PDF)
                    </button>
                    {[CONTRACT_STATUS.ACTIVE, CONTRACT_STATUS.EXPIRED, CONTRACT_STATUS.TERMINATED].some(status => status === con.trangThai) && (
                      <>
                        <button
                          type="button"
                          onClick={() => {
                            selectBillContract(con.id);
                            setBillModalOpen(true);
                          }}
                          className="btn-primary"
                        >
                          <FiZap /> Lập Hóa Đơn
                        </button>
                        {con.trangThai === CONTRACT_STATUS.ACTIVE && <button
                          type="button"
                          onClick={() => openAction({ title: 'Chấm dứt hợp đồng', message: 'Xác nhận chấm dứt hợp đồng và trả phòng về còn trống?', reasonLabel: 'Lý do chấm dứt (không bắt buộc)', run: reason => handleTerminateContract(con.id, reason) })}
                          className="btn-danger"
                          title="Chấm dứt hợp đồng và trả phòng về còn trống"
                        >
                          <FiAlertCircle /> Chấm Dứt HĐ
                        </button>}
                      </>
                    )}
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
          {deposits.length > 0 && <section className="mb-6" id="landlord-deposits">
            <h3 className="mb-3 text-base font-bold text-slate-900">Tiền cọc cần theo dõi</h3>
            <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
              {deposits.map(dep => {
                const request = requests.find(r => r.id === dep.yeuCauThueId);
                if (!matchesQuery(dep.id, request?.tieuDeBaiDang, request?.tenNguoiThue)) return null;
                return <div key={dep.id} className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
                  <div className="flex items-start justify-between gap-3"><div><p className="text-sm font-bold text-slate-900">{request?.tieuDeBaiDang || `Khoản cọc #${dep.id}`}</p><p className="mt-1 text-xl font-bold text-blue-600">{formatPrice(dep.soTien)}</p></div><StatusBadge label={dep.trangThai === DEPOSIT_STATUS.PENDING ? 'Chờ thanh toán' : dep.trangThai === DEPOSIT_STATUS.PAID ? 'Đã thanh toán' : dep.trangThai === DEPOSIT_STATUS.CONFIRMED ? 'Đã xác nhận' : dep.trangThai === DEPOSIT_STATUS.EXPIRED ? 'Đã hết hạn' : 'Đã đóng'} tone={dep.trangThai === DEPOSIT_STATUS.CONFIRMED ? 'success' : dep.trangThai === DEPOSIT_STATUS.PAID ? 'info' : dep.trangThai === DEPOSIT_STATUS.EXPIRED ? 'danger' : 'pending'} /></div>
                  {dep.hanThanhToan && dep.trangThai === DEPOSIT_STATUS.PENDING && <p className="mt-2 text-xs text-amber-700">Hạn thanh toán: {new Date(dep.hanThanhToan).toLocaleString('vi-VN')}</p>}
                  {dep.ngayThanhToan && <p className="mt-2 text-xs text-slate-500">Thanh toán: {new Date(dep.ngayThanhToan).toLocaleString('vi-VN')}</p>}
                  {dep.trangThai === DEPOSIT_STATUS.PAID && <button onClick={() => openAction({ title: 'Xác nhận đã nhận cọc', message: `Xác nhận bạn đã nhận đúng khoản tiền cọc ${formatPrice(dep.soTien)}?`, run: () => handleConfirmDeposit(dep.id) })} className="mt-3 rounded-md bg-emerald-600 px-3 py-2 text-xs font-semibold text-white hover:bg-emerald-700">Xác nhận đã nhận cọc</button>}
                </div>;
              })}
            </div>
          </section>}
          {depositSection && deposits.length === 0 && <PageState type="empty" message="Chưa có khoản cọc nào." />}
          <div hidden={depositSection}>
          {requests.length === 0 ? (
            <div className="empty-state">
              <FiUsers className="empty-state-icon" />
              <h3>Chưa có yêu cầu thuê phòng nào từ khách.</h3>
              <p>Khi khách thuê gửi yêu cầu thuê phòng, danh sách sẽ hiển thị tại đây.</p>
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {requests.filter(req => matchesQuery(req.id, req.tieuDeBaiDang, req.tenNguoiThue, req.diaChi)).map((req) => (
                <div
                  key={req.id}
                  className="bg-white rounded-2xl border border-slate-200 p-5 shadow-sm hover:shadow transition-all space-y-3"
                >
                  <div className="flex items-start justify-between gap-2 border-b border-slate-100 pb-3">
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-xs font-bold uppercase tracking-wider text-blue-600">
                          Yêu cầu #{req.id}
                        </span>
                        <StatusBadge label={requestStatus(req.trangThai).label} tone={requestStatus(req.trangThai).tone} />
                      </div>
                      <h4 className="font-bold text-slate-900 mt-1 text-base">
                        {req.tieuDeBaiDang || `Bài đăng #${req.baiDangId}`}
                      </h4>
                    </div>
                    <span className="text-xs text-slate-400">
                      {new Date(req.ngayTao).toLocaleDateString('vi-VN')}
                    </span>
                  </div>

                  <div className="text-xs text-slate-600 bg-slate-50 p-3 rounded-xl space-y-1">
                    <div>
                      Khách thuê: <strong>{req.tenNguoiThue || `#${req.nguoiThueId}`}</strong> {req.sdtNguoiThue && `- ${req.sdtNguoiThue}`}
                    </div>
                    <div>
                      Ghi chú: <span className="italic text-slate-700">"{req.ghiChu || 'Không có ghi chú'}"</span>
                    </div>
                  </div>

                  <div className="flex items-center gap-2 pt-2">
                    {req.trangThai === RENTAL_REQUEST_STATUS.PENDING && (
                      <>
                        <button
                          type="button"
                          onClick={() => openDepositModal(req, 'approve')}
                          className="px-3.5 py-2 bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs rounded-xl shadow-sm cursor-pointer transition-all border-none flex items-center gap-1.5"
                        >
                          <FiCheck /> Duyệt Yêu Cầu
                        </button>
                        <button
                          type="button"
                          onClick={() => openAction({ title: 'Từ chối yêu cầu thuê', message: `Từ chối yêu cầu thuê #${req.id}?`, reasonLabel: 'Lý do từ chối', run: reason => handleRejectRequest(req.id, reason) })}
                          className="px-3.5 py-2 bg-slate-100 hover:bg-rose-50 hover:text-rose-600 text-slate-600 font-bold text-xs rounded-xl cursor-pointer transition-all border border-slate-200"
                        >
                          Từ Chối
                        </button>
                      </>
                    )}

                    {req.trangThai === RENTAL_REQUEST_STATUS.APPROVED && deposits.some(d => d.yeuCauThueId === req.id && d.trangThai === DEPOSIT_STATUS.CONFIRMED) && (
                      <button
                        type="button"
                        onClick={() => handleOpenCreateContract(req)}
                        className="px-4 py-2 bg-[#0084ff] hover:bg-blue-600 text-white font-bold text-xs rounded-xl shadow-md cursor-pointer transition-all border-none flex items-center gap-1.5"
                      >
                        <FiPlus /> Tạo Hợp Đồng Thuê
                      </button>
                    )}
                    {req.trangThai === RENTAL_REQUEST_STATUS.APPROVED && !deposits.some(d => d.yeuCauThueId === req.id) && (
                      <button
                        type="button"
                        onClick={() => openDepositModal(req, 'setup')}
                        className="px-3.5 py-2 bg-blue-600 hover:bg-blue-700 text-white font-bold text-xs rounded-xl shadow-sm cursor-pointer transition-all border-none flex items-center gap-1.5"
                      >
                        <FiPlus /> Bổ sung cọc (dữ liệu cũ)
                      </button>
                    )}
                    {req.trangThai === RENTAL_REQUEST_STATUS.APPROVED && !deposits.some(d => d.yeuCauThueId === req.id && d.trangThai === DEPOSIT_STATUS.CONFIRMED) && <p className="text-xs font-medium text-amber-700">Chờ người thuê thanh toán và chủ trọ xác nhận cọc trước khi tạo hợp đồng.</p>}
                    {req.lyDoHuy && <p className="text-sm text-rose-700">{req.lyDoHuy}</p>}
                  </div>
                </div>
              ))}
            </div>
          )}
          </div>
        </div>
      )}

      {depositModalOpen && depositRequest && (
        <Modal isOpen={depositModalOpen} onClose={() => { if (!submittingDeposit) setDepositModalOpen(false); }} title={depositAction === 'approve' ? 'Duyệt yêu cầu và thiết lập cọc' : 'Thiết lập khoản đặt cọc'}>
            <p className="mt-2 text-sm text-slate-600">
              Khách thuê chỉ có thể giữ phòng khi thanh toán đúng số tiền trước hạn bạn đặt ra.
            </p>
            <form onSubmit={handleDepositSetup} className="mt-5 space-y-4">
              <div className="rounded-lg bg-slate-50 p-3 text-sm text-slate-700">
                <p className="font-semibold">{depositRequest.tieuDeBaiDang || `Yêu cầu #${depositRequest.id}`}</p>
                <p className="mt-1">Khách thuê: {depositRequest.tenNguoiThue || `#${depositRequest.nguoiThueId}`}</p>
              </div>
              <label className="block text-sm font-semibold text-slate-700">
                Số tiền đặt cọc (VNĐ)
                <input
                  type="number"
                  min="1"
                  value={depositAmount || ''}
                  onChange={(event) => setDepositAmount(Number(event.target.value))}
                  className="mt-1.5 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  required
                />
              </label>
              <label className="block text-sm font-semibold text-slate-700">
                Hạn thanh toán
                <input
                  type="datetime-local"
                  value={depositDueAt}
                  min={dateTimeInputAfterHours(1)}
                  onChange={(event) => setDepositDueAt(event.target.value)}
                  className="mt-1.5 w-full rounded-lg border border-slate-300 px-3 py-2.5 outline-none focus:border-blue-500 focus:ring-2 focus:ring-blue-100"
                  required
                />
              </label>
              <div className="flex justify-end gap-3 pt-2">
                <button type="button" disabled={submittingDeposit} onClick={() => setDepositModalOpen(false)} className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 hover:bg-slate-50">
                  Hủy
                </button>
                <button type="submit" disabled={submittingDeposit} className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white hover:bg-emerald-700 disabled:cursor-not-allowed disabled:opacity-60">
                  {submittingDeposit ? 'Đang lưu...' : depositAction === 'approve' ? 'Duyệt và tạo cọc' : 'Tạo khoản cọc'}
                </button>
              </div>
            {formErrors.deposit && <p role="alert" className="form-error">{formErrors.deposit}</p>}
            </form>
        </Modal>
      )}

      {/* MODAL 1: CREATE BILL & CALCULATE UTILITIES */}
      {billModalOpen && (
        <Modal isOpen={billModalOpen} onClose={() => { if (!submittingBill) setBillModalOpen(false); }} title="Tạo hóa đơn hàng tháng" size="lg">
            <form onSubmit={handleCreateBill} className="space-y-4 text-xs">
              {/* Row 1: Contract & Month/Year */}
              <div className="grid grid-cols-1 sm:grid-cols-3 gap-3">
                <div className="sm:col-span-1">
                  <label htmlFor="field-selectedContractId" className="block font-bold text-slate-700 mb-1">Chọn Hợp Đồng *</label>
                  <select id="field-selectedContractId"
                    value={selectedContractId}
                    onChange={(e) => selectBillContract(e.target.value ? Number(e.target.value) : '')}
                    required
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs focus:border-[#0084ff] bg-slate-50 font-semibold"
                  >
                    <option value="">-- Chọn hợp đồng --</option>
                    {contracts.filter((c) => [CONTRACT_STATUS.ACTIVE, CONTRACT_STATUS.EXPIRED, CONTRACT_STATUS.TERMINATED].some(status => status === c.trangThai)).map((c) => (
                      <option key={c.id} value={c.id}>
                        HD-{String(c.id).padStart(4, '0')} ({formatPrice(c.tienThueHangThang)}/tháng)
                      </option>
                    ))}
                  </select>
                </div>

                <div>
                  <label htmlFor="field-billMonth" className="block font-bold text-slate-700 mb-1">Tháng *</label>
                  <select id="field-billMonth"
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
                  <label htmlFor="field-billYear" className="block font-bold text-slate-700 mb-1">Năm *</label>
                  <input id="field-billYear"
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
                    <label htmlFor="field-oldElec" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số điện cũ</label>
                    <input id="field-oldElec"
                      type="number"
                      step="any"
                      value={oldElec}
                      onChange={(e) => setOldElec(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                  <div>
                    <label htmlFor="field-newElec" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số điện mới</label>
                    <input id="field-newElec"
                      type="number"
                      step="any"
                      value={newElec}
                      onChange={(e) => setNewElec(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white font-bold text-amber-700"
                    />
                  </div>
                  <div>
                    <label htmlFor="field-priceElec" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Đơn giá (VNĐ/kWh)</label>
                    <input id="field-priceElec"
                      type="number"
                      value={priceElec}
                      readOnly
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-slate-100"
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
                    <label htmlFor="field-oldWater" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số nước cũ</label>
                    <input id="field-oldWater"
                      type="number"
                      step="any"
                      value={oldWater}
                      onChange={(e) => setOldWater(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white"
                    />
                  </div>
                  <div>
                    <label htmlFor="field-newWater" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Số nước mới</label>
                    <input id="field-newWater"
                      type="number"
                      step="any"
                      value={newWater}
                      onChange={(e) => setNewWater(Number(e.target.value))}
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-white font-bold text-blue-700"
                    />
                  </div>
                  <div>
                    <label htmlFor="field-priceWater" className="block text-[10px] font-semibold text-slate-600 mb-0.5">Đơn giá (VNĐ/m³)</label>
                    <input id="field-priceWater"
                      type="number"
                      value={priceWater}
                      readOnly
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 text-xs bg-slate-100"
                    />
                  </div>
                </div>
              </div>

              {/* ROOM PRICE & OTHER FEES */}
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label htmlFor="field-roomPrice" className="block font-bold text-slate-700 mb-1">Tiền Phòng (VNĐ) *</label>
                  <input id="field-roomPrice"
                    type="number"
                    value={roomPrice}
                    readOnly
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 bg-slate-100 text-xs font-bold text-slate-800"
                    required
                  />
                </div>

                <div>
                  <label htmlFor="field-otherFees" className="block font-bold text-slate-700 mb-1">Phụ phí / Dịch vụ (VNĐ)</label>
                  <input id="field-otherFees"
                    type="number"
                    value={otherFees}
                    onChange={(e) => setOtherFees(Number(e.target.value))}
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  />
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                <div>
                  <label htmlFor="field-otherFeesNote" className="block font-bold text-slate-700 mb-1">Ghi chú phụ phí</label>
                  <input id="field-otherFeesNote"
                    type="text"
                    value={otherFeesNote}
                    onChange={(e) => setOtherFeesNote(e.target.value)}
                    placeholder="Ví dụ: Tiền rác, tiền vệ sinh, wifi..."
                    className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  />
                </div>
                <div>
                  <label htmlFor="field-billDueDate" className="block font-bold text-slate-700 mb-1">Hạn đóng tiền</label>
                  <input id="field-billDueDate"
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
                  {serviceFee > 0 && <div>Dịch vụ: {formatPrice(serviceFee)}</div>}
                  {otherFees > 0 && <div>Phụ phí: {formatPrice(otherFees)}</div>}
                </div>
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  disabled={submittingBill} onClick={() => setBillModalOpen(false)}
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
            {formErrors.bill && <p role="alert" className="form-error">{formErrors.bill}</p>}
            </form>
        </Modal>
      )}

      {/* MODAL 2: EDIT BILL */}
      {editModalOpen && editingBill && (
        <Modal isOpen={editModalOpen} onClose={() => { if (!submittingEdit) setEditModalOpen(false); }} title={`Cập nhật hóa đơn #${editingBill.id} (${editingBill.thang}/${editingBill.nam})`} size="lg">
            <form onSubmit={handleUpdateBill} className="space-y-3 text-xs">
              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label htmlFor="field-editOldElec" className="block text-[10px] font-bold text-slate-700 mb-1">Số điện cũ</label>
                  <input id="field-editOldElec"
                    type="number"
                    min={0}
                    value={editOldElec}
                    onChange={(e) => setEditOldElec(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
                <div>
                  <label htmlFor="field-editNewElec" className="block text-[10px] font-bold text-slate-700 mb-1">Số điện mới</label>
                  <input id="field-editNewElec"
                    type="number"
                    min={0}
                    value={editNewElec}
                    onChange={(e) => setEditNewElec(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 font-bold text-amber-600"
                  />
                </div>
                <div>
                  <label htmlFor="field-editPriceElec" className="block text-[10px] font-bold text-slate-700 mb-1">Đơn giá điện</label>
                  <input id="field-editPriceElec"
                    type="number"
                    value={editPriceElec}
                      readOnly
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 bg-slate-100"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-2">
                <div>
                  <label htmlFor="field-editOldWater" className="block text-[10px] font-bold text-slate-700 mb-1">Số nước cũ</label>
                  <input id="field-editOldWater"
                    type="number"
                    min={0}
                    value={editOldWater}
                    onChange={(e) => setEditOldWater(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
                <div>
                  <label htmlFor="field-editNewWater" className="block text-[10px] font-bold text-slate-700 mb-1">Số nước mới</label>
                  <input id="field-editNewWater"
                    type="number"
                    min={0}
                    value={editNewWater}
                    onChange={(e) => setEditNewWater(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 font-bold text-blue-600"
                  />
                </div>
                <div>
                  <label htmlFor="field-editPriceWater" className="block text-[10px] font-bold text-slate-700 mb-1">Đơn giá nước</label>
                  <input id="field-editPriceWater"
                    type="number"
                    value={editPriceWater}
                      readOnly
                      className="w-full px-3 py-1.5 rounded-xl border border-slate-200 bg-slate-100"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-2">
                <div>
                  <label htmlFor="field-editRoomPrice" className="block text-[10px] font-bold text-slate-700 mb-1">Tiền phòng (VNĐ)</label>
                  <input id="field-editRoomPrice"
                    type="number"
                    value={editRoomPrice}
                    readOnly
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200 bg-slate-100 font-bold"
                  />
                </div>
                <div>
                  <label htmlFor="field-editOtherFees" className="block text-[10px] font-bold text-slate-700 mb-1">Phụ phí (VNĐ)</label>
                  <input id="field-editOtherFees"
                    type="number"
                    min={0}
                    value={editOtherFees}
                    onChange={(e) => setEditOtherFees(Number(e.target.value))}
                    className="w-full px-3 py-1.5 rounded-xl border border-slate-200"
                  />
                </div>
              </div>

              <div className="bg-slate-50 p-3 rounded-xl border border-slate-200 flex justify-between items-center font-bold">
                <span>Tổng tiền tính lại:</span>
                <span className="text-base text-[#0084ff]">{formatPrice(editCalculatedTotalAmount)}</span>
              </div>

              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  disabled={submittingEdit} onClick={() => setEditModalOpen(false)}
                  className="px-4 py-2 rounded-xl text-slate-600 hover:bg-slate-100 font-semibold"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  disabled={submittingEdit}
                  className="px-5 py-2 rounded-xl font-bold bg-[#0084ff] hover:bg-[#0073e6] text-white shadow-md cursor-pointer"
                >
                  {submittingEdit ? 'Đang lưu...' : 'Lưu thay đổi'}
                </button>
              </div>
            {formErrors.edit && <p role="alert" className="form-error">{formErrors.edit}</p>}
            </form>
        </Modal>
      )}

      {/* MODAL 3: CREATE CONTRACT */}
      {contractModalOpen && (
        <Modal isOpen={contractModalOpen} onClose={() => { if (!submittingContract) setContractModalOpen(false); }} title="Tạo hợp đồng thuê phòng">
            <form onSubmit={handleCreateContract} className="space-y-3 text-xs">
              <div>
                <label htmlFor="field-contractStartDate" className="block font-bold text-slate-700 mb-1">Ngày bắt đầu *</label>
                <input id="field-contractStartDate"
                  type="date"
                  value={contractStartDate}
                  onChange={(e) => setContractStartDate(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  required
                />
              </div>
              <div>
                <label htmlFor="field-contractEndDate" className="block font-bold text-slate-700 mb-1">Ngày thuê cuối cùng *</label>
                <input id="field-contractEndDate"
                  type="date"
                  value={contractEndDate}
                  min={contractStartDate}
                  onChange={(e) => setContractEndDate(e.target.value)}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs"
                  required
                />
              </div>
              <div>
                <label htmlFor="field-contractRent" className="block font-bold text-slate-700 mb-1">Tiền thuê hàng tháng (VNĐ) *</label>
                <input id="field-contractRent"
                  type="number"
                  value={contractRent}
                  min="1"
                  onChange={(e) => setContractRent(Number(e.target.value))}
                  className="w-full px-3 py-2 rounded-xl border border-slate-200 text-xs font-bold"
                  required
                />
              </div>
              <div className="flex justify-end gap-2 pt-2">
                <button
                  type="button"
                  disabled={submittingContract} onClick={() => setContractModalOpen(false)}
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
            {formErrors.contract && <p role="alert" className="form-error">{formErrors.contract}</p>}
            </form>
        </Modal>
      )}
      {dialog}
      </div>
    </div>
  );
};

export default LandlordContractsPage;
