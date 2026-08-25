import { FormaPagamento } from "../enums/forma-pagamento.model";

export interface ItemVenda {
  produtoId: number;
  quantidade: number;
}

export interface CriarVenda {
  itens: ItemVenda[];
  formaPagamento: FormaPagamento;
}