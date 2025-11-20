import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, throwError, timer, timeout } from 'rxjs';
import { catchError, switchMap, retry } from 'rxjs';
import { AuthService } from '../services/auth.service';

const NO_RETRY_URLS = ['/auth/login', '/auth/register', '/auth/refresh'];
const REFRESH_TIMEOUT_MS = 10000;

export const errorInterceptor: HttpInterceptorFn = (req: HttpRequest<any>, next: HttpHandlerFn): Observable<HttpEvent<any>> => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    retry({
      count: 1,
      delay: (error, retryCount) => {
        if (error.status >= 500 && error.status < 600) {
          console.warn(`Server error ${error.status}, retrying...`);
          return timer(1000);
        }
        return throwError(() => error);
      }
    }),
    catchError((error: HttpErrorResponse) => {
      const isAuthEndpoint = NO_RETRY_URLS.some(url => req.url.includes(url));

      if (error.status === 401 && !isAuthEndpoint) {
        if (authService.isRefreshing()) {
          console.warn('Refresh in progress, waiting for completion');
          return authService.refreshCompleted$.pipe(
            timeout(REFRESH_TIMEOUT_MS),
            switchMap(() => {
              console.log('Refresh completed, retrying request');
              return next(req);
            }),
            catchError((timeoutError) => {
              if (timeoutError.name === 'TimeoutError') {
                console.error('Refresh timeout exceeded');
                authService.clearAuth();
                router.navigate(['/auth/login']);
              }
              return throwError(() => error);
            })
          );
        }

        console.warn('Token expired, attempting refresh');
        return authService.refreshToken().pipe(
          timeout(REFRESH_TIMEOUT_MS),
          switchMap(() => {
            console.log('Token refreshed, retrying request');
            return next(req);
          }),
          catchError((refreshError) => {
            console.error('Token refresh failed:', refreshError);
            authService.clearAuth();
            router.navigate(['/auth/login']);
            return throwError(() => error);
          })
        );
      }

      if (error.status === 403) {
        console.warn('Access forbidden:', error.error?.message || 'No permission');
        router.navigate(['/']);
        return throwError(() => error);
      }

      if (error.status === 404) {
        console.warn('Resource not found:', req.url);
        return throwError(() => error);
      }

      if (error.status >= 500) {
        console.error('Server error:', error.status, error.error?.message);
        return throwError(() => error);
      }

      console.error('HTTP Error:', error.status, error.error?.message || error.message);
      return throwError(() => error);
    })
  );
};