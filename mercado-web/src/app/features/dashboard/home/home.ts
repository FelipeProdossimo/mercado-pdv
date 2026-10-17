import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { Dashboard } from '../../../core/models/dashboard.model';
import { DashboardService } from '../../../core/services/dashboard';
import { RealtimeService } from '../../../core/services/realtime';
import { GraficoVenda } from '../../../core/models/grafico-venda.model';
import ApexCharts from 'apexcharts';
import { ProdutoMaisVendido } from '../../../core/models/produto-mais-vendido.model';
import { ProdutoEstoqueBaixo } from '../../../core/models/produto-estoque-baixo.model';

@Component({
  selector: 'app-home',
  imports: [CommonModule, RouterLink],
  templateUrl: './home.html',
})
export class HomeComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private realtimeService = inject(RealtimeService);
  public dashboard?: Dashboard;
  public chart?: ApexCharts;
  public maisVendidos: ProdutoMaisVendido[] = [];
  public estoqueBaixo: ProdutoEstoqueBaixo[] = [];
  public readonly hoje = new Date();

  get ticketMedio(): number {
    const vendas = this.dashboard?.quantidadeVendasHoje ?? 0;
    return vendas ? this.dashboard!.totalVendidoHoje / vendas : 0;
  }

  // Largura da barra de cada item em relação ao primeiro colocado.
  percentualMaisVendido(quantidade: number): number {
    const maior = this.maisVendidos[0]?.quantidadeVendida ?? 0;
    return maior ? (quantidade / maior) * 100 : 0;
  }

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
    const moeda = (valor: number) =>
      valor.toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });

    const options = {
      chart: {
        type: 'area' as const,
        height: 320,
        toolbar: { show: false },
        zoom: { enabled: false },
        background: 'transparent',
        fontFamily: 'Inter, sans-serif',
        foreColor: '#8fa0bd',
      },
      theme: { mode: 'dark' as const },
      colors: ['#10b981'],
      stroke: { curve: 'smooth' as const, width: 3 },
      fill: {
        type: 'gradient',
        gradient: { shadeIntensity: 1, opacityFrom: 0.45, opacityTo: 0.02, stops: [0, 95] },
      },
      dataLabels: { enabled: false },
      markers: { size: 5, strokeWidth: 0, hover: { size: 7 } },
      grid: { borderColor: '#233150', strokeDashArray: 4 },
      series: [
        {
          name: 'Faturamento',
          data: dados.map((x) => x.total),
        },
      ],
      xaxis: {
        categories: dados.map((x) => x.mes),
        axisBorder: { show: false },
        axisTicks: { show: false },
      },
      yaxis: { labels: { formatter: moeda } },
      tooltip: { y: { formatter: moeda } },
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
