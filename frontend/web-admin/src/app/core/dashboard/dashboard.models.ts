export interface DashboardUpcomingBooking {
  id: string;
  number: string;
  status: string;
  customerName: string;
  locationName: string;
  startsAtUtc: string;
  endsAtUtc: string;
}

export interface DashboardSummary {
  activeCustomers: number;
  scheduledBookings: number;
  confirmedBookings: number;
  nextSevenDaysBookings: number;
  upcomingBookings: DashboardUpcomingBooking[];
}
