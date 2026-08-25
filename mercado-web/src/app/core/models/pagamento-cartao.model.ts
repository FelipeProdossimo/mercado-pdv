import { FormaPagamento } from '../enums/forma-pagamento.model';
import { StatusPagamento } from '../enums/status-pagamento.model';

export interface ItemVendaCartao {
  produtoId: number;
  quantidade: number;
}

export interface ProcessarPagamentoCartao {
  itens: ItemVendaCartao[];
  formaPagamento: FormaPagamento;
  cardToken: string;
  paymentMethodId: string;
  parcelas: number;
  cpfCliente?: string;
}

export interface PagamentoCartaoResposta {
  vendaId: number;
  valorTotal: number;
  statusPagamento: StatusPagamento;
  motivoRecusa?: string;
}
