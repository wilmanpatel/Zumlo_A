
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'app-summary',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="card">
      <h2>Session Summary</h2>

      <ng-container *ngIf="loading">
        <p>Loading summary…</p>
      </ng-container>

      <ng-container *ngIf="!loading && error">
        <p style="color:#ff6b6b;">{{ error }}</p>
        <a routerLink="/" class="btn">Back to conversation</a>
      </ng-container>

      <ng-container *ngIf="!loading && data">
        <div class="badge">Session: {{ data.sessionId }}</div>

        <h3>Themes</h3>
        <ul><li *ngFor="let t of data.themes">{{ t }}</li></ul>

        <h3>Micro‑actions</h3>
        <ul><li *ngFor="let a of data.microActions">{{ a }}</li></ul>

        <h3>Transcript</h3>
        <p style="white-space: pre-wrap">{{ data.transcript }}</p>

        <a routerLink="/" class="btn">Back to conversation</a>
      </ng-container>
    </div>
  `
})
export class SummaryComponent {
  data: any;
  error = '';
  loading = true;

  constructor(private route: ActivatedRoute) {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.loading = false;
      this.error = 'No session id in route.';
      return;
    }

    // Use RELATIVE URL so Angular proxy forwards to backend (no CORS headaches)
    fetch(`http://localhost:5099/api/sessions/${id}/summary`, {
      headers: {
        'Authorization': 'Bearer dev', // Required by backend (Fake JWT)
        'Accept': 'application/json'
      }
    })
      .then(async (res) => {
        if (!res.ok) {
          const text = await res.text().catch(() => '');
          // Backend returns a consistent error envelope for 404; surface something human‑readable
          throw new Error(text || `Request failed: ${res.status} ${res.statusText}`);
        }
        return res.json();
      })
      .then(json => {
        this.data = json;
        this.loading = false;
      })
      .catch(err => {
        this.error = `Could not load summary. ${err.message}`;
        this.loading = false;
        console.error('SUMMARY FETCH ERROR', err);
      });
  }
}
