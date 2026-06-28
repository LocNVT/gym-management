import { Component } from '@angular/core';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-header',
  standalone: false,
  templateUrl: './header.html',
  styleUrls: ['./header.scss']
})
export class HeaderComponent {
  constructor(public authService: AuthService) { }

  get displayName(): string {
    const user = this.authService.getUser();
    return user?.fullName || user?.username || 'Admin';
  }

  logout(): void {
    this.authService.logout();
  }
}
