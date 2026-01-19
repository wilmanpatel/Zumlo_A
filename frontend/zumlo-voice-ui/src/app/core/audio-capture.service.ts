
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AudioCaptureService {
  private mediaRecorder?: MediaRecorder;
  private sequence = 0;

  async start(onChunk: (seq: number, b64: string) => void) {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    this.sequence = 0;
    this.mediaRecorder = new MediaRecorder(stream, { mimeType: 'audio/webm' });
    this.mediaRecorder.ondataavailable = async (e) => {
      const b = await e.data.arrayBuffer();
      const bytes = new Uint8Array(b);
      let binary = '';
      for (let i = 0; i < bytes.byteLength; i++) binary += String.fromCharCode(bytes[i]);
      const b64 = btoa(binary);
      onChunk(this.sequence++, b64);
    };
    this.mediaRecorder.start(250);
  }

  stop() {
    this.mediaRecorder?.stop();
    this.mediaRecorder?.stream.getTracks().forEach(t => t.stop());
  }
}
