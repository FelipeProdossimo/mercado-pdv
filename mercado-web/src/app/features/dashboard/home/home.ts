import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Dashboard } from '../../../core/models/dashboard.model';
import { DashboardService } from '../../../core/services/dashboard';
import { RealtimeService } from '../../../core/services/realtime';
import { GraficoVenda } from '../../../core/models/grafico-venda.model';
import ApexCharts from 'apexcharts';
import { ProdutoMaisVendido } from '../../../core/models/produto-mais-vendido.model';
import { ProdutoEstoqueBaixo } from '../../../core/models/produto-estoque-baixo.model';

@Component({
  selector: 'app-home',
  imports: [CommonModule],
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private realtimeService = inject(RealtimeService);
  public dashboard?: Dashboard;
  public chart?: ApexCharts;
  public maisVendidos: ProdutoMaisVendido[] = [];
  public estoqueBaixo: ProdutoEstoqueBaixo[] = [];

  ngOnInit(): void {
    this.carregarDashboard();
    this.realtimeService.iniciarConexao();

    this.realtimeService.escutarVendaCriada(() => {
      this.carregarDashboard();
    });
  }

  carregarDashboard(): void {
    this.dashboardService.obterDashboard().subscribe({
      next: (response) => {
        this.dashboard = response;

        setTimeout(() => {
          this.carregarGraficoMensal();
        });
      },
    });

    this.dashboardService.obterMaisVendidos().subscribe({
      next: (response) => {
        this.maisVendidos = response;
      },
    });

    this.dashboardService.obterEstoqueBaixo().subscribe({
      next: (response) => {
        this.estoqueBaixo = response;
      },
    });
  }

  carregarGraficoMensal(): void {
    this.dashboardService.obterGraficoMensal().subscribe({
      next: (response) => {
        this.carregarGrafico(response);
      },
    });
  }

  carregarGrafico(dados: GraficoVenda[]): void {
    const options = {
      chart: {
        type: 'line' as const,
        height: 350,
        toolbar: {
          show: false,
        },
        background: '#111827',
      },
      theme: {
        mode: 'dark' as const,
      },
      series: [
        {
          name: 'Vendas',
          data: dados.map((x) => x.total),
        },
      ],
      xaxis: {
        categories: dados.map((x) => x.mes),
      },
    };

    if (this.chart) {
      this.chart?.destroy();
    }

    const elemento = document.querySelector('#grafico-vendas') as HTMLElement;

    if (!elemento) {
      return;
    }

    this.chart = new ApexCharts(elemento, options);

    this.chart.render();
  }
}
