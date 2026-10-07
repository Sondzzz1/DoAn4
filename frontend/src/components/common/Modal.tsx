import React, { ReactNode, useEffect, useId, useRef } from 'react';
import { createPortal } from 'react-dom';
import { FiX } from 'react-icons/fi';

interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title?: string;
  children: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  bodyClassName?: string;
}

const Modal: React.FC<ModalProps> = ({ isOpen, onClose, title, children, size = 'md', bodyClassName = 'p-4 sm:p-6' }) => {
  const titleId = useId();
  const panelRef = useRef<HTMLDivElement>(null);
  const closeRef = useRef(onClose);
  useEffect(() => { closeRef.current = onClose; }, [onClose]);

  useEffect(() => {
    if (!isOpen) return;
    const previouslyFocused = document.activeElement as HTMLElement | null;
    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    const panel = panelRef.current;
    panel?.focus();
    const handleEscape = (e: KeyboardEvent) => {
      if (e.key === 'Escape') {
        closeRef.current();
      }
      if (e.key === 'Tab' && panel) {
        const items = [...panel.querySelectorAll<HTMLElement>('button:not(:disabled), input:not(:disabled), select:not(:disabled), textarea:not(:disabled), a[href], [tabindex="0"]')].filter(el => el.getClientRects().length);
        const first = items[0];
        const last = items[items.length - 1];
        if (!first) { e.preventDefault(); return; }
        if (e.shiftKey && (document.activeElement === first || document.activeElement === panel)) { e.preventDefault(); last.focus(); }
        else if (!e.shiftKey && (document.activeElement === last || document.activeElement === panel)) { e.preventDefault(); first.focus(); }
      }
    };
    document.addEventListener('keydown', handleEscape);

    return () => {
      document.removeEventListener('keydown', handleEscape);
      document.body.style.overflow = previousOverflow;
      previouslyFocused?.focus();
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const sizeClasses = {
    sm: 'max-w-md',
    md: 'max-w-lg',
    lg: 'max-w-2xl',
    xl: 'max-w-4xl',
  };

  return createPortal(
    <div className="ui-modal-backdrop fixed inset-0 z-[2000] flex items-center justify-center p-3 sm:p-6">
      {/* Backdrop */}
      <div
        className="absolute inset-0 bg-slate-900/50 transition-opacity"
        onClick={onClose}
      ></div>

      {/* Modal */}
      <div ref={panelRef} role="dialog" aria-modal="true" aria-labelledby={title ? titleId : undefined} aria-label={title ? undefined : 'Hộp thoại'} tabIndex={-1} className={`ui-modal-panel relative min-w-0 bg-white rounded-lg shadow-xl w-full max-h-[90dvh] overflow-y-auto ${sizeClasses[size]}`}>
        {/* Header */}
        {title && (
          <div className="sticky top-0 z-10 flex items-center justify-between gap-3 bg-white p-4 sm:px-6 border-b border-slate-200">
            <h2 id={titleId} className="min-w-0 text-lg font-semibold text-gray-900">{title}</h2>
            <button
              onClick={onClose}
              type="button"
              className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md text-gray-500 hover:bg-slate-100 hover:text-gray-700 transition-colors"
              aria-label="Đóng"
            >
              <FiX className="h-6 w-6" />
            </button>
          </div>
        )}

        {/* Content */}
        <div className={bodyClassName}>{children}</div>
      </div>
    </div>,
    document.body
  );
};

export default Modal;
