import api from './api';
import type { ApiResponse } from '../types/common.types';

export interface ClosureRecord {
  id: number; contractId: number; handoverStatus: number; handoverKind: number | null; handoverRevision: number;
  effectiveEndAtUtc: string | null; lastRentalDate: string | null; actualHandoverAtUtc: string | null;
  condition: string | null; damageNotes: string | null; closingElectricity: number | null; closingWater: number | null;
  evidenceJson: string | null;
  tenantHandoverConfirmedAtUtc: string | null; landlordHandoverConfirmedAtUtc: string | null;
  billingFinalizedAtUtc: string | null; releasedAtUtc: string | null; rowVersion: string;
}
export interface EndProposal {
  id: number; status: number; reason: string; requestedByAccountId: number; requestedAtUtc: string;
  proposedEffectiveEndAtUtc: string; responseNote: string | null;
}
export interface SettlementLine { id: number; kind: number; description: string; amount: number; depositAppliedAmount: number; evidenceJson: string | null }
export interface Settlement {
  id: number; revision: number; status: number; depositAmountSnapshot: number; totalCharges: number; refundDue: number; extraDue: number;
  refundReference: string | null; extraReference: string | null; refundReceivedAtUtc: string | null; extraReceivedAtUtc: string | null;
  tenantAgreedAtUtc: string | null; landlordAgreedAtUtc: string | null; completedAtUtc: string | null; lines: SettlementLine[];
}
export interface ClosureView {
  contractId: number; contractStatus: number; needsReview: boolean; closure: ClosureRecord | null; endRequests: EndProposal[]; settlements: Settlement[];
  releaseBlockers: string[]; allowedOperations: string[];
  events: { id: number; eventType: string; actorAccountId: number | null; occurredAtUtc: string; reason: string | null; snapshotJson: string | null }[];
  bills: { id: number; month: number; year: number; status: number; totalAmount: number; newElectricity: number; newWater: number }[];
}
export interface DamageInput { amount: number; description: string; evidenceUrls: string[] }
export interface ClosureCommand {
  idempotencyKey: string; rowVersion?: string; targetId?: number; note?: string; effectiveEndAt?: string;
  handoverKind?: number; actualHandoverAt?: string; condition?: string; closingElectricity?: number; closingWater?: number;
  damageNotes?: string; evidenceUrls?: string[]; damages?: DamageInput[]; reference?: string;
}
export const contractClosureService = {
  get: async (id: number) => (await api.get<ApiResponse<ClosureView>>(`/hop-dong/${id}/ket-thuc`)).data.data!,
  execute: async (id: number, operation: string, command: ClosureCommand) =>
    (await api.post<ApiResponse<ClosureView>>(`/hop-dong/${id}/ket-thuc/${operation}`, command)).data.data!,
};
