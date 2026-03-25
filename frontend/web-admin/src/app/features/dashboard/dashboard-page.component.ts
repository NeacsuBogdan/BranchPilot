import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDividerModule } from '@angular/material/divider';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { AuthService } from '../../core/auth/auth.service';
import { PermissionCodes } from '../../core/auth/permission-codes';
import { DashboardApiService } from '../../core/dashboard/dashboard-api.service';
import { type DashboardSummary } from '../../core/dashboard/dashboard.models';
import { LocationsApiService } from '../../core/locations/locations-api.service';

@Component({
  selector: 'app-dashboard-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDividerModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressBarModule,
    ReactiveFormsModule,
  ],
  templateUrl: './dashboard-page.component.html',
  styleUrl: './dashboard-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DashboardPageComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly dashboardApiService = inject(DashboardApiService);
  private readonly locationsApiService = inject(LocationsApiService);

  protected readonly session = this.authService.session;
  protected readonly locations = computed(() => this.session()?.locations ?? []);
  protected readonly canManageLocations = computed(() =>
    this.authService.hasPermission(PermissionCodes.locationsManage),
  );
  protected readonly summary = signal<DashboardSummary | null>(null);
  protected readonly isLoadingSummary = signal(false);
  protected readonly isSaving = signal(false);
  protected readonly summaryErrorMessage = signal<string | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly successMessage = signal<string | null>(null);

  protected readonly locationForm = this.formBuilder.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    code: ['', [Validators.required, Validators.pattern(/^[A-Za-z0-9-]+$/)]],
    timeZone: ['Europe/Bucharest', [Validators.required, Validators.maxLength(100)]],
  });

  ngOnInit(): void {
    void this.loadSummary();
  }

  protected async createLocation(): Promise<void> {
    if (!this.canManageLocations()) {
      return;
    }

    if (this.locationForm.invalid) {
      this.locationForm.markAllAsTouched();
      return;
    }

    this.isSaving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    try {
      await this.locationsApiService.create(this.locationForm.getRawValue());
      await this.authService.reloadSession();
      await this.loadSummary();
      this.locationForm.reset({
        name: '',
        code: '',
        timeZone: 'Europe/Bucharest',
      });
      this.successMessage.set('Location created successfully.');
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isSaving.set(false);
    }
  }

  private async loadSummary(): Promise<void> {
    this.isLoadingSummary.set(true);
    this.summaryErrorMessage.set(null);

    try {
      this.summary.set(await this.dashboardApiService.getSummary());
    } catch (error) {
      this.summaryErrorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoadingSummary.set(false);
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

  return 'The workspace could not be updated right now.';
}
