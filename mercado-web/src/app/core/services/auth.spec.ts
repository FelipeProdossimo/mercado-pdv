import { TestBed } from '@angular/core/testing';
import { HttpTestingController } from '@angular/common/http/testing';

import { AuthService } from './auth';
import { providersTeste } from '../../../testing/providers-teste';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({ providers: providersTeste });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('guarda token e refresh token depois do login', () => {
    service.login({ email: 'caixa@mercado.local', senha: '123' }).subscribe();

    const req = httpMock.expectOne((r) => r.url.endsWith('/auth/login'));
    expect(req.request.method).toBe('POST');
    req.flush({ token: 'jwt', refreshToken: 'refresh' });

    expect(service.getToken()).toBe('jwt');
    expect(localStorage.getItem('refreshToken')).toBe('refresh');
    expect(service.isAuthenticated()).toBeTrue();
  });

  it('logout remove a sessão', () => {
    localStorage.setItem('token', 'jwt');

    service.logout();

    expect(service.isAuthenticated()).toBeFalse();
  });
});
