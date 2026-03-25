export interface UserSummary {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
}

export interface TenantSummary {
  id: string;
  name: string;
  slug: string;
}

export interface LocationSummary {
  id: string;
  name: string;
  code: string;
  timeZone: string;
}

export interface CurrentSessionResponse {
  user: UserSummary;
  tenant: TenantSummary;
  locations: LocationSummary[];
}

export interface AuthenticatedSessionResponse {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  session: CurrentSessionResponse;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterOrganizationRequest {
  tenantName: string;
  primaryLocationName: string;
  primaryLocationCode: string;
  primaryLocationTimeZone: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
}

export interface AuthStorageState {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
  session: CurrentSessionResponse | null;
}
