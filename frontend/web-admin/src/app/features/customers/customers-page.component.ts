import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, type PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { firstValueFrom } from 'rxjs';

import { PermissionCodes } from '../../core/auth/permission-codes';
import { AuthService } from '../../core/auth/auth.service';
import { CustomersApiService } from '../../core/customers/customers-api.service';
import {
  type Customer,
  type CustomerDialogResult,
  type PagedResponse,
} from '../../core/customers/customers.models';
import { CustomerDialogComponent } from './customer-dialog.component';

@Component({
  selector: 'app-customers-page',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    ReactiveFormsModule,
  ],
  templateUrl: './customers-page.component.html',
  styleUrl: './customers-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomersPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly customersApiService = inject(CustomersApiService);
  private readonly dialog = inject(MatDialog);
  private readonly formBuilder = inject(FormBuilder);

  private readonly pageState = signal(1);
  private readonly pageSizeState = signal(10);

  protected readonly canManageCustomers = computed(() =>
    this.authService.hasPermission(PermissionCodes.customersManage),
  );
  protected readonly pageResponse = signal<PagedResponse<Customer> | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    search: [''],
    status: ['all'],
  });

  protected readonly totalCustomers = computed(() => this.pageResponse()?.totalCount ?? 0);
  protected readonly activeCustomersOnPage = computed(
    () => this.pageResponse()?.items.filter((customer) => customer.isActive).length ?? 0,
  );

  ngOnInit(): void {
    void this.loadCustomers();
  }

  protected async applyFilters(): Promise<void> {
    this.pageState.set(1);
    await this.loadCustomers();
  }

  protected async changePage(event: PageEvent): Promise<void> {
    this.pageState.set(event.pageIndex + 1);
    this.pageSizeState.set(event.pageSize);
    await this.loadCustomers();
  }

  protected async openCreateDialog(): Promise<void> {
    if (!this.canManageCustomers()) {
      return;
    }

    const dialogResult = await this.openDialog({ mode: 'create' });

    if (!dialogResult || dialogResult.action !== 'create') {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.customersApiService.create(dialogResult.payload);
      await this.loadCustomers();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  protected async openEditDialog(customer: Customer): Promise<void> {
    if (!this.canManageCustomers()) {
      return;
    }

    const dialogResult = await this.openDialog({ mode: 'edit', customer });

    if (!dialogResult || dialogResult.action !== 'update') {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.customersApiService.update(dialogResult.customerId, dialogResult.payload);
      await this.loadCustomers();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  protected async deleteCustomer(customer: Customer): Promise<void> {
    if (!this.canManageCustomers()) {
      return;
    }

    if (!globalThis.confirm(`Delete ${customer.fullName}? This only works when no bookings exist.`)) {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.customersApiService.delete(customer.id);
      await this.loadCustomers();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  private async loadCustomers(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const filters = this.filtersForm.getRawValue();
      this.pageResponse.set(
        await this.customersApiService.list({
          page: this.pageState(),
          pageSize: this.pageSizeState(),
          search: filters.search.trim() || undefined,
          isActive: parseStatus(filters.status),
        }),
      );
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async openDialog(data: {
    mode: 'create' | 'edit';
    customer?: Customer;
  }): Promise<CustomerDialogResult | undefined> {
    const dialogRef = this.dialog.open(CustomerDialogComponent, {
      width: '640px',
      maxWidth: 'calc(100vw - 2rem)',
      data,
    });

    return firstValueFrom(dialogRef.afterClosed());
  }
}

function parseStatus(value: string): boolean | undefined {
  return value === 'active' ? true : value === 'inactive' ? false : undefined;
}

function getApiErrorMessage(error: unknown): string {
  if (
    error instanceof HttpErrorResponse &&
    typeof error.error?.detail === 'string' &&
    error.error.detail.length > 0
  ) {
    return error.error.detail;
  }

  return 'The customer workspace could not be updated right now.';
}
