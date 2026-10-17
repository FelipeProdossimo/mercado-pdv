import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListaVendas } from './lista-vendas';
import { providersTeste } from '../../../../testing/providers-teste';

describe('ListaVendas', () => {
  let component: ListaVendas;
  let fixture: ComponentFixture<ListaVendas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListaVendas],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(ListaVendas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
