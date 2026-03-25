import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import {
  type AuthStorageState,
  type AuthenticatedSessionResponse,
  type CurrentSessionResponse,
  type LoginRequest,
  type RegisterOrganizationRequest,
} from './auth.models';

const storageKey = 'branchpilot.auth';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly initializedState = signal(false);
  private readonly accessTokenState = signal<string | null>(null);
  private readonly accessTokenExpiresAtState = signal<string | null>(null);
  private readonly refreshTokenState = signal<string | null>(null);
  private readonly refreshTokenExpiresAtState = signal<string | null>(null);
  private readonly sessionState = signal<CurrentSessionResponse | null>(null);

  private refreshPromise: Promise<boolean> | null = null;

  readonly initialized = this.initializedState.asReadonly();
  readonly session = this.sessionState.asReadonly();
  readonly isAuthenticated = computed(
    () => this.sessionState() !== null && this.accessTokenState() !== null,
  );

  async initialize(): Promise<void> {
    const persistedState = this.readStorage();

    if (!persistedState) {
      this.clearSession();
      this.initializedState.set(true);
      return;
    }

    this.restoreState(persistedState);

    try {
      await this.reloadSession();
    } catch {
      const refreshed = await this.refreshAccessToken();

      if (!refreshed) {
        this.clearSession();
      }
    } finally {
      this.initializedState.set(true);
    }
  }

  async login(request: LoginRequest): Promise<void> {
    const payload = await firstValueFrom(
      this.http.post<AuthenticatedSessionResponse>('/api/auth/login', request),
    );

    this.applyAuthenticatedSession(payload);
  }

  async registerOrganization(request: RegisterOrganizationRequest): Promise<void> {
    const payload = await firstValueFrom(
      this.http.post<AuthenticatedSessionResponse>('/api/auth/register-organization', request),
    );

    this.applyAuthenticatedSession(payload);
  }

  async reloadSession(): Promise<CurrentSessionResponse> {
    const session = await firstValueFrom(this.http.get<CurrentSessionResponse>('/api/auth/me'));
    this.sessionState.set(session);
    this.persistState();

    return session;
  }

  async refreshAccessToken(): Promise<boolean> {
    if (this.refreshPromise) {
      return this.refreshPromise;
    }

    const refreshToken = this.refreshTokenState();

    if (!refreshToken) {
      return false;
    }

    this.refreshPromise = (async () => {
      try {
        const payload = await firstValueFrom(
          this.http.post<AuthenticatedSessionResponse>('/api/auth/refresh', {
            refreshToken,
          }),
        );

        this.applyAuthenticatedSession(payload);
        return true;
      } catch {
        this.clearSession();
        return false;
      } finally {
        this.refreshPromise = null;
      }
    })();

    return this.refreshPromise;
  }

  async logout(): Promise<void> {
    const refreshToken = this.refreshTokenState();

    if (refreshToken) {
      try {
        await firstValueFrom(this.http.post('/api/auth/logout', { refreshToken }));
      } catch {
        // The local session still needs to be cleared even if the API is unavailable.
      }
    }

    this.clearSession();
  }

  getAccessToken(): string | null {
    return this.accessTokenState();
  }

  getRefreshToken(): string | null {
    return this.refreshTokenState();
  }

  clearSession(): void {
    this.accessTokenState.set(null);
    this.accessTokenExpiresAtState.set(null);
    this.refreshTokenState.set(null);
    this.refreshTokenExpiresAtState.set(null);
    this.sessionState.set(null);
    localStorage.removeItem(storageKey);
  }

  private applyAuthenticatedSession(payload: AuthenticatedSessionResponse): void {
    this.accessTokenState.set(payload.accessToken);
    this.accessTokenExpiresAtState.set(payload.accessTokenExpiresAtUtc);
    this.refreshTokenState.set(payload.refreshToken);
    this.refreshTokenExpiresAtState.set(payload.refreshTokenExpiresAtUtc);
    this.sessionState.set(payload.session);
    this.persistState();
  }

  private restoreState(state: AuthStorageState): void {
    this.accessTokenState.set(state.accessToken);
    this.accessTokenExpiresAtState.set(state.accessTokenExpiresAtUtc);
    this.refreshTokenState.set(state.refreshToken);
    this.refreshTokenExpiresAtState.set(state.refreshTokenExpiresAtUtc);
    this.sessionState.set(state.session);
  }

  private persistState(): void {
    const accessToken = this.accessTokenState();
    const refreshToken = this.refreshTokenState();

    if (!accessToken || !refreshToken) {
      localStorage.removeItem(storageKey);
      return;
    }

    const state: AuthStorageState = {
      accessToken,
      accessTokenExpiresAtUtc: this.accessTokenExpiresAtState() ?? '',
      refreshToken,
      refreshTokenExpiresAtUtc: this.refreshTokenExpiresAtState() ?? '',
      session: this.sessionState(),
    };

    localStorage.setItem(storageKey, JSON.stringify(state));
  }

  private readStorage(): AuthStorageState | null {
    const rawState = localStorage.getItem(storageKey);

    if (!rawState) {
      return null;
    }

    try {
      return JSON.parse(rawState) as AuthStorageState;
    } catch {
      localStorage.removeItem(storageKey);
      return null;
    }
  }
}
