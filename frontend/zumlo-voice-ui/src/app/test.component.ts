
import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'test',
  standalone: true,
  imports: [CommonModule],
  template: `<h1 style="color:white">Angular Works ✓</h1>`
})
export class TestComponent {}
