import React, { InputHTMLAttributes, forwardRef, useId } from 'react';
import clsx from 'clsx';

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label?: string;
  error?: string;
  helperText?: string;
}

const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ label, error, helperText, className, ...props }, ref) => {
    const generatedId = useId();
    const inputId = props.id || generatedId;
    const messageId = `${inputId}-message`;
    return (
      <div className="w-full">
        {label && (
          <label htmlFor={inputId} className="block text-sm font-medium text-gray-700 mb-1">
            {label}
            {props.required && <span className="text-red-500 ml-1">*</span>}
          </label>
        )}
        
        <input
          ref={ref}
          className={clsx(
            'w-full px-4 py-2 border rounded-lg focus:outline-none focus:ring-2 transition-colors',
            error
              ? 'border-red-500 focus:ring-red-500 focus:border-red-500'
              : 'border-gray-300 focus:ring-blue-500 focus:border-blue-500',
            props.disabled && 'bg-gray-100 cursor-not-allowed',
            className
          )}
          {...props}
          id={inputId}
          aria-invalid={!!error}
          aria-describedby={error || helperText ? messageId : props['aria-describedby']}
        />
        
        {error && (
          <p id={messageId} role="alert" className="mt-1 text-sm text-red-600">{error}</p>
        )}
        
        {helperText && !error && (
          <p id={messageId} className="mt-1 text-sm text-gray-500">{helperText}</p>
        )}
      </div>
    );
  }
);

Input.displayName = 'Input';

export default Input;
