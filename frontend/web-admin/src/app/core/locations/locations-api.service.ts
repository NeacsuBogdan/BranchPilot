import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { type LocationSummary } from '../auth/auth.models';

export interface CreateLocationRequest {
  name: string;
  code: string;
  timeZone: string;
}

@Injectable({ providedIn: 'root' })
export class LocationsApiService {
  private readonly http = inject(HttpClient);

  list(): Promise<LocationSummary[]> {
    return firstValueFrom(this.http.get<LocationSummary[]>('/api/locations'));
  }

  create(request: CreateLocationRequest): Promise<LocationSummary> {
    return firstValueFrom(this.http.post<LocationSummary>('/api/locations', request));
  }
}
