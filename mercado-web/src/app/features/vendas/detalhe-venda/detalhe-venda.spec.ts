import { ComponentFixture, TestBed } from '@angular/core/testing';

import { DetalheVenda } from './detalhe-venda';

describe('DetalheVenda', () => {
  let component: DetalheVenda;
  let fixture: ComponentFixture<DetalheVenda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DetalheVenda]
    })
    .compileComponents();

    fixture = TestBed.createComponent(DetalheVenda);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
