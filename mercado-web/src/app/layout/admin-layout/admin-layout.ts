import { Component, DestroyRef, HostListener, OnInit, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth';

interface ItemMenu {
  rota: string;
  icone: string;
  texto: string;
}

@Component({
  selector: 'app-admin-layout',
  imports: [RouterLink, RouterLinkActive, RouterOutlet, DatePipe],
  templateUrl: './admin-layout.html',
})
export class AdminLayout implements OnInit {
  private router = inject(Router);
  private authService = inject(AuthService);
  private destroyRef = inject(DestroyRef);

  // O menu fica escondido por padrão para não tirar espaço da tela de venda.
  public menuAberto = signal(false);
  public agora = signal(new Date());
  public naTelaDeVenda = signal(false);

  public readonly itensMenu: ItemMenu[] = [
    { rota: '/vendas/nova', icone: 'point_of_sale', texto: 'Nova venda' },
    { rota: '/dashboard', icone: 'space_dashboard', texto: 'Dashboard' },
    { rota: '/vendas', icone: 'receipt_long', texto: 'Vendas' },
    { rota: '/produtos', icone: 'inventory_2', texto: 'Produtos' },
  ];

  ngOnInit(): void {
    this.naTelaDeVenda.set(this.router.url.startsWith('/vendas/nova'));

    const relogio = setInterval(() => this.agora.set(new Date()), 15_000);
    this.destroyRef.onDestroy(() => clearInterval(relogio));

    const navegacao = this.router.events
      .pipe(filter((e) => e instanceof NavigationEnd))
      .subscribe((e) => {
        this.menuAberto.set(false);
        this.naTelaDeVenda.set(e.urlAfterRedirects.startsWith('/vendas/nova'));
      });
    this.destroyRef.onDestroy(() => navegacao.unsubscribe());
  }

  @HostListener('window:keydown.escape')
  fecharMenu(): void {
    this.menuAberto.set(false);
  }

  alternarMenu(): void {
    this.menuAberto.update((aberto) => !aberto);
  }

  sair(): void {
    this.authService.logout();
    this.router.navigate(['/']);
  }
}
