import { DatePipe, DecimalPipe } from '@angular/common';
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
import { BookingsApiService } from '../../core/bookings/bookings-api.service';
import {
  type BookingActionDialogResult,
  type BookingDialogResult,
  type BookingListItem,
  type BookingOptions,
  type PagedResponse,
} from '../../core/bookings/bookings.models';
import { BookingActionDialogComponent } from './booking-action-dialog.component';
import { BookingDialogComponent } from './booking-dialog.component';

@Component({
  selector: 'app-bookings-page',
  imports: [
    DatePipe,
    DecimalPipe,
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
  templateUrl: './bookings-page.component.html',
  styleUrl: './bookings-page.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BookingsPageComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly bookingsApiService = inject(BookingsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly formBuilder = inject(FormBuilder);

  private readonly pageState = signal(1);
  private readonly pageSizeState = signal(10);

  protected readonly canManageBookings = computed(() =>
    this.authService.hasPermission(PermissionCodes.bookingsManage),
  );
  protected readonly options = signal<BookingOptions | null>(null);
  protected readonly pageResponse = signal<PagedResponse<BookingListItem> | null>(null);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly filtersForm = this.formBuilder.nonNullable.group({
    search: [''],
    status: ['all'],
    locationId: [''],
  });

  protected readonly totalBookings = computed(() => this.pageResponse()?.totalCount ?? 0);
  protected readonly openBookingsOnPage = computed(
    () =>
      this.pageResponse()?.items.filter((booking) => booking.status === 'Scheduled' || booking.status === 'Confirmed')
        .length ?? 0,
  );
  protected readonly canCreateBookings = computed(() => {
    const options = this.options();

    return (
      this.canManageBookings() &&
      !!options &&
      options.customers.length > 0 &&
      options.locations.length > 0 &&
      options.services.length > 0
    );
  });

  ngOnInit(): void {
    void this.loadInitialState();
  }

  protected async applyFilters(): Promise<void> {
    this.pageState.set(1);
    await this.loadBookings();
  }

  protected async changePage(event: PageEvent): Promise<void> {
    this.pageState.set(event.pageIndex + 1);
    this.pageSizeState.set(event.pageSize);
    await this.loadBookings();
  }

  protected async openCreateDialog(): Promise<void> {
    const options = this.options();

    if (!this.canCreateBookings() || !options) {
      return;
    }

    const dialogResult = await this.openCreateBookingDialog(options);

    if (!dialogResult) {
      return;
    }

    try {
      this.errorMessage.set(null);
      await this.bookingsApiService.create(dialogResult.payload);
      await this.loadBookings();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  protected async confirmBooking(booking: BookingListItem): Promise<void> {
    if (!this.canManageBookings()) {
      return;
    }

    await this.performMutation(() => this.bookingsApiService.confirm(booking.id));
  }

  protected async completeBooking(booking: BookingListItem): Promise<void> {
    if (!this.canManageBookings()) {
      return;
    }

    await this.performMutation(() => this.bookingsApiService.complete(booking.id));
  }

  protected async openRescheduleDialog(booking: BookingListItem): Promise<void> {
    if (!this.canManageBookings()) {
      return;
    }

    const dialogResult = await this.openActionDialog({ mode: 'reschedule', booking });

    if (!dialogResult || dialogResult.action !== 'reschedule') {
      return;
    }

    await this.performMutation(() =>
      this.bookingsApiService.reschedule(booking.id, dialogResult.payload),
    );
  }

  protected async openCancelDialog(booking: BookingListItem): Promise<void> {
    if (!this.canManageBookings()) {
      return;
    }

    const dialogResult = await this.openActionDialog({ mode: 'cancel', booking });

    if (!dialogResult || dialogResult.action !== 'cancel') {
      return;
    }

    await this.performMutation(() => this.bookingsApiService.cancel(booking.id, dialogResult.payload));
  }

  private async loadInitialState(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const [options, pageResponse] = await Promise.all([
        this.bookingsApiService.getOptions(),
        this.bookingsApiService.list({
          page: this.pageState(),
          pageSize: this.pageSizeState(),
        }),
      ]);

      this.options.set(options);
      this.pageResponse.set(pageResponse);
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async loadBookings(): Promise<void> {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    try {
      const filters = this.filtersForm.getRawValue();
      this.pageResponse.set(
        await this.bookingsApiService.list({
          page: this.pageState(),
          pageSize: this.pageSizeState(),
          search: filters.search.trim() || undefined,
          status: filters.status === 'all' ? undefined : filters.status,
          locationId: filters.locationId || undefined,
        }),
      );
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    } finally {
      this.isLoading.set(false);
    }
  }

  private async performMutation(work: () => Promise<unknown>): Promise<void> {
    try {
      this.errorMessage.set(null);
      await work();
      await this.loadBookings();
    } catch (error) {
      this.errorMessage.set(getApiErrorMessage(error));
    }
  }

  private async openCreateBookingDialog(
    options: BookingOptions,
  ): Promise<BookingDialogResult | undefined> {
    const dialogRef = this.dialog.open(BookingDialogComponent, {
      width: '900px',
      maxWidth: 'calc(100vw - 2rem)',
      data: { options },
    });

    return firstValueFrom(dialogRef.afterClosed());
  }

  private async openActionDialog(data: {
    mode: 'reschedule' | 'cancel';
    booking: BookingListItem;
  }): Promise<BookingActionDialogResult | undefined> {
    const dialogRef = this.dialog.open(BookingActionDialogComponent, {
      width: '560px',
      maxWidth: 'calc(100vw - 2rem)',
      data,
    });

    return firstValueFrom(dialogRef.afterClosed());
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

  return 'The booking workspace could not be updated right now.';
}
