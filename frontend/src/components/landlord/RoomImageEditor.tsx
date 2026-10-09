import { useId, useState } from 'react';
import { FiImage, FiPlus, FiTrash2 } from 'react-icons/fi';
import { isRoomImageUrl } from '../../utils/roomImageUrls';
import './RoomImageEditor.css';

function ImagePreview({ url, alt }: { url: string; alt: string }) {
  const [status, setStatus] = useState<'loading' | 'loaded' | 'error'>('loading');
  const invalid = !isRoomImageUrl(url);
  return <div className="room-image-preview" data-image-state={invalid ? 'error' : status}>
    {!invalid && <img src={url} alt={alt} hidden={status !== 'loaded'} onLoad={() => setStatus('loaded')} onError={() => setStatus('error')} />}
    {status === 'loading' && !invalid && <span role="status"><FiImage />Đang tải ảnh...</span>}
    {(status === 'error' || invalid) && <span role="alert"><FiImage />Không thể tải ảnh từ URL này.</span>}
  </div>;
}

export default function RoomImageEditor({ imageUrls, onChange, disabled = false }: {
  imageUrls: string[];
  onChange: (urls: string[]) => void;
  disabled?: boolean;
}) {
  const fieldId = useId();
  const [draft, setDraft] = useState('');
  const url = draft.trim();
  const valid = isRoomImageUrl(url);
  const duplicate = imageUrls.some(image => image.trim() === url);
  const error = url && !valid ? 'Chỉ chấp nhận http://, https:// hoặc /uploads/...; không chấp nhận Base64 (tối đa 1000 ký tự).'
    : url && duplicate ? 'URL ảnh này đã có trong danh sách.' : '';
  const addImage = () => {
    if (disabled || !valid || duplicate) return;
    onChange([...imageUrls, url]);
    setDraft('');
  };
  return <fieldset className="room-image-editor" disabled={disabled}>
    <legend>Ảnh phòng</legend>
    <label htmlFor={fieldId}>URL ảnh</label>
    <div className="room-image-input-row">
      <input id={fieldId} type="text" inputMode="url" autoComplete="off" placeholder="https://... hoặc /uploads/..." value={draft}
        aria-invalid={!!error} aria-describedby={error ? `${fieldId}-error` : undefined}
        onChange={event => setDraft(event.target.value)} onKeyDown={event => { if (event.key === 'Enter') { event.preventDefault(); addImage(); } }} />
      <button type="button" className="landlord-button" onClick={addImage} disabled={!valid || duplicate || disabled}><FiPlus />Thêm ảnh</button>
    </div>
    {error && <p id={`${fieldId}-error`} role="alert" className="room-image-error">{error}</p>}
    {url && valid && <div className="room-image-draft"><ImagePreview key={url} url={url} alt="Ảnh xem trước từ URL" /></div>}
    <div className="room-image-grid">
      {imageUrls.map((image, index) => <article key={image} className="room-image-item">
        <ImagePreview key={image} url={image.trim()} alt={`Ảnh phòng ${index + 1}`} />
        <div className="room-image-item-actions">
          <label><input type="radio" name={`${fieldId}-thumbnail`} checked={index === 0} aria-label={`Chọn ảnh ${index + 1} làm ảnh đại diện`}
            onChange={() => onChange([image, ...imageUrls.filter((_, position) => position !== index)])} />
            {index === 0 ? 'Ảnh đại diện' : 'Chọn đại diện'}
          </label>
          <button type="button" className="landlord-icon-button danger" title="Xóa ảnh" aria-label={`Xóa ảnh ${index + 1}`}
            onClick={() => onChange(imageUrls.filter((_, position) => position !== index))}><FiTrash2 /></button>
        </div>
      </article>)}
    </div>
  </fieldset>;
}
