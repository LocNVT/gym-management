import { Injectable, NgZone } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';

const API_BASE = '/api/Auth';
const GOOGLE_CLIENT_ID = '622852160112-4vf6aacujd838uf1p13fl6e3395rjr3r.apps.googleusercontent.com';

export interface LoginInput {
    username: string;
    password: string;
}

export interface RegisterInput {
    username: string;
    email: string;
    password: string;
    fullName: string;
    /** Name of the new gym branch this registration creates (server defaults it if left blank). */
    tenantName?: string;
}

export interface AuthResponse {
    token: string;
    [key: string]: any;
}

declare const google: any;

@Injectable({ providedIn: 'root' })
export class AuthService {
    private readonly TOKEN_KEY = 'gym_auth_token';
    private readonly USER_KEY = 'gym_auth_user';

    constructor(
        private http: HttpClient,
        private router: Router,
        private ngZone: NgZone
    ) { }

    login(data: LoginInput): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${API_BASE}/login`, data).pipe(
            tap(res => {
                if (res.token) {
                    localStorage.setItem(this.TOKEN_KEY, res.token);
                    localStorage.setItem(this.USER_KEY, JSON.stringify(res));
                }
            })
        );
    }

    googleLogin(idToken: string): Observable<AuthResponse> {
        return this.http.post<AuthResponse>(`${API_BASE}/google`, { idToken }).pipe(
            tap(res => {
                if (res.token) {
                    localStorage.setItem(this.TOKEN_KEY, res.token);
                    localStorage.setItem(this.USER_KEY, JSON.stringify(res));
                }
            })
        );
    }

    initGoogleSignIn(buttonElement: HTMLElement, callback: (idToken: string) => void): void {
        if (typeof google === 'undefined') {
            console.warn('Google Identity Services not loaded yet');
            return;
        }
        google.accounts.id.initialize({
            client_id: GOOGLE_CLIENT_ID,
            callback: (response: any) => {
                this.ngZone.run(() => callback(response.credential));
            }
        });
        google.accounts.id.renderButton(buttonElement, {
            theme: 'outline',
            size: 'large',
            width: '100%',
            text: 'signin_with',
            locale: 'vi'
        });
    }

    register(data: RegisterInput): Observable<any> {
        return this.http.post<any>(`${API_BASE}/register`, data);
    }

    logout(): void {
        localStorage.removeItem(this.TOKEN_KEY);
        localStorage.removeItem(this.USER_KEY);
        this.router.navigate(['/login']);
    }

    getToken(): string | null {
        return localStorage.getItem(this.TOKEN_KEY);
    }

    isAuthenticated(): boolean {
        return !!this.getToken();
    }

    getUser(): any {
        const user = localStorage.getItem(this.USER_KEY);
        return user ? JSON.parse(user) : null;
    }
}
