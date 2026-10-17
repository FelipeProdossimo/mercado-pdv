import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Relatorios } from './relatorios';
import { providersTeste } from '../../../../testing/providers-teste';

describe('Relatorios', () => {
  let component: Relatorios;
  let fixture: ComponentFixture<Relatorios>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Relatorios],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(Relatorios);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
