import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { AdminLayout } from './layout/admin-layout/admin-layout';

// Cada tela é carregada sob demanda: o ApexCharts, por exemplo, só é baixado
// quando o dashboard é aberto, e a tela de login abre mais rápido.
export const routes: Routes = [
  {
    path: '',
    component: Login,
  },

  {
    path: '',
    component: AdminLayout,

    children: [
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/home/home').then((m) => m.HomeComponent),
      },
      {
        path: 'produtos',
        loadComponent: () =>
          import('./features/produtos/lista-produtos/lista-produtos').then((m) => m.ListaProdutos),
      },
      {
        path: 'produtos/novo',
        loadComponent: () =>
          import('./features/produtos/form-produto/form-produto').then((m) => m.FormProduto),
      },
      {
        path: 'produtos/editar/:id',
        loadComponent: () =>
          import('./features/produtos/form-produto/form-produto').then((m) => m.FormProduto),
      },
      {
        path: 'vendas',
        loadComponent: () =>
          import('./features/vendas/lista-vendas/lista-vendas').then((m) => m.ListaVendas),
      },
      {
        path: 'vendas/nova',
        loadComponent: () =>
          import('./features/vendas/nova-venda/nova-venda').then((m) => m.NovaVenda),
      },
      {
        path: 'vendas/:id',
        loadComponent: () =>
          import('./features/vendas/detalhe-venda/detalhe-venda').then((m) => m.DetalheVenda),
      },
      {
        path: 'relatorios',
        loadComponent: () =>
          import('./features/relatorios/relatorios/relatorios').then((m) => m.Relatorios),
      },
    ],
  },
];
