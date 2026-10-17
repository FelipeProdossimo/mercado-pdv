import { TestBed } from '@angular/core/testing';

import { DashboardService } from './dashboard';
import { providersTeste } from '../../../testing/providers-teste';

describe('DashboardService', () => {
  let service: DashboardService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: providersTeste });
    service = TestBed.inject(DashboardService);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });
});
