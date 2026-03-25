import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDialogModule, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import {
  type CustomerDialogData,
  type CustomerDialogResult,
} from '../../core/customers/customers.models';

@Component({
  selector: 'app-customer-dialog',
  imports: [
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    ReactiveFormsModule,
  ],
  templateUrl: './customer-dialog.component.html',
  styleUrl: './customer-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<CustomerDialogComponent, CustomerDialogResult | undefined>>(MatDialogRef);

  protected readonly data = inject<CustomerDialogData>(MAT_DIALOG_DATA);
  protected readonly isEditMode = this.data.mode === 'edit';

  protected readonly form = this.formBuilder.nonNullable.group({
    firstName: [this.data.customer?.firstName ?? '', [Validators.required, Validators.maxLength(80)]],
    lastName: [this.data.customer?.lastName ?? '', [Validators.required, Validators.maxLength(80)]],
    email: [
      this.data.customer?.email ?? '',
      [Validators.required, Validators.email, Validators.maxLength(200)],
    ],
    phoneNumber: [this.data.customer?.phoneNumber ?? '', [Validators.maxLength(40)]],
    notes: [this.data.customer?.notes ?? '', [Validators.maxLength(1000)]],
    isActive: [this.data.customer?.isActive ?? true],
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

    if (this.isEditMode && this.data.customer) {
      this.dialogRef.close({
        action: 'update',
        customerId: this.data.customer.id,
        payload: {
          firstName: value.firstName.trim(),
          lastName: value.lastName.trim(),
          email: value.email.trim(),
          phoneNumber: value.phoneNumber.trim(),
          notes: value.notes.trim(),
          isActive: value.isActive,
        },
      });
      return;
    }

    this.dialogRef.close({
      action: 'create',
      payload: {
        firstName: value.firstName.trim(),
        lastName: value.lastName.trim(),
        email: value.email.trim(),
        phoneNumber: value.phoneNumber.trim(),
        notes: value.notes.trim(),
      },
    });
  }
}
