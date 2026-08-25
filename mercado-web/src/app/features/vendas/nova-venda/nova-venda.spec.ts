import { ComponentFixture, TestBed } from '@angular/core/testing';

import { NovaVenda } from './nova-venda';

describe('NovaVenda', () => {
  let component: NovaVenda;
  let fixture: ComponentFixture<NovaVenda>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NovaVenda]
    })
    .compileComponents();

    fixture = TestBed.createComponent(NovaVenda);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
