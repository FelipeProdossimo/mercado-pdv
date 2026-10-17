import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ListaProdutos } from './lista-produtos';
import { providersTeste } from '../../../../testing/providers-teste';

describe('ListaProdutos', () => {
  let component: ListaProdutos;
  let fixture: ComponentFixture<ListaProdutos>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ListaProdutos],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(ListaProdutos);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
