import React from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { FiCheckCircle, FiXCircle, FiFileText, FiHome } from 'react-icons/fi';
import { formatPrice } from '../../utils/helpers';

const PaymentResultPage: React.FC = () => {
  const [searchParams] = useSearchParams();

  const success = searchParams.get('success') === 'true';
  const orderId = searchParams.get('orderId') || '';
  const transactionId = searchParams.get('transactionId') || '';
  const amount = Number(searchParams.get('amount')) || 0;
  const message = searchParams.get('message') || '';

  return (
    <div className="max-w-xl mx-auto px-4 py-16">
      <div className="bg-white rounded-3xl border border-slate-200 p-8 md:p-10 shadow-xl text-center">
        {success ? (
          <div className="w-20 h-20 bg-emerald-50 text-emerald-500 rounded-full flex items-center justify-center mx-auto mb-6 text-4xl shadow-inner">
            <FiCheckCircle />
          </div>
        ) : (
          <div className="w-20 h-20 bg-rose-50 text-rose-500 rounded-full flex items-center justify-center mx-auto mb-6 text-4xl shadow-inner">
            <FiXCircle />
          </div>
        )}

        <h1 className="text-2xl font-black text-slate-900 mb-2">
          {success ? 'Thanh toán đặt cọc thành công!' : 'Thanh toán không thành công'}
        </h1>

        <p className="text-sm text-slate-600 mb-6">
          {message || (success ? 'Giao dịch qua cổng VNPay Sandbox đã được xác nhận.' : 'Đã có lỗi xảy ra hoặc bạn đã hủy giao dịch.')}
        </p>

        {amount > 0 && (
          <div className="bg-slate-50 border border-slate-100 rounded-2xl p-4 mb-6 text-left space-y-2 text-xs text-slate-600">
            <div className="flex justify-between">
              <span className="text-slate-400">Số tiền thanh toán:</span>
              <b className="text-sm text-[#0084ff]">{formatPrice(amount)}</b>
            </div>
            {orderId && (
              <div className="flex justify-between">
                <span className="text-slate-400">Mã đơn hàng:</span>
                <span className="font-mono text-slate-800">{orderId}</span>
              </div>
            )}
            {transactionId && (
              <div className="flex justify-between">
                <span className="text-slate-400">Mã giao dịch VNPay:</span>
                <span className="font-mono text-slate-800">{transactionId}</span>
              </div>
            )}
            <div className="flex justify-between">
              <span className="text-slate-400">Phương thức:</span>
              <span className="font-semibold text-slate-800">VNPay QR / Thẻ nội địa</span>
            </div>
          </div>
        )}

        <div className="flex flex-col sm:flex-row gap-3 justify-center">
          <Link
            to="/tenant/rentals"
            className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-[#0084ff] hover:bg-[#0073e6] text-white font-bold text-xs rounded-2xl shadow-md shadow-blue-500/20 transition-all"
          >
            <FiFileText /> Xem hợp đồng & Cọc
          </Link>
          <Link
            to="/"
            className="inline-flex items-center justify-center gap-2 px-6 py-3 bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold text-xs rounded-2xl transition-all"
          >
            <FiHome /> Về trang chủ
          </Link>
        </div>
      </div>
    </div>
  );
};

export default PaymentResultPage;
