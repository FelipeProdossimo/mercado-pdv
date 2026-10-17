import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetalheVenda } from './detalhe-venda';
import { providersTeste } from '../../../../testing/providers-teste';

describe('DetalheVenda', () => {
  let component: DetalheVenda;
  let fixture: ComponentFixture<DetalheVenda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DetalheVenda],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(DetalheVenda);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
