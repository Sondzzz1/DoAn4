import React from 'react';
import { FiAlertCircle, FiInbox, FiLoader } from 'react-icons/fi';

interface PageStateProps {
  type: 'loading' | 'empty' | 'error';
  message: string;
  onRetry?: () => void;
}

const PageState: React.FC<PageStateProps> = ({ type, message, onRetry }) => {
  const Icon = type === 'loading' ? FiLoader : type === 'error' ? FiAlertCircle : FiInbox;
  return (
    <div className="rounded-lg border border-slate-200 bg-white px-5 py-12 text-center text-slate-500">
      <Icon className={`mx-auto mb-3 text-3xl ${type === 'loading' ? 'animate-spin text-blue-500' : ''}`} />
      <p className="text-sm">{message}</p>
      {type === 'error' && onRetry && (
        <button onClick={onRetry} className="mt-4 rounded-md bg-blue-600 px-4 py-2 text-sm font-semibold text-white hover:bg-blue-700">
          Thử lại
        </button>
      )}
    </div>
  );
};

export default PageState;
