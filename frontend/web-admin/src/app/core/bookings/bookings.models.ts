export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface BookingCustomerOption {
  id: string;
  fullName: string;
  email: string;
}

export interface BookingLocationOption {
  id: string;
  name: string;
  code: string;
  timeZone: string;
}

export interface BookingServicePriceOption {
  locationId: string;
  priceAmount: number;
  currencyCode: string;
}

export interface BookingServiceOption {
  id: string;
  name: string;
  code: string;
  durationInMinutes: number;
  locationPrices: BookingServicePriceOption[];
}

export interface BookingOptions {
  customers: BookingCustomerOption[];
  locations: BookingLocationOption[];
  services: BookingServiceOption[];
}

export interface BookingCustomerSummary {
  id: string;
  fullName: string;
  email: string;
}

export interface BookingLocationSummary {
  id: string;
  name: string;
  code: string;
}

export interface BookingListItem {
  id: string;
  number: string;
  status: string;
  startsAtUtc: string;
  endsAtUtc: string;
  notes: string;
  customer: BookingCustomerSummary;
  location: BookingLocationSummary;
  totalAmount: number;
  currencyCode: string;
  totalDurationInMinutes: number;
  lineCount: number;
}

export interface BookingLine {
  id: string;
  catalogItemId: string;
  itemName: string;
  quantity: number;
  durationInMinutes: number;
  unitPriceAmount: number;
  lineTotalAmount: number;
}

export interface BookingTimelineEntry {
  eventName: string;
  occurredAtUtc: string;
  description: string;
}

export interface BookingDetails {
  id: string;
  number: string;
  status: string;
  startsAtUtc: string;
  endsAtUtc: string;
  notes: string;
  customer: BookingCustomerSummary;
  location: BookingLocationSummary;
  totalAmount: number;
  currencyCode: string;
  totalDurationInMinutes: number;
  cancellationReason: string;
  rescheduleReason: string;
  lines: BookingLine[];
  timeline: BookingTimelineEntry[];
}

export interface GetBookingsQuery {
  page: number;
  pageSize: number;
  search?: string;
  locationId?: string;
  status?: string;
}

export interface CreateBookingLineRequest {
  catalogItemId: string;
  quantity: number;
}

export interface CreateBookingRequest {
  customerId: string;
  locationId: string;
  startsAtUtc: string;
  notes?: string;
  lines: CreateBookingLineRequest[];
}

export interface RescheduleBookingRequest {
  startsAtUtc: string;
  reason?: string;
}

export interface CancelBookingRequest {
  reason: string;
}

export interface BookingDialogData {
  options: BookingOptions;
}

export interface BookingDialogResult {
  payload: CreateBookingRequest;
}

export interface BookingActionDialogData {
  mode: 'reschedule' | 'cancel';
  booking: BookingListItem;
}

export type BookingActionDialogResult =
  | {
      action: 'reschedule';
      payload: RescheduleBookingRequest;
    }
  | {
      action: 'cancel';
      payload: CancelBookingRequest;
    };
