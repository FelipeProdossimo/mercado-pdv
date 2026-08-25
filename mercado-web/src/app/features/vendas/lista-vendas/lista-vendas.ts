import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { VendaService } from '../../../core/services/venda';
import { VendaListagem } from '../../../core/models/venda-listagem.model';
import { StatusPagamento } from '../../../core/enums/status-pagamento.model';

const ROTULOS_STATUS: Record<StatusPagamento, { texto: string; classe: string }> = {
  [StatusPagamento.Pendente]: { texto: 'Pendente', classe: 'bg-yellow-900 text-yellow-300' },
  [StatusPagamento.Aprovado]: { texto: 'Aprovado', classe: 'bg-green-900 text-green-300' },
  [StatusPagamento.Recusado]: { texto: 'Recusado', classe: 'bg-red-900 text-red-300' },
  [StatusPagamento.Cancelado]: { texto: 'Cancelado', classe: 'bg-gray-800 text-gray-400' },
};

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

  rotuloStatus(status: StatusPagamento) {
    return ROTULOS_STATUS[status] ?? { texto: '—', classe: 'bg-gray-800 text-gray-400' };
  }
}
