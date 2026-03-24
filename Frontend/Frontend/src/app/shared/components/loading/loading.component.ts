import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-loading',
  standalone: true,
  template: `
    <div class="loading-center">
      <div class="spinner" [class.spinner-sm]="size === 'sm'"></div>
    </div>
  `
})
export class LoadingComponent {
  @Input() size: 'sm' | 'md' = 'md';
}
