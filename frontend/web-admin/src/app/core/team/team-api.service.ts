import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  type CreateUserRequest,
  type PagedResponse,
  type RoleOption,
  type TeamMember,
  type TeamMembersQuery,
  type UpdateUserMembershipRequest,
} from './team.models';

@Injectable({ providedIn: 'root' })
export class TeamApiService {
  private readonly http = inject(HttpClient);

  list(query: TeamMembersQuery): Promise<PagedResponse<TeamMember>> {
    let params = new HttpParams()
      .set('page', query.page)
      .set('pageSize', query.pageSize);

    if (query.search) {
      params = params.set('search', query.search);
    }

    return firstValueFrom(this.http.get<PagedResponse<TeamMember>>('/api/users', { params }));
  }

  getRoleOptions(): Promise<RoleOption[]> {
    return firstValueFrom(this.http.get<RoleOption[]>('/api/users/roles'));
  }

  createUser(request: CreateUserRequest): Promise<TeamMember> {
    return firstValueFrom(this.http.post<TeamMember>('/api/users', request));
  }

  updateMembership(userId: string, request: UpdateUserMembershipRequest): Promise<TeamMember> {
    return firstValueFrom(this.http.put<TeamMember>(`/api/users/${userId}/membership`, request));
  }
}
