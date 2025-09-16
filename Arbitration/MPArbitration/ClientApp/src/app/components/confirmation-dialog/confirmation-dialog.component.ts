import { Component, EventEmitter, Input, Output } from '@angular/core';
//See https://dev.to/elasticrash/angular-display-a-warning-and-prevent-navigation-when-model-is-dirty-29e0

@Component({
  selector: 'confirmation-dialog',
  templateUrl: './confirmation-dialog.component.html',
  styleUrls: ['./confirmation-dialog.component.css']
})
export class ConfirmationDialogComponent {
  @Input() confirmText = "Lose Changes";
  @Input() message = "WARNING: You have unsaved changes! Click 'Cancel' to return to the form and save your work.";
  @Input() title = "Arbitration Alert";
  @Input() visible = false;
  @Output() onConfirmed = new EventEmitter<boolean>();

  public closeDialog(): void {
    this.onConfirmed.emit(false);
  }

  public navigateAway(): void {
    this.onConfirmed.emit(true);
  }

}
