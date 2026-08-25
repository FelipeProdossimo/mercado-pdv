import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListaVendas } from './lista-vendas';

describe('ListaVendas', () => {
  let component: ListaVendas;
  let fixture: ComponentFixture<ListaVendas>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListaVendas]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ListaVendas);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
