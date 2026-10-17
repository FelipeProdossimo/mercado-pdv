import { TestBed } from '@angular/core/testing';

import { RealtimeService } from './realtime';
import { providersTeste } from '../../../testing/providers-teste';

describe('RealtimeService', () => {
  let service: RealtimeService;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: providersTeste });
    service = TestBed.inject(RealtimeService);
  });

  it('deve ser criado', () => {
    expect(service).toBeTruthy();
  });
});
