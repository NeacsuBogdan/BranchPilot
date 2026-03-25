import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';

import {
  type BookingDialogData,
  type BookingDialogResult,
  type CreateBookingLineRequest,
  type BookingServiceOption,
} from '../../core/bookings/bookings.models';

@Component({
  selector: 'app-booking-dialog',
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './booking-dialog.component.html',
  styleUrl: './booking-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<BookingDialogComponent, BookingDialogResult | undefined>>(MatDialogRef);

  protected readonly data = inject<BookingDialogData>(MAT_DIALOG_DATA);
  protected readonly submissionError = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    customerId: [this.data.options.customers[0]?.id ?? '', [Validators.required]],
    locationId: [this.data.options.locations[0]?.id ?? '', [Validators.required]],
    startsAtUtc: ['', [Validators.required]],
    notes: ['', [Validators.maxLength(1000)]],
    lines: this.formBuilder.array([]),
  });

  constructor() {
    this.addLine();
  }

  protected get lineRows(): FormArray {
    return this.form.controls.lines as FormArray;
  }

  protected addLine(): void {
    this.lineRows.push(
      this.formBuilder.group({
        catalogItemId: [this.data.options.services[0]?.id ?? '', [Validators.required]],
        quantity: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
      }),
    );
  }

  protected removeLine(index: number): void {
    if (this.lineRows.length === 1) {
      return;
    }

    this.lineRows.removeAt(index);
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected estimateDuration(): number {
    return this.lineRows.controls.reduce((total, control) => {
      const service = this.findService(control.value.catalogItemId);
      const quantity = Number(control.value.quantity ?? 0);

      return total + (service?.durationInMinutes ?? 0) * quantity;
    }, 0);
  }

  protected estimateAmount(): string {
    const rawValue = this.form.getRawValue();
    const locationId = rawValue.locationId;
    const lines = rawValue.lines as CreateBookingLineRequest[];

    if (!locationId) {
      return 'Select a location';
    }

    let totalAmount = 0;
    let currencyCode = '';

    for (const line of lines) {
      const service = this.findService(line.catalogItemId);
      const locationPrice = service?.locationPrices.find((price) => price.locationId === locationId);

      if (!service || !locationPrice) {
        return 'Add priced services for the selected location';
      }

      totalAmount += locationPrice.priceAmount * Number(line.quantity ?? 0);
      currencyCode = locationPrice.currencyCode;
    }

    return `${totalAmount.toFixed(2)} ${currencyCode}`;
  }

  protected submit(): void {
    if (this.form.invalid || this.lineRows.length === 0) {
      this.form.markAllAsTouched();
      this.lineRows.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    const locationId = value.locationId;
    const lines = (value.lines as CreateBookingLineRequest[]).map((line) => ({
      catalogItemId: line.catalogItemId,
      quantity: Number(line.quantity),
    }));

    if (!lines.every((line) => this.serviceHasLocationPrice(line.catalogItemId, locationId))) {
      this.submissionError.set('Each selected service must have pricing at the chosen location.');
      return;
    }

    this.submissionError.set(null);
    this.dialogRef.close({
      payload: {
        customerId: value.customerId,
        locationId,
        startsAtUtc: toIsoDateTime(value.startsAtUtc),
        notes: value.notes.trim(),
        lines,
      },
    });
  }

  private serviceHasLocationPrice(catalogItemId: string, locationId: string): boolean {
    return (
      this.findService(catalogItemId)?.locationPrices.some((price) => price.locationId === locationId) ??
      false
    );
  }

  private findService(serviceId: string): BookingServiceOption | undefined {
    return this.data.options.services.find((service) => service.id === serviceId);
  }
}

function toIsoDateTime(value: string): string {
  return new Date(value).toISOString();
}
