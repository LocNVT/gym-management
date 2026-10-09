import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

/** Admin-only routes (Role 1) - e.g. account management. Staff (Role 0) are redirected home. */
@Injectable({ providedIn: 'root' })
export class RoleGuard implements CanActivate {
    constructor(private authService: AuthService, private router: Router) { }

    canActivate(): boolean {
        if (this.authService.getUser()?.role === 1) {
            return true;
        }
        this.router.navigate(['/dashboard']);
        return false;
    }
}
