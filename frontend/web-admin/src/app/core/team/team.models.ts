import { type LocationSummary } from '../auth/auth.models';

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface MembershipDetails {
  id: string;
  role: string;
  permissions: string[];
  assignedLocations: AssignedLocation[];
}

export interface AssignedLocation {
  id: string;
  name: string;
  code: string;
}

export interface TeamMember {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  isCurrentUser: boolean;
  membership: MembershipDetails;
}

export interface RoleOption {
  code: string;
  name: string;
  description: string;
  permissions: string[];
}

export interface TeamMembersQuery {
  page: number;
  pageSize: number;
  search?: string;
}

export interface CreateUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  role: string;
  locationIds: string[];
}

export interface UpdateUserMembershipRequest {
  role: string;
  isActive: boolean;
  locationIds: string[];
}

export interface TeamMemberDialogData {
  mode: 'create' | 'edit';
  roleOptions: RoleOption[];
  locations: LocationSummary[];
  member?: TeamMember;
}

export type TeamMemberDialogResult =
  | {
      action: 'create';
      payload: CreateUserRequest;
    }
  | {
      action: 'update';
      userId: string;
      payload: UpdateUserMembershipRequest;
    };
