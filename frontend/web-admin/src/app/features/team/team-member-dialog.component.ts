import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import {
  type TeamMemberDialogData,
  type TeamMemberDialogResult,
} from '../../core/team/team.models';

@Component({
  selector: 'app-team-member-dialog',
  imports: [
    MatButtonModule,
    MatCheckboxModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './team-member-dialog.component.html',
  styleUrl: './team-member-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TeamMemberDialogComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly dialogRef =
    inject<MatDialogRef<TeamMemberDialogComponent, TeamMemberDialogResult | undefined>>(MatDialogRef);

  protected readonly data = inject<TeamMemberDialogData>(MAT_DIALOG_DATA);
  protected readonly isEditMode = this.data.mode === 'edit';
  protected readonly selectedRole = computed(() =>
    this.data.roleOptions.find((option) => option.code === this.form.controls.role.value) ?? null,
  );

  protected readonly form = this.formBuilder.nonNullable.group({
    firstName: [
      this.data.member?.firstName ?? '',
      [Validators.required, Validators.maxLength(80)],
    ],
    lastName: [this.data.member?.lastName ?? '', [Validators.required, Validators.maxLength(80)]],
    email: [this.data.member?.email ?? '', [Validators.required, Validators.email]],
    password: ['', this.isEditMode ? [] : [Validators.required, Validators.minLength(10)]],
    role: [this.data.member?.membership.role ?? 'Staff', [Validators.required]],
    isActive: [this.data.member?.isActive ?? true],
    locationIds: [
      this.data.member?.membership.assignedLocations.map((location) => location.id) ?? [],
      [Validators.required],
    ],
  });

  constructor() {
    if (this.isEditMode) {
      this.form.controls.firstName.disable();
      this.form.controls.lastName.disable();
      this.form.controls.email.disable();
      this.form.controls.password.disable();
    }
  }

  protected close(): void {
    this.dialogRef.close();
  }

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();

    if (this.isEditMode && this.data.member) {
      this.dialogRef.close({
        action: 'update',
        userId: this.data.member.id,
        payload: {
          role: value.role,
          isActive: value.isActive,
          locationIds: value.locationIds,
        },
      });
      return;
    }

    this.dialogRef.close({
      action: 'create',
      payload: {
        firstName: value.firstName,
        lastName: value.lastName,
        email: value.email,
        password: value.password,
        role: value.role,
        locationIds: value.locationIds,
      },
    });
  }
}
