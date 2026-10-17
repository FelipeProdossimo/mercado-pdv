import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NovaVenda } from './nova-venda';
import { Produto } from '../../../core/models/produto.model';
import { providersTeste } from '../../../../testing/providers-teste';

describe('NovaVenda (carrinho)', () => {
  let component: NovaVenda;
  let fixture: ComponentFixture<NovaVenda>;

  const arroz: Produto = { id: 1, descricao: 'Arroz 5kg', valor: 25.9, estoque: 3, codigoBarras: '789001' };
  const feijao: Produto = { id: 2, descricao: 'Feijão 1kg', valor: 8.5, estoque: 10, codigoBarras: '789002' };

  function adicionar(produto: Produto, quantidade = 1): void {
    component.produtoSelecionadoId = produto.id;
    component.quantidade = quantidade;
    component.adicionarItem();
  }

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NovaVenda],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(NovaVenda);
    component = fixture.componentInstance;
    fixture.detectChanges();

    component.produtos = [arroz, feijao];
  });

  afterEach(() => component.ngOnDestroy());

  it('adiciona o produto e calcula o total', () => {
    adicionar(arroz, 2);
    adicionar(feijao, 1);

    expect(component.itens.length).toBe(2);
    expect(component.totalPecas).toBe(3);
    expect(component.valorTotal).toBeCloseTo(25.9 * 2 + 8.5, 2);
  });

  it('soma a quantidade quando o mesmo produto é lido de novo', () => {
    adicionar(feijao, 2);
    adicionar(feijao, 3);

    expect(component.itens.length).toBe(1);
    expect(component.itens[0].quantidade).toBe(5);
    expect(component.itens[0].total).toBeCloseTo(42.5, 2);
  });

  it('não deixa passar do estoque disponível', () => {
    adicionar(arroz, 2);
    adicionar(arroz, 2);

    expect(component.itens[0].quantidade).toBe(2);
    expect(component.mensagem?.tipo).toBe('erro');
  });

  it('remove o item quando a quantidade chega a zero', () => {
    adicionar(feijao, 1);

    component.alterarQuantidade(component.itens[0], -1);

    expect(component.itens.length).toBe(0);
    expect(component.valorTotal).toBe(0);
  });

  it('bloqueia aumentar a quantidade além do estoque', () => {
    adicionar(arroz, 3);

    component.alterarQuantidade(component.itens[0], +1);

    expect(component.itens[0].quantidade).toBe(3);
  });
});
