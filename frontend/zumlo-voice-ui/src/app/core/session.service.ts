
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SessionService {
  sessionId = this.newId();
  token = 'dev';

  newId() { return Math.random().toString(36).slice(2); }
}
