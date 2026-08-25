export interface PaginacaoResposta<T> {
  dados: T[];
  totalRegistros: number;
  pagina: number;
  tamanhoPagina: number;
}