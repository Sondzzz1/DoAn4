import { useEffect, useRef, useState, type FormEvent } from 'react';
import { FiArrowLeft, FiCheck, FiFileText, FiPlus, FiRefreshCw, FiTrash2 } from 'react-icons/fi';
import { toast } from 'react-toastify';
import Modal from '../common/Modal';
import { contractClosureService, type ClosureCommand, type ClosureView, type DamageInput } from '../../services/contractClosureService';
import { getApiErrorMessage } from '../../utils/apiError';
import { formatPrice } from '../../utils/helpers';
import './ContractClosure.css';

const labels: Record<string, string> = {
  'request-end': 'Đề nghị kết thúc / xác minh ngày kết thúc', 'agree-end': 'Đồng ý kết thúc', 'reject-end': 'Từ chối đề nghị', 'withdraw-end': 'Rút đề nghị',
  'save-handover': 'Lập / sửa biên bản bàn giao', 'confirm-handover': 'Xác nhận bàn giao', 'dispute-handover': 'Chưa thống nhất bàn giao',
  'finalize-bills': 'Chốt công nợ cuối', 'create-settlement': 'Lập phương án tất toán', 'propose-settlement': 'Gửi phương án',
  'agree-settlement': 'Đồng ý phương án', 'dispute-settlement': 'Chưa thống nhất tất toán', 'report-refund': 'Báo đã chuyển tiền hoàn',
  'confirm-refund': 'Xác nhận đã nhận tiền hoàn', 'report-extra': 'Báo đã trả khoản bổ sung', 'confirm-extra': 'Xác nhận đã nhận khoản bổ sung',
  'complete-settlement': 'Hoàn tất quyết toán', release: 'Xác nhận sẵn sàng cho thuê', mediate: 'Ghi nhận hỗ trợ đối soát',
  'contract-ended': 'Hợp đồng kết thúc hiệu lực', 'unsigned-cancelled': 'Hợp đồng chờ ký bị hủy', 'billing-invalidated': 'Công nợ thay đổi', 'contract-activated': 'Hợp đồng bắt đầu hiệu lực',
  'contract-created': 'Hợp đồng được tạo', 'contract-signed': 'Xác nhận ký hợp đồng',
  'resume-settlement': 'Đồng ý lại cùng phương án', 'related-room-release': 'Đã xử lý các hồ sơ kết thúc cùng phòng',
};
const handovers = ['Chưa ghi nhận', 'Đang lập', 'Chờ hai bên xác nhận', 'Hai bên đã xác nhận', 'Chưa thống nhất'];
const settlements = ['Bản nháp', 'Chờ đồng thuận', 'Hai bên đã đồng ý', 'Đã báo chuyển tiền', 'Đã tất toán', 'Đang tranh chấp', 'Đã được thay thế'];
const endStates = ['Chờ phản hồi', 'Đã đồng ý, chờ hiệu lực', 'Đã từ chối', 'Đã rút', 'Đã được thay thế', 'Đã có hiệu lực'];
const utc = (s: string) => /Z$|[+-]\d{2}:\d{2}$/.test(s) ? s : `${s}Z`;
const time = (s: string | null | undefined) => s ? new Date(utc(s)).toLocaleString('vi-VN', { timeZone: 'Asia/Ho_Chi_Minh' }) : 'Chưa ghi nhận';
const inputTime = (value?: string) => new Date(value ? utc(value) : Date.now()).toLocaleString('sv-SE', { timeZone: 'Asia/Ho_Chi_Minh' }).replace(' ', 'T').slice(0, 16);
const urls = (value: string) => value.split('\n').map(s => s.trim()).filter(Boolean);
function Evidence({ value }: { value: string | null }) {
  let links: string[];
  try { links = JSON.parse(value || '[]') as string[]; } catch { return null; }
  return <span className="cc-evidence">{links.filter(s => /^https?:\/\//.test(s) || s.startsWith('/uploads/')).map((s, i) => <a key={s} href={s} target="_blank" rel="noreferrer">Chứng cứ {i + 1}</a>)}</span>;
}

export default function ContractClosurePanel({ contractId, onChanged }: { contractId: number; onChanged?: () => Promise<void> | void }) {
  const [open, setOpen] = useState(false);
  const [view, setView] = useState<ClosureView | null>(null);
  const [action, setAction] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [note, setNote] = useState('');
  const [at, setAt] = useState(inputTime);
  const [kind, setKind] = useState(0);
  const [condition, setCondition] = useState('');
  const [electricity, setElectricity] = useState('');
  const [water, setWater] = useState('');
  const [damageNotes, setDamageNotes] = useState('');
  const [evidence, setEvidence] = useState('');
  const [reference, setReference] = useState('');
  const [damages, setDamages] = useState<DamageInput[]>([]);
  const retry = useRef<{ payload: string; key: string } | null>(null);
  const errorRef = useRef<HTMLParagraphElement>(null);
  useEffect(() => { if (error) errorRef.current?.scrollIntoView({ block: 'nearest' }); }, [error]);
  const reload = async () => {
    setLoading(true); setError('');
    try { setView(await contractClosureService.get(contractId)); }
    catch (e) { setError(getApiErrorMessage(e, 'Không tải được hồ sơ kết thúc.')); }
    finally { setLoading(false); }
  };
  const begin = (op: string) => {
    const h = view?.closure;
    setAction(op); setError(''); setNote(''); setReference(''); setDamages([]); setAt(inputTime(op === 'save-handover' ? h?.actualHandoverAtUtc ?? undefined : undefined)); setKind(h?.handoverKind ?? 0);
    setCondition(h?.condition ?? ''); setElectricity(h?.closingElectricity?.toString() ?? ''); setWater(h?.closingWater?.toString() ?? '');
    setDamageNotes(h?.damageNotes ?? ''); setEvidence(op === 'save-handover' && h?.evidenceJson ? (JSON.parse(h.evidenceJson) as string[]).join('\n') : ''); retry.current = null;
  };
  const submit = async (event: FormEvent) => {
    event.preventDefault(); if (!view || !action || busy) return;
    const command: Omit<ClosureCommand, 'idempotencyKey'> = { rowVersion: view.closure?.rowVersion, note, evidenceUrls: urls(evidence) };
    if (action.endsWith('-end')) command.targetId = view.endRequests.find(x => x.status === 0)?.id;
    else command.targetId = view.settlements.find(x => x.status !== 6)?.id;
    if (action === 'request-end') command.effectiveEndAt = `${at}:00+07:00`;
    if (action === 'save-handover') {
      Object.assign(command, { handoverKind: kind, condition, damageNotes });
      if (kind === 0) Object.assign(command, { actualHandoverAt: `${at}:00+07:00`, closingElectricity: Number(electricity), closingWater: Number(water) });
    }
    if (action === 'create-settlement') command.damages = damages;
    if (action === 'report-refund' || action === 'report-extra') command.reference = reference;
    const payload = JSON.stringify(command);
    if (retry.current?.payload !== payload) retry.current = { payload, key: crypto.randomUUID() };
    setBusy(true); setError('');
    try {
      const result = await contractClosureService.execute(contractId, action, { ...command, idempotencyKey: retry.current!.key });
      setView(result); setAction(null); setOpen(false);
      toast.success('Đã cập nhật hồ sơ kết thúc hợp đồng.');
      await onChanged?.();
    } catch (e) { setError(getApiErrorMessage(e, 'Thao tác chưa thành công.')); }
    finally { setBusy(false); }
  };
  const h = view?.closure;
  const needsNote = action === 'request-end' || action === 'mediate' || action?.startsWith('dispute-') || action === 'reject-end';
  return <>
    <button type="button" className="cc-open" aria-label={`Kết thúc và trả phòng hợp đồng #${contractId}`} onClick={() => { setOpen(true); setAction(null); void reload(); }}><FiFileText /> Kết thúc &amp; trả phòng</button>
    <Modal isOpen={open} onClose={() => { if (!busy) setOpen(false); }} title={action ? labels[action] : `Hồ sơ kết thúc hợp đồng #${contractId}`} size="xl">
      <div className="contract-closure">
        <div className="cc-toolbar">
          {action && <button type="button" disabled={busy} onClick={() => { setAction(null); setError(''); }} title="Quay lại hồ sơ"><FiArrowLeft /><span>Hồ sơ</span></button>}
          <button type="button" disabled={busy || loading} onClick={() => void reload()} title="Tải lại hồ sơ" aria-label="Tải lại hồ sơ kết thúc"><FiRefreshCw /></button>
        </div>
        {error && !action && <p ref={errorRef} role="alert" className="cc-error">{error}</p>}
        {loading && <p role="status">Đang tải hồ sơ...</p>}
        {view && !action && <>
          {view.needsReview && <p className="cc-warning">Hồ sơ lịch sử cần xác minh. Chưa đủ căn cứ xác nhận đã trả phòng hoặc tất toán.</p>}
          <dl className="cc-summary">
            <div><dt>Kết thúc hiệu lực</dt><dd>{time(h?.effectiveEndAtUtc)}</dd></div>
            <div><dt>Bàn giao</dt><dd>{handovers[h?.handoverStatus ?? 0]}</dd></div>
            <div><dt>Ngày trả thực tế</dt><dd>{h?.handoverKind === 1 ? 'Hai bên khai báo chưa nhận phòng' : time(h?.actualHandoverAtUtc)}</dd></div>
            <div><dt>Chốt công nợ</dt><dd>{time(h?.billingFinalizedAtUtc)}</dd></div>
            <div><dt>Xác nhận xử lý phòng</dt><dd>{h?.releasedAtUtc ? time(h.releasedAtUtc) : 'Chưa xác nhận'}</dd></div>
            <div><dt>Chỉ số điện / nước trả phòng</dt><dd>{h?.closingElectricity ?? '—'} / {h?.closingWater ?? '—'}</dd></div>
          </dl>
          {h?.condition && <p><strong>Tình trạng:</strong> {h.condition}</p>}
          {h?.damageNotes && <p><strong>Hư hỏng:</strong> {h.damageNotes}</p>}
          <section className="cc-section"><h3>Đề nghị kết thúc</h3>
            {view.endRequests.length === 0 ? <p>Chưa có đề nghị.</p> : view.endRequests.map(r => <div className="cc-row" key={r.id}>
              <strong>#{r.id} · {endStates[r.status]}</strong><p>{r.reason}</p><p>Hiệu lực đề nghị: {time(r.proposedEffectiveEndAtUtc)}</p>{r.responseNote && <p>Phản hồi: {r.responseNote}</p>}
            </div>)}
          </section>
          <section className="cc-section"><h3>Hóa đơn và công nợ</h3>
            {view.bills.length === 0 ? <p>Chưa có hóa đơn.</p> : <div className="cc-table-scroll"><table><thead><tr><th>Kỳ</th><th>Điện / nước</th><th>Tổng tiền</th><th>Trạng thái</th></tr></thead><tbody>{view.bills.map(b => <tr key={b.id}><td>{b.month}/{b.year}</td><td>{b.newElectricity} / {b.newWater}</td><td>{formatPrice(b.totalAmount)}</td><td>{['Chưa thanh toán', 'Đã thanh toán', 'Đã hủy', 'Đang thanh toán', 'Quá hạn'][b.status]}</td></tr>)}</tbody></table></div>}
          </section>
          <section className="cc-section"><h3>Tất toán tiền cọc</h3>
            {view.settlements.length === 0 ? <p>Chưa có phương án. Cọc đã nhận chưa đồng nghĩa đã tất toán.</p> : view.settlements.map(p => <div className="cc-settlement" key={p.id}>
              <h4>Phương án {p.revision} · {settlements[p.status]}</h4>
              <dl className="cc-summary"><div><dt>Cọc gốc đã nhận</dt><dd>{formatPrice(p.depositAmountSnapshot)}</dd></div><div><dt>Công nợ / thiệt hại đề xuất</dt><dd>{formatPrice(p.totalCharges)}</dd></div><div><dt>Tiền hoàn</dt><dd>{formatPrice(p.refundDue)}</dd></div><div><dt>Cần trả thêm</dt><dd>{formatPrice(p.extraDue)}</dd></div></dl>
              {p.lines.map(l => <div className="cc-row" key={l.id}><strong>{l.description}</strong><p>{formatPrice(l.amount)} · Dùng từ cọc: {formatPrice(l.depositAppliedAmount)}</p><Evidence value={l.evidenceJson} /></div>)}
              <p>Chủ trọ đồng ý: {time(p.landlordAgreedAtUtc)} · Người thuê đồng ý: {time(p.tenantAgreedAtUtc)}</p>
              {p.refundReference && <p>Chứng từ hoàn: {p.refundReference} · Nhận tiền: {time(p.refundReceivedAtUtc)}</p>}
              {p.extraReference && <p>Chứng từ bổ sung: {p.extraReference} · Nhận tiền: {time(p.extraReceivedAtUtc)}</p>}
            </div>)}
          </section>
          {!h?.releasedAtUtc && view.releaseBlockers.length > 0 && <section className="cc-section cc-warning"><h3>Chưa đủ điều kiện cho thuê lại</h3><ul>{view.releaseBlockers.map(reason => <li key={reason}>{reason}</li>)}</ul></section>}
          <div className="cc-actions">{view.allowedOperations.map(op => <button type="button" key={op} onClick={() => begin(op)}><FiCheck />{labels[op] ?? op}</button>)}</div>
          <section className="cc-section"><h3>Lịch sử xử lý</h3>{view.events.length === 0 ? <p>Chưa có lịch sử.</p> : view.events.map(e => <div className="cc-row" key={e.id}><strong>{labels[e.eventType] ?? e.eventType}</strong><p>{time(e.occurredAtUtc)} · {e.actorAccountId ? `Tài khoản #${e.actorAccountId}` : 'Hệ thống'}</p>{e.reason && <p>{e.reason}</p>}<details><summary>Chứng từ / nội dung đã ghi nhận</summary><pre>{e.snapshotJson ? JSON.stringify(JSON.parse(e.snapshotJson), null, 2) : 'Không có bản đính kèm.'}</pre></details></div>)}</section>
        </>}
        {view && action && <form className="cc-form" onSubmit={event => void submit(event)}>
          {action === 'request-end' && <label>Thời điểm kết thúc đề nghị (giờ Việt Nam)<input type="datetime-local" value={at} onChange={e => setAt(e.target.value)} required /></label>}
          {action === 'save-handover' && <>
            <fieldset className="cc-modes"><legend>Hình thức bàn giao</legend><label><input type="radio" name="handoverKind" checked={kind === 0} onChange={() => setKind(0)} />Đã nhận phòng, trả thực tế</label><label><input type="radio" name="handoverKind" checked={kind === 1} onChange={() => setKind(1)} />Chưa từng nhận phòng</label></fieldset>
            <label>Tình trạng / xác nhận thực tế<textarea value={condition} onChange={e => setCondition(e.target.value)} required minLength={5} maxLength={2000} /></label>
            {kind === 0 && <><label>Thời điểm trả phòng thực tế (giờ Việt Nam)<input type="datetime-local" value={at} onChange={e => setAt(e.target.value)} required /></label><div className="cc-fields"><label>Chỉ số điện trả phòng<input type="number" min="0" step="0.01" value={electricity} onChange={e => setElectricity(e.target.value)} required /></label><label>Chỉ số nước trả phòng<input type="number" min="0" step="0.01" value={water} onChange={e => setWater(e.target.value)} required /></label></div></>}
            <label>Ghi nhận hư hỏng<textarea value={damageNotes} onChange={e => setDamageNotes(e.target.value)} maxLength={2000} /></label>
          </>}
          {action === 'create-settlement' && <>
            <div className="cc-actions"><button type="button" onClick={() => setDamages(current => [...current, { amount: 0, description: '', evidenceUrls: [] }])} disabled={damages.length >= 20}><FiPlus />Khoản thiệt hại đề xuất</button></div>
            {damages.map((d, i) => <fieldset key={i} className="cc-damage"><legend>Khoản {i + 1}</legend><button type="button" title="Xóa khoản thiệt hại" aria-label={`Xóa khoản ${i + 1}`} onClick={() => setDamages(current => current.filter((_, index) => index !== i))}><FiTrash2 /></button>
              <label>Nội dung<textarea required minLength={5} maxLength={1000} value={d.description} onChange={e => setDamages(current => current.map((item, index) => index === i ? { ...item, description: e.target.value } : item))} /></label>
              <label>Số tiền đề xuất<input type="number" min="0.01" step="0.01" required value={d.amount || ''} onChange={e => setDamages(current => current.map((item, index) => index === i ? { ...item, amount: Number(e.target.value) } : item))} /></label>
              <label>URL chứng cứ<textarea required value={d.evidenceUrls.join('\n')} onChange={e => setDamages(current => current.map((item, index) => index === i ? { ...item, evidenceUrls: e.target.value.split('\n') } : item))} /></label>
            </fieldset>)}
          </>}
          {(action === 'save-handover' || action === 'mediate' || action === 'request-end' && view.needsReview) && <label>URL chứng cứ<textarea aria-label="URL chứng cứ" value={evidence} onChange={e => setEvidence(e.target.value)} required={action === 'mediate' || kind === 1 || view.needsReview} /></label>}
          {(action === 'report-refund' || action === 'report-extra') && <label>Chứng từ / mã chuyển tiền<input value={reference} onChange={e => setReference(e.target.value)} required minLength={5} maxLength={500} /></label>}
          <label>{needsNote ? 'Lý do / nội dung xác nhận' : 'Ghi chú'}<textarea value={note} onChange={e => setNote(e.target.value)} required={Boolean(needsNote)} minLength={needsNote ? 5 : undefined} maxLength={2000} /></label>
          {error && <p ref={errorRef} role="alert" className="cc-error">{error}</p>}
          <div className="cc-actions"><button type="button" disabled={busy} onClick={() => { setAction(null); setError(''); }}>Hủy</button><button type="submit" disabled={busy || loading || !view.allowedOperations.includes(action)}><FiCheck />{busy ? 'Đang xử lý...' : 'Xác nhận'}</button></div>
        </form>}
        {!action && <div className="cc-actions"><button type="button" onClick={() => setOpen(false)}>Đóng</button></div>}
      </div>
    </Modal>
  </>;
}

export function AdminContractClosureLookup() {
  const [input, setInput] = useState(''); const [id, setId] = useState<number | null>(null);
  return <section className="contract-closure cc-admin"><h2>Hỗ trợ kết thúc hợp đồng</h2><form onSubmit={e => { e.preventDefault(); setId(Number(input)); }} className="cc-actions"><label>Mã hợp đồng<input type="number" min="1" step="1" value={input} onChange={e => setInput(e.target.value)} required /></label><button type="submit"><FiFileText />Tra cứu</button></form>{id && <ContractClosurePanel key={id} contractId={id} />}</section>;
}
