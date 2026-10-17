import { StatusPagamento } from './status-pagamento.model';

export interface RotuloStatus {
  texto: string;
  classe: string;
}

const ROTULOS_STATUS: Record<StatusPagamento, RotuloStatus> = {
  [StatusPagamento.Pendente]: { texto: 'Pendente', classe: 'bg-amber-500/15 text-amber-300' },
  [StatusPagamento.Aprovado]: { texto: 'Aprovado', classe: 'bg-emerald-500/15 text-emerald-300' },
  [StatusPagamento.Recusado]: { texto: 'Recusado', classe: 'bg-red-500/15 text-red-300' },
  [StatusPagamento.Cancelado]: { texto: 'Cancelado', classe: 'bg-slate-500/15 text-slate-400' },
};

export function rotuloStatus(status: StatusPagamento): RotuloStatus {
  return ROTULOS_STATUS[status] ?? { texto: '—', classe: 'bg-slate-500/15 text-slate-400' };
}
