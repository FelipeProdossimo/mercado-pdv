import { Component, OnInit, inject } from '@angular/core';

import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';

import { VendaService } from '../../../core/services/venda';

import { VendaDetalhe } from '../../../core/models/venda-detalhe.model';

@Component({
  selector: 'app-detalhe-venda',
  imports: [CommonModule],
  templateUrl: './detalhe-venda.html',
})
export class DetalheVenda implements OnInit {
  private route = inject(ActivatedRoute);

  private vendaService = inject(VendaService);

  venda?: VendaDetalhe;

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));

    this.vendaService.obterPorId(id).subscribe({
      next: (response) => {
        this.venda = response;
      },
    });
  }
}
