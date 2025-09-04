import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  title = 'gym-management';
  
  menuItems = [
    { id: 'dashboard', text: 'Dashboard', icon: 'chart', path: '/dashboard' },
    { id: 'customers', text: 'Quản lý khách hàng', icon: 'group', path: '/customers' },
    { id: 'services', text: 'Quản lý gói tập', icon: 'card', path: '/services' },
    { id: 'attendance', text: 'Điểm danh', icon: 'check', path: '/attendance' },
    { id: 'finance', text: 'Tài chính', icon: 'money', path: '/finance' }
  ];

  drawerOpened = true;
  
  toolbarItems = [
    {
      widget: 'dxButton',
      location: 'before',
      options: {
        icon: 'menu',
        onClick: () => this.drawerOpened = !this.drawerOpened
      }
    },
    {
      text: 'FitGym Manager',
      location: 'before'
    },
    {
      widget: 'dxButton',
      location: 'after',
      options: {
        icon: 'user',
        text: 'Profile'
      }
    }
  ];

  constructor(private router: Router) {}

  onMenuItemClick(e: any) {
    if (e.itemData && e.itemData.path) {
      this.router.navigate([e.itemData.path]);
    }
  }
}