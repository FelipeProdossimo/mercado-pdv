import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Produto } from '../models/produto.model';
import { PaginacaoResposta } from '../models/paginacao-resposta.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class ProdutoService {
  private http = inject(HttpClient);

  obterTodos() {
    return this.http.get<PaginacaoResposta<Produto>>(
      `${environment.apiUrl}/produtos?pagina=1&tamanhoPagina=50`,
    );
  }

  excluir(id: number) {
    return this.http.delete(`${environment.apiUrl}/produtos/${id}`);
  }

  obterPorId(id: number) {
    return this.http.get<Produto>(`${environment.apiUrl}/produtos/${id}`);
  }

  adicionar(produto: any) {
    return this.http.post<number>(`${environment.apiUrl}/produtos`, produto);
  }

  editar(id: number, produto: any) {
    return this.http.put(`${environment.apiUrl}/produtos/${id}`, produto);
  }

  obterTodosSemPaginacao() {
    return this.http.get<any>(`${environment.apiUrl}/produtos?pagina=1&tamanhoPagina=999`);
  }

  uploadImagem(id: number, arquivo: File) {
    const formData = new FormData();

    formData.append('arquivo', arquivo);

    return this.http.post(`${environment.apiUrl}/produtos/${id}/upload-imagem`, formData);
  }
}
