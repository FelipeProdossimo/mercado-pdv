import { ComponentFixture, TestBed } from '@angular/core/testing';

import { FormProduto } from './form-produto';
import { providersTeste } from '../../../../testing/providers-teste';

describe('FormProduto', () => {
  let component: FormProduto;
  let fixture: ComponentFixture<FormProduto>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormProduto],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(FormProduto);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
