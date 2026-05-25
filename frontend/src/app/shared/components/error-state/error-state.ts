import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-error-state',
  templateUrl: './error-state.html',
  styleUrl: './error-state.scss'
})
export class ErrorState {
  @Input() message = 'An unexpected error occurred.';
}
