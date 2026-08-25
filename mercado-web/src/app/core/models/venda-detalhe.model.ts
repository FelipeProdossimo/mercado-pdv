import { StatusPagamento } from '../enums/status-pagamento.model';

export interface ItemVendaDetalhe {
  produtoId: number;
  descricaoProduto: string;
  quantidade: number;
  valorUnitario: number;
  total: number;
}

export interface VendaDetalhe {
  id: number;
  dataVenda: string;
  valorTotal: number;
  statusPagamento: StatusPagamento;
  itens: ItemVendaDetalhe[];
}