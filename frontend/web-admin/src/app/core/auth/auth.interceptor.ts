import { HttpErrorResponse, type HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';

import { AuthService } from './auth.service';

const anonymousEndpoints = new Set([
  '/api/auth/login',
  '/api/auth/register-organization',
  '/api/auth/refresh',
]);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const isApiRequest = request.url.startsWith('/api');
  const isAnonymousRequest = anonymousEndpoints.has(request.url);
  const accessToken = authService.getAccessToken();

  const authenticatedRequest =
    isApiRequest && !isAnonymousRequest && accessToken
      ? request.clone({
          setHeaders: {
            Authorization: `Bearer ${accessToken}`,
          },
        })
      : request;

  return next(authenticatedRequest).pipe(
    catchError((error: unknown) => {
      if (
        !(error instanceof HttpErrorResponse) ||
        error.status !== 401 ||
        !isApiRequest ||
        isAnonymousRequest ||
        !authService.getRefreshToken()
      ) {
        return throwError(() => error);
      }

      return from(authService.refreshAccessToken()).pipe(
        switchMap((refreshed) => {
          if (!refreshed) {
            return throwError(() => error);
          }

          const refreshedAccessToken = authService.getAccessToken();

          if (!refreshedAccessToken) {
            return throwError(() => error);
          }

          return next(
            request.clone({
              setHeaders: {
                Authorization: `Bearer ${refreshedAccessToken}`,
              },
            }),
          );
        }),
        catchError((refreshError) => {
          authService.clearSession();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
