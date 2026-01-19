
import { Injectable, NgZone } from '@angular/core';
import { Subject, timer, Subscription } from 'rxjs';

type Outbound = any;

@Injectable({ providedIn: 'root' })
export class WebSocketService {
  private socket?: WebSocket;
  private url = 'ws://localhost:5099/ws';
  private token = 'dev';

  /** Emits parsed inbound messages */
  public incoming$ = new Subject<any>();
  /** Emits once per successful open (including reconnects) */
  public open$ = new Subject<void>();
  /** Emits on close/error */
  public closed$ = new Subject<CloseEvent | Event>();

  /** Queue outbound messages while CONNECTING */
  private sendQueue: Outbound[] = [];
  /** Reconnect settings */
  private reconnectAttempts = 0;
  private maxReconnectDelayMs = 8000;
  private reconnectTimerSub?: Subscription;

  /** Heartbeat ping to detect half-open connections */
  private heartbeatIntervalMs = 30000;
  private heartbeatTimerSub?: Subscription;

  constructor(private zone: NgZone) {}

  /** Connect (idempotent). Safe to call multiple times. */
  connect(token: string, url = this.url) {
    this.token = token;
    this.url = url;

    if (this.socket && (this.socket.readyState === WebSocket.OPEN || this.socket.readyState === WebSocket.CONNECTING)) {
      return; // already connecting/open
    }

    // Build URL with token (since browsers can't set WS headers)
    const wsUrl = url + `?token=${encodeURIComponent(token)}`;

    // Create socket outside Angular zone to avoid excessive change detection
    this.zone.runOutsideAngular(() => {
      this.socket = new WebSocket(wsUrl);

      this.socket.onopen = () => {
        this.reconnectAttempts = 0;
        this.zone.run(() => {
          // Flush any queued messages
          this.flushQueue();
          this.open$.next();
          this.startHeartbeat();
        });
      };

      this.socket.onmessage = (ev) => {
        try {
          const data = JSON.parse(ev.data);
          this.zone.run(() => this.incoming$.next(data));
        } catch (e) {
          // ignore malformed frames
        }
      };

      this.socket.onclose = (ev) => {
        this.zone.run(() => this.closed$.next(ev));
        this.stopHeartbeat();
        this.scheduleReconnect();
      };

      this.socket.onerror = (ev) => {
        this.zone.run(() => this.closed$.next(ev));
        // Close will follow and trigger reconnect
      };
    });
  }

  /** Safe send: queues until OPEN, drops if CLOSED (until reconnect). */
  send(obj: any) {
    if (!this.socket) {
      this.sendQueue.push(obj);
      return;
    }
    if (this.socket.readyState === WebSocket.OPEN) {
      this.socket.send(JSON.stringify(obj));
    } else if (this.socket.readyState === WebSocket.CONNECTING) {
      this.sendQueue.push(obj);
    } else {
      // CLOSED / CLOSING: keep in queue; will be flushed after reconnect
      this.sendQueue.push(obj);
    }
  }

  /** Explicit close (e.g., on component destroy) */
  close(code: number = 1000, reason: string = 'client closing') {
    this.stopHeartbeat();
    this.reconnectTimerSub?.unsubscribe();
    this.socket?.close(code, reason);
  }

  // --- internals ---

  private flushQueue() {
    if (!this.socket || this.socket.readyState !== WebSocket.OPEN) return;
    while (this.sendQueue.length) {
      const msg = this.sendQueue.shift()!;
      this.socket.send(JSON.stringify(msg));
    }
  }

  private scheduleReconnect() {
    this.reconnectTimerSub?.unsubscribe();
    this.reconnectAttempts++;
    const delay = Math.min(250 * Math.pow(2, this.reconnectAttempts - 1), this.maxReconnectDelayMs);
    this.reconnectTimerSub = timer(delay).subscribe(() => this.connect(this.token, this.url));
  }

  private startHeartbeat() {
    this.stopHeartbeat();
    this.heartbeatTimerSub = timer(this.heartbeatIntervalMs, this.heartbeatIntervalMs).subscribe(() => {
      if (this.socket?.readyState === WebSocket.OPEN) {
        try {
          this.socket.send(JSON.stringify({ type: 'ping', t: Date.now() }));
        } catch {
          // if send fails, close will trigger reconnect
          this.socket?.close(1011, 'heartbeat failed');
        }
      }
    });
  }

  private stopHeartbeat() {
    this.heartbeatTimerSub?.unsubscribe();
  }
}
``
