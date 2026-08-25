export interface Produto {
  id: number;
  descricao: string;
  valor: number;
  estoque: number;
  codigoBarras: string;
  imagemUrl?: string;
}