import { Component, AfterViewInit, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../shared/services/auth.service';

@Component({
    selector: 'app-login',
    standalone: false,
    templateUrl: './login.component.html',
    styleUrls: ['./login.component.scss']
})
export class LoginComponent implements AfterViewInit {
    @ViewChild('googleBtn') googleBtn!: ElementRef;

    loginForm: FormGroup;
    isLoading = false;
    errorMessage = '';
    hidePassword = true;

    constructor(
        private fb: FormBuilder,
        private authService: AuthService,
        private router: Router
    ) {
        this.loginForm = this.fb.group({
            username: ['', Validators.required],
            password: ['', Validators.required]
        });

        if (this.authService.isAuthenticated()) {
            this.router.navigate(['/members']);
        }
    }

    ngAfterViewInit(): void {
        setTimeout(() => {
            this.authService.initGoogleSignIn(
                this.googleBtn.nativeElement,
                (idToken: string) => this.handleGoogleLogin(idToken)
            );
        }, 500);
    }

    handleGoogleLogin(idToken: string): void {
        this.isLoading = true;
        this.errorMessage = '';
        this.authService.googleLogin(idToken).subscribe({
            next: () => {
                this.router.navigate(['/members']);
            },
            error: (err: any) => {
                this.isLoading = false;
                this.errorMessage = err.error?.message || 'Đăng nhập Google thất bại';
            }
        });
    }

    onSubmit(): void {
        if (this.loginForm.invalid) return;

        this.isLoading = true;
        this.errorMessage = '';

        this.authService.login(this.loginForm.value).subscribe({
            next: () => {
                this.router.navigate(['/members']);
            },
            error: (err: any) => {
                this.isLoading = false;
                this.errorMessage = err.error?.message || 'Sai tài khoản hoặc mật khẩu';
            }
        });
    }
}
