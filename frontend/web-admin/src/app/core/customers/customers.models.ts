export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface Customer {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phoneNumber: string;
  notes: string;
  isActive: boolean;
  bookingCount: number;
  createdAtUtc: string;
}

export interface GetCustomersQuery {
  page: number;
  pageSize: number;
  search?: string;
  isActive?: boolean;
}

export interface CreateCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  notes?: string;
}

export interface UpdateCustomerRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  notes?: string;
  isActive: boolean;
}

export interface CustomerDialogData {
  mode: 'create' | 'edit';
  customer?: Customer;
}

export type CustomerDialogResult =
  | {
      action: 'create';
      payload: CreateCustomerRequest;
    }
  | {
      action: 'update';
      customerId: string;
      payload: UpdateCustomerRequest;
    };
