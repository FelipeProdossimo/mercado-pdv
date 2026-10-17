import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Produto } from '../../../core/models/produto.model';
import { ProdutoService } from '../../../core/services/produto';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-lista-produtos',
  imports: [
    CommonModule,
    RouterLink
  ],
  templateUrl: './lista-produtos.html',
  styleUrl: './lista-produtos.scss',
})
export class ListaProdutos implements OnInit {

  private produtoService = inject(ProdutoService);

  produtos: Produto[] = [];
  filtro = '';

  get produtosFiltrados(): Produto[] {
    const texto = this.filtro.trim().toLowerCase();

    if (!texto) {
      return this.produtos;
    }

    return this.produtos.filter(
      (x) => x.descricao.toLowerCase().includes(texto) || x.codigoBarras.includes(texto),
    );
  }

  ngOnInit(): void {
    this.carregarProdutos();
  }

  carregarProdutos(): void {

    this.produtoService
      .obterTodos()
      .subscribe({

        next: response => {

          this.produtos = response.dados;
        }
      });
  }

  excluir(id: number): void {
    if (!confirm('Deseja excluir este produto?')) {
      return;
    }
    
    this.produtoService
      .excluir(id)
      .subscribe({
        next: () => {
          this.carregarProdutos();
        }
      });
  }
}