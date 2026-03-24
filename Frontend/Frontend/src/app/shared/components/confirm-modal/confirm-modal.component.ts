import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  template: `
    @if (visible) {
      <div class="modal-backdrop" (click)="cancel.emit()">
        <div class="modal" style="max-width:420px" (click)="$event.stopPropagation()">
          <div class="modal-header">
            <h3>{{ title }}</h3>
          </div>
          <div class="modal-body">
            <p>{{ message }}</p>
          </div>
          <div class="modal-footer">
            <button type="button" class="btn btn-ghost" (click)="cancel.emit()">Cancel</button>
            <button type="button" [class]="'btn btn-' + confirmClass" (click)="confirm.emit()">{{ confirmText }}</button>
          </div>
        </div>
      </div>
    }
  `
})
export class ConfirmModalComponent {
  @Input() visible      = false;
  @Input() title        = 'Confirm Action';
  @Input() message      = 'Are you sure?';
  @Input() confirmText  = 'Confirm';
  @Input() confirmClass = 'danger';
  @Output() confirm     = new EventEmitter<void>();
  @Output() cancel      = new EventEmitter<void>();
}
