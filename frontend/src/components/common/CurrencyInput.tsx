import { useRef, useState, type ComponentPropsWithoutRef } from 'react';
import { flushSync } from 'react-dom';
import Input from './Input';

type CurrencyInputProps = Omit<ComponentPropsWithoutRef<typeof Input>, 'value' | 'onChange' | 'type' | 'inputMode'> & {
  value: string;
  onValueChange: (value: string) => void;
};

const normalize = (value: string) => value.replace(/\./g, '').replace(/^0+(?=\d)/, '');
const formatAmount = (value: string) => value.replace(/\B(?=(\d{3})+(?!\d))/g, '.');

export default function CurrencyInput({ value, onValueChange, onFocus, onBlur, onCompositionStart, onCompositionEnd, ...props }: CurrencyInputProps) {
  const composing = useRef(false);
  const [draft, setDraft] = useState<string | null>(null);
  const formatted = formatAmount(value);
  const preview = draft !== null && /^[\d.]*$/.test(draft) ? normalize(draft) : value;

  return (
    <Input {...props} type="text" inputMode="numeric" value={draft ?? formatted}
      helperText={preview ? `Tương ứng: ${formatAmount(preview)} đồng` : props.helperText}
      onFocus={event => {
        const input = event.currentTarget;
        // Finish the one-time focus transition before the browser processes typing/paste.
        flushSync(() => setDraft(value));
        input.select();
        onFocus?.(event);
      }}
      onChange={event => {
        const next = event.currentTarget.value;
        if (composing.current || (event.nativeEvent as InputEvent).isComposing) {
          setDraft(next);
          return;
        }
        if (!/^[\d.]*$/.test(next)) return;
        // Leave the browser's editing text untouched so typing never moves the caret.
        setDraft(next);
        onValueChange(normalize(next));
      }}
      onCompositionStart={event => {
        composing.current = true;
        onCompositionStart?.(event);
      }}
      onCompositionEnd={event => {
        composing.current = false;
        const next = event.currentTarget.value;
        if (/^[\d.]*$/.test(next)) {
          setDraft(next);
          onValueChange(normalize(next));
        } else {
          setDraft(value);
        }
        onCompositionEnd?.(event);
      }}
      onBlur={event => {
        composing.current = false;
        if (/^[\d.]*$/.test(event.currentTarget.value)) onValueChange(normalize(event.currentTarget.value));
        setDraft(null);
        onBlur?.(event);
      }} />
  );
}
