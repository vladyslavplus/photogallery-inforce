import { Injectable, signal, computed, inject, DestroyRef, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, Subject, timer, tap, switchMap, throwError } from 'rxjs';
import { jwtDecode } from 'jwt-decode';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { environment } from '../../../environments/environment.development';
import { StorageService } from './storage.service';
import {
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  DecodedToken,
  RefreshConfig,
} from '../models/auth.models';

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private readonly apiUrl = `${environment.apiUrl}/auth`;
  private readonly config: RefreshConfig = {
    maxRetries: 3,
    baseDelayMs: 1000,
    refreshThresholdSeconds: 60,
  };

  private currentUserSignal = signal<DecodedToken | null>(null);
  private refreshInProgressSignal = signal(false);
  private refreshRetryCountSignal = signal(0);

  private refreshCompletedSubject = new Subject<void>();
  public refreshCompleted$ = this.refreshCompletedSubject.asObservable();

  public isAuthenticated = computed(() => this.currentUserSignal() !== null);
  public currentUser = computed(() => this.currentUserSignal());
  public isRefreshing = computed(() => this.refreshInProgressSignal());

  public isAdmin = computed(() => {
    const user = this.currentUserSignal();
    if (!user) return false;
    const userAny = user as any;
    const role =
      userAny.role || userAny['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
    if (!role) return false;
    const roles = Array.isArray(role) ? role : [role];
    return roles.includes('Admin');
  });

  private destroyRef = inject(DestroyRef);

  constructor(private http: HttpClient, private storage: StorageService, private router: Router) {
    this.loadUserFromToken();
    this.setupProactiveRefresh();
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/register`, request)
      .pipe(tap((response) => this.handleAuthResponse(response)));
  }

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${this.apiUrl}/login`, request, { withCredentials: true })
      .pipe(
        tap((response) => {
          this.handleAuthResponse(response);
          this.resetRefreshRetry();
        })
      );
  }

  logout(): Observable<any> {
    return this.http
      .post(`${this.apiUrl}/logout`, {}, { withCredentials: true })
      .pipe(tap(() => this.clearAuth()));
  }

  refreshToken(): Observable<AuthResponse> {
    if (this.refreshInProgressSignal()) {
      return this.refreshCompleted$.pipe(
        switchMap(() => {
          const token = this.storage.getAccessToken();
          if (!token) return throwError(() => new Error('No token after refresh'));
          return new Observable<AuthResponse>((observer) => observer.complete());
        })
      );
    }

    this.refreshInProgressSignal.set(true);

    return this.http
      .post<AuthResponse>(`${this.apiUrl}/refresh`, {}, { withCredentials: true })
      .pipe(
        tap({
          next: (res) => {
            this.handleAuthResponse(res);
            this.refreshInProgressSignal.set(false);
            this.refreshCompletedSubject.next();
            this.resetRefreshRetry();
          },
          error: (err) => {
            console.error('Token refresh failed', err);
            this.refreshInProgressSignal.set(false);
            this.refreshCompletedSubject.next();
          },
        })
      );
  }

  isTokenValid(): boolean {
    const token = this.storage.getAccessToken();
    if (!token) return false;
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      return decoded.exp * 1000 > Date.now();
    } catch {
      return false;
    }
  }

  getTokenExpiresIn(): number {
    const token = this.storage.getAccessToken();
    if (!token) return 0;
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      return Math.floor((decoded.exp * 1000 - Date.now()) / 1000);
    } catch {
      return 0;
    }
  }

  isOwner(userId: string): boolean {
    const user = this.currentUserSignal();
    return user?.sub === userId;
  }

  private getTimeToRefresh(): number {
    const expiresIn = this.getTokenExpiresIn();
    const refreshIn = expiresIn - this.config.refreshThresholdSeconds;
    return Math.max(0, Math.min(refreshIn, expiresIn));
  }

  private setupProactiveRefresh(): void {
    effect(() => {
      if (this.currentUserSignal()) {
        this.scheduleNextRefresh();
      }
    });
  }

  private scheduleNextRefresh(): void {
    const timeMs = this.getTimeToRefresh() * 1000;

    if (timeMs <= 0) {
      console.warn('Token expires soon, refreshing immediately');
      this.refreshToken().subscribe({ error: () => console.error('Immediate refresh failed') });
      return;
    }

    console.log(`Next token refresh scheduled in ${timeMs / 1000}s`);

    timer(timeMs)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        console.log('Running proactive token refresh');
        this.refreshToken().subscribe({
          next: () => this.scheduleNextRefresh(),
          error: () => this.scheduleNextRefresh(),
        });
      });
  }

  private resetRefreshRetry() {
    this.refreshRetryCountSignal.set(0);
  }

  private handleAuthResponse(response: AuthResponse) {
    this.storage.setAccessToken(response.accessToken);
    this.loadUserFromToken();
  }

  private loadUserFromToken() {
    const token = this.storage.getAccessToken();
    if (!token) {
      this.currentUserSignal.set(null);
      return;
    }
    try {
      const decoded = jwtDecode<DecodedToken>(token);
      if (decoded.exp * 1000 < Date.now()) {
        this.clearAuth();
        return;
      }
      this.currentUserSignal.set(decoded);
      console.log('User loaded from token:', decoded.sub);
    } catch (err) {
      console.error('Failed to decode token:', err);
      this.clearAuth();
    }
  }

  clearAuth(): void {
    this.storage.clear();
    this.currentUserSignal.set(null);
    this.router.navigate(['/auth/login']).catch((err) => console.error(err));
  }
}
