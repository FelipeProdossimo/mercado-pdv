import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VendaService } from '../../../core/services/venda';
import { VendaListagem } from '../../../core/models/venda-listagem.model';
import { rotuloStatus } from '../../../core/enums/status-pagamento-rotulo';

@Component({
  selector: 'app-lista-vendas',
  imports: [CommonModule, RouterLink],
  templateUrl: './lista-vendas.html',
})
export class ListaVendas implements OnInit {
  private vendaService = inject(VendaService);

  vendas: VendaListagem[] = [];

  ngOnInit(): void {
    this.vendaService.obterTodas().subscribe({
      next: (response) => {
        this.vendas = response;
      },
    });
  }

  readonly rotuloStatus = rotuloStatus;
}
