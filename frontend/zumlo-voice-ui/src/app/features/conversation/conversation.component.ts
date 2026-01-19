
import { Component, effect, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { WebSocketService } from '../../core/websocket.service';
import { AudioCaptureService } from '../../core/audio-capture.service';
import { SessionService } from '../../core/session.service';

@Component({
  selector: 'app-conversation',
  standalone: true,
  template: `
  <div class="card">
    <div class="row" style="justify-content: space-between;">
      <div>
        <div class="badge">Session: {{session.sessionId}}</div>
      </div>
      <button class="btn" (click)="goSummary()">Summary</button>
    </div>
    <div style="min-height: 320px; margin: 16px 0;">
      <div *ngFor="let m of messages">
        <div class="bubble" [class.user]="m.role==='user'" [class.assistant]="m.role==='assistant'">{{m.text}}</div>
      </div>
      <div class="bubble assistant" *ngIf="liveTranscript">{{liveTranscript}}</div>
    </div>
    <div class="row" style="justify-content: center; gap: 24px;">
      <div class="mic" style="cursor: pointer;" [class.pulse]="state==='listening'" (click)="toggleMic()">🎤</div>
      <div class="badge">State: {{state}}</div>
    </div>
  </div>
  `
})
export class ConversationComponent implements OnInit {
  state: 'idle'|'listening'|'processing'|'responding' = 'idle';
  messages: {role:'user'|'assistant', text:string}[] = [];
  liveTranscript = '';
  sessionReady: boolean = false;

  constructor(
    public session: SessionService,
    private ws: WebSocketService,
    private audio: AudioCaptureService,
    private router: Router,
  ) {
    
  }

  
 ngOnInit() {
    this.ws.connect(this.session.token,'ws://localhost:5099/ws');

     this.ws.send({
      type: 'session.start',
      payload: { sessionId: this.session.sessionId }
    });

    this.ws.incoming$.subscribe(ev => {
      if (ev.type === 'session.started') this.session.sessionId = ev.payload.sessionId;this.sessionReady = true; 
      if (ev.type === 'transcript.partial') this.liveTranscript += ev.payload.text + ' ';
      if (ev.type === 'assistant.delta') this.pushAssistant(ev.payload.text);
      if (ev.type === 'assistant.complete') this.state = 'responding';
       if (ev.type === 'session.completed') this.state = 'idle';
    });

   
  }


  connect() {
    this.ws.connect(this.session.token);
    this.ws.send({ type: 'session.start', payload: { sessionId: this.session.sessionId }});
  }

  async toggleMic() {

    
      if (!this.sessionReady) {
        return;
      }
      console.log('Started recording');
     if (!this.session.sessionId) return; 
    if (this.state !== 'listening') {
     this.state = 'listening'; 
      this.liveTranscript = '';
      this.messages.push({role:'user', text:'(recording...)'});
      await this.audio.start((seq, b64) => {
        this.ws.send({ type: 'audio.chunk', payload: { sessionId: this.session.sessionId, sequence: seq, base64Data: b64 }});
      });
      setTimeout(() => { this.stopRecording(); }, 3000); // quick demo
    } else {
      this.stopRecording();
    }
  }

  stopRecording() {
 
    console.log('Stopping recording');
    this.audio.stop();
    this.ws.send({ type: 'audio.end', payload: { sessionId: this.session.sessionId }});
    this.state = 'processing';
  }

  pushAssistant(text: string) {
    const last = this.messages[this.messages.length-1];
    if (last && last.role==='assistant') last.text += text; else this.messages.push({role:'assistant', text});
  }

  goSummary() {
    this.router.navigate(['/summary', this.session.sessionId]);
  }
}
