import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Dashboard } from '../models/dashboard.model';
import { GraficoVenda } from '../models/grafico-venda.model';
import { ProdutoMaisVendido } from '../models/produto-mais-vendido.model';
import { ProdutoEstoqueBaixo } from '../models/produto-estoque-baixo.model';

@Injectable({
  providedIn: 'root',
})
export class DashboardService {
  private http = inject(HttpClient);

  obterDashboard(): Observable<Dashboard> {
    return this.http.get<Dashboard>(`${environment.apiUrl}/dashboard`);
  }

  obterGraficoMensal() {
    return this.http.get<GraficoVenda[]>(`${environment.apiUrl}/dashboard/grafico-vendas`);
  }

  obterMaisVendidos() {
    return this.http.get<ProdutoMaisVendido[]>(`${environment.apiUrl}/dashboard/mais-vendidos`);
  }

  obterEstoqueBaixo() {
    return this.http.get<ProdutoEstoqueBaixo[]>(`${environment.apiUrl}/dashboard/estoque-baixo`);
  }
}
