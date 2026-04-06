import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-confirm-modal',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './confirm-modal.component.html',
  styleUrls: ['./confirm-modal.component.css']
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
