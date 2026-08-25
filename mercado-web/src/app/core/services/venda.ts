import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CriarVenda } from '../models/criar-venda.model';
import { environment } from '../../../environments/environment';
import { VendaListagem } from '../models/venda-listagem.model';
import { VendaDetalhe } from '../models/venda-detalhe.model';
import {
  IniciarPagamentoPix,
  PagamentoPixResposta,
  StatusPagamentoPixResposta,
} from '../models/pagamento-pix.model';
import {
  PagamentoCartaoResposta,
  ProcessarPagamentoCartao,
} from '../models/pagamento-cartao.model';

@Injectable({
  providedIn: 'root',
})
export class VendaService {
  private http = inject(HttpClient);

  criar(venda: CriarVenda) {
    return this.http.post(`${environment.apiUrl}/vendas`, venda);
  }

  obterTodas() {
    return this.http.get<VendaListagem[]>(`${environment.apiUrl}/vendas`);
  }

  obterPorId(id: number) {
    return this.http.get<VendaDetalhe>(`${environment.apiUrl}/vendas/${id}`);
  }

  iniciarPagamentoPix(dto: IniciarPagamentoPix) {
    return this.http.post<PagamentoPixResposta>(`${environment.apiUrl}/vendas/pix/iniciar`, dto);
  }

  consultarStatusPix(vendaId: number) {
    return this.http.get<StatusPagamentoPixResposta>(
      `${environment.apiUrl}/vendas/pix/${vendaId}/status`,
    );
  }

  cancelarPagamentoPix(vendaId: number) {
    return this.http.post(`${environment.apiUrl}/vendas/pix/${vendaId}/cancelar`, {});
  }

  processarPagamentoCartao(dto: ProcessarPagamentoCartao) {
    return this.http.post<PagamentoCartaoResposta>(
      `${environment.apiUrl}/vendas/cartao/processar`,
      dto,
    );
  }
}
