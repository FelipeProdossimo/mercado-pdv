import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
      ],
    });

    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('envia o token no cabeçalho Authorization quando o usuário está logado', () => {
    localStorage.setItem('token', 'token-de-teste');

    http.get('/api/produtos').subscribe();

    const req = httpMock.expectOne('/api/produtos');
    expect(req.request.headers.get('Authorization')).toBe('Bearer token-de-teste');
    req.flush([]);
  });

  it('não envia Authorization quando não há token', () => {
    http.get('/api/produtos').subscribe();

    const req = httpMock.expectOne('/api/produtos');
    expect(req.request.headers.has('Authorization')).toBeFalse();
    req.flush([]);
  });
});
