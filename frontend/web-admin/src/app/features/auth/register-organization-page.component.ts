import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';

import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register-organization-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    ReactiveFormsModule,
    RouterLink,
  ],
  templateUrl: './register-organization-page.component.html',
  styleUrl: './register-organization-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RegisterOrganizationPageComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    tenantName: ['Harbor & Pine Operations', [Validators.required, Validators.maxLength(120)]],
    primaryLocationName: ['Bucharest Flagship', [Validators.required, Validators.maxLength(120)]],
    primaryLocationCode: ['BUC-FLAG', [Validators.required, Validators.pattern(/^[A-Za-z0-9-]+$/)]],
    primaryLocationTimeZone: ['Europe/Bucharest', [Validators.required, Validators.maxLength(100)]],
    firstName: ['Elena', [Validators.required, Validators.maxLength(80)]],
    lastName: ['Ionescu', [Validators.required, Validators.maxLength(80)]],
    email: ['elena@harborpine.test', [Validators.required, Validators.email]],
    password: ['BranchPilot!123', [Validators.required, Validators.minLength(10)]],
  });

  protected async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    try {
      await this.authService.registerOrganization(this.form.getRawValue());
      await this.router.navigateByUrl('/dashboard');
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isSubmitting.set(false);
    }
  }
}

function getApiErrorMessage(error: unknown): string {
  if (
    error instanceof HttpErrorResponse &&
    typeof error.error?.detail === 'string' &&
    error.error.detail.length > 0
  ) {
    return error.error.detail;
  }

  return 'Organization setup failed. Review the details and try again.';
}
