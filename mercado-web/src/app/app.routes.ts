import { Routes } from '@angular/router';
import { Login } from './features/auth/login/login';
import { HomeComponent } from './features/dashboard/home/home';
import { AdminLayout } from './layout/admin-layout/admin-layout';
import { ListaProdutos } from './features/produtos/lista-produtos/lista-produtos';
import { ListaVendas } from './features/vendas/lista-vendas/lista-vendas';
import { Relatorios } from './features/relatorios/relatorios/relatorios';
import { FormProduto } from './features/produtos/form-produto/form-produto';
import { NovaVenda } from './features/vendas/nova-venda/nova-venda';
import { DetalheVenda } from './features/vendas/detalhe-venda/detalhe-venda';

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
        component: HomeComponent,
      },

      {
        path: 'produtos',
        component: ListaProdutos,
      },

      {
        path: 'vendas',
        component: ListaVendas,
      },

      {
        path: 'relatorios',
        component: Relatorios,
      },
      {
        path: 'produtos/novo',
        component: FormProduto,
      },
      {
        path: 'produtos/editar/:id',
        component: FormProduto,
      },
      {
        path: 'vendas/nova',
        component: NovaVenda,
      },
      {
        path: 'vendas/:id',
        component: DetalheVenda,
      },
    ],
  },
];
