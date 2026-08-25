import { StatusPagamento } from '../enums/status-pagamento.model';

export interface VendaListagem {
  id: number;
  dataVenda: string;
  valorTotal: number;
  statusPagamento: StatusPagamento;
}