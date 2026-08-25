import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProdutoService } from '../../../core/services/produto';

@Component({
  selector: 'app-form-produto',
  imports: [CommonModule, FormsModule],
  templateUrl: './form-produto.html',
  styleUrl: './form-produto.scss',
})
export class FormProduto implements OnInit {
  private produtoService = inject(ProdutoService);

  private route = inject(ActivatedRoute);

  private router = inject(Router);

  id?: number;

  arquivoSelecionado?: File;

  imagemPreview?: string;

  produto = {
    descricao: '',
    valor: 0,
    estoque: 0,
    codigoBarras: '',
    imagemUrl: ''
  };

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');

    if (id) {
      this.id = Number(id);

      this.produtoService.obterPorId(this.id).subscribe({
        next: (response) => {
          this.produto = {
            descricao: response.descricao,
            valor: response.valor,
            estoque: response.estoque,
            codigoBarras: response.codigoBarras,
            imagemUrl: response.imagemUrl ?? ''
          };
        },
      });
    }
  }

  salvar(): void {
    if (this.id) {
      this.produtoService.editar(this.id, this.produto).subscribe({
        next: () => {
          if (this.arquivoSelecionado) {
            this.produtoService.uploadImagem(this.id!, this.arquivoSelecionado).subscribe({
              next: () => {
                this.router.navigate(['/produtos']);
              },
            });

            return;
          }

          this.router.navigate(['/produtos']);
        },
      });

      return;
    }
    debugger;
    this.produtoService.adicionar(this.produto).subscribe({
      next: (id) => {
        if (this.arquivoSelecionado) {
          this.produtoService.uploadImagem(id, this.arquivoSelecionado).subscribe({
            next: () => {
              this.router.navigate(['/produtos']);
            },
          });

          return;
        }

        this.router.navigate(['/produtos']);
      },
    });
  }

  selecionarImagem(event: Event): void {
    const input = event.target as HTMLInputElement;

    if (!input.files || input.files.length === 0) {
      return;
    }

    this.arquivoSelecionado = input.files[0];
    debugger;
    this.imagemPreview = URL.createObjectURL(this.arquivoSelecionado);
  }
}
