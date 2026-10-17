import { ComponentFixture, TestBed } from '@angular/core/testing';

import { AdminLayout } from './admin-layout';
import { providersTeste } from '../../../testing/providers-teste';

describe('AdminLayout', () => {
  let component: AdminLayout;
  let fixture: ComponentFixture<AdminLayout>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AdminLayout],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(AdminLayout);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });

  it('começa com o menu fechado e abre/fecha pelo botão', () => {
    expect(component.menuAberto()).toBeFalse();

    component.alternarMenu();
    expect(component.menuAberto()).toBeTrue();

    window.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    expect(component.menuAberto()).toBeFalse();
  });
});
