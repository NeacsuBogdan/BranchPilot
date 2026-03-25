import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import {
  type BookingActionDialogData,
  type BookingActionDialogResult,
} from '../../core/bookings/bookings.models';

@Component({
  selector: 'app-booking-action-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './booking-action-dialog.component.html',
  styleUrl: './booking-action-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingActionDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<BookingActionDialogComponent, BookingActionDialogResult | undefined>>(
      MatDialogRef,
    );

  protected readonly data = inject<BookingActionDialogData>(MAT_DIALOG_DATA);
  protected readonly isRescheduleMode = computed(() => this.data.mode === 'reschedule');

  protected readonly form = this.formBuilder.nonNullable.group({
    startsAtUtc: [toLocalDateTimeValue(this.data.booking.startsAtUtc)],
    reason: [
      '',
      [
        Validators.maxLength(300),
        ...(this.data.mode === 'cancel' ? [Validators.required] : []),
      ],
    ],
  });

  protected close(): void {
    this.dialogRef.close();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (this.data.mode === 'reschedule') {
      this.dialogRef.close({
        action: 'reschedule',
        payload: {
          startsAtUtc: new Date(value.startsAtUtc).toISOString(),
          reason: value.reason.trim(),
        },
      });
      return;
    }

    this.dialogRef.close({
      action: 'cancel',
      payload: {
        reason: value.reason.trim(),
      },
    });
  }
}

function toLocalDateTimeValue(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return '';
  }

  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, '0');
  const day = String(date.getDate()).padStart(2, '0');
  const hours = String(date.getHours()).padStart(2, '0');
  const minutes = String(date.getMinutes()).padStart(2, '0');

  return `${year}-${month}-${day}T${hours}:${minutes}`;
}
