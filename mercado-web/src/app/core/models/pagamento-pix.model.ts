import { StatusPagamento } from '../enums/status-pagamento.model';

export interface ItemVendaPix {
  produtoId: number;
  quantidade: number;
}

export interface IniciarPagamentoPix {
  itens: ItemVendaPix[];
}

export interface PagamentoPixResposta {
  vendaId: number;
  valorTotal: number;
  qrCode: string;
  qrCodeBase64: string;
  expiraEm: string;
}

export interface StatusPagamentoPixResposta {
  status: StatusPagamento;
}
