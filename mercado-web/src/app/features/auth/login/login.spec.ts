import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Login } from './login';
import { providersTeste } from '../../../../testing/providers-teste';

describe('Login', () => {
  let component: Login;
  let fixture: ComponentFixture<Login>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Login],
      providers: providersTeste,
    }).compileComponents();

    fixture = TestBed.createComponent(Login);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('deve criar a tela', () => {
    expect(component).toBeTruthy();
  });
});
