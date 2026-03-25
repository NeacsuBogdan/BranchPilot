import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  type CreateCustomerRequest,
  type Customer,
  type GetCustomersQuery,
  type PagedResponse,
  type UpdateCustomerRequest,
} from './customers.models';

@Injectable({ providedIn: 'root' })
export class CustomersApiService {
  private readonly http = inject(HttpClient);

  list(query: GetCustomersQuery): Promise<PagedResponse<Customer>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    if (typeof query.isActive === 'boolean') {
      params = params.set('isActive', query.isActive);
    }

    return firstValueFrom(this.http.get<PagedResponse<Customer>>('/api/customers', { params }));
  }

  create(request: CreateCustomerRequest): Promise<Customer> {
    return firstValueFrom(this.http.post<Customer>('/api/customers', request));
  }

  update(customerId: string, request: UpdateCustomerRequest): Promise<Customer> {
    return firstValueFrom(this.http.put<Customer>(`/api/customers/${customerId}`, request));
  }

  delete(customerId: string): Promise<void> {
    return firstValueFrom(this.http.delete<void>(`/api/customers/${customerId}`));
  }
}
