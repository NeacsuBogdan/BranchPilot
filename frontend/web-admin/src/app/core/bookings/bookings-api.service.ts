import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  type BookingDetails,
  type BookingOptions,
  type CreateBookingRequest,
  type GetBookingsQuery,
  type PagedResponse,
  type BookingListItem,
  type RescheduleBookingRequest,
  type CancelBookingRequest,
} from './bookings.models';

@Injectable({ providedIn: 'root' })
export class BookingsApiService {
  private readonly http = inject(HttpClient);

  getOptions(): Promise<BookingOptions> {
    return firstValueFrom(this.http.get<BookingOptions>('/api/bookings/options'));
  }

  list(query: GetBookingsQuery): Promise<PagedResponse<BookingListItem>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (query.locationId) {
      params = params.set('locationId', query.locationId);
    }

    if (query.status) {
      params = params.set('status', query.status);
    }

    return firstValueFrom(this.http.get<PagedResponse<BookingListItem>>('/api/bookings', { params }));
  }

  create(request: CreateBookingRequest): Promise<BookingDetails> {
    return firstValueFrom(this.http.post<BookingDetails>('/api/bookings', request));
  }

  confirm(bookingId: string): Promise<BookingDetails> {
    return firstValueFrom(this.http.post<BookingDetails>(`/api/bookings/${bookingId}/confirm`, {}));
  }

  complete(bookingId: string): Promise<BookingDetails> {
    return firstValueFrom(this.http.post<BookingDetails>(`/api/bookings/${bookingId}/complete`, {}));
  }

  reschedule(bookingId: string, request: RescheduleBookingRequest): Promise<BookingDetails> {
    return firstValueFrom(
      this.http.post<BookingDetails>(`/api/bookings/${bookingId}/reschedule`, request),
    );
  }

  cancel(bookingId: string, request: CancelBookingRequest): Promise<BookingDetails> {
    return firstValueFrom(this.http.post<BookingDetails>(`/api/bookings/${bookingId}/cancel`, request));
  }
}
