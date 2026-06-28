import { Component } from '@angular/core';

export interface MenuItem {
  label: string;
  icon: string;
  route: string;
}

export interface MenuGroup {
  title: string;
  items: MenuItem[];
}

@Component({
  selector: 'app-sidebar',
  standalone: false,
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss']
})
export class SidebarComponent {
  menuGroups: MenuGroup[] = [
    {
      title: 'Quản lý',
      items: [
        { label: 'Thành viên', icon: 'people', route: '/members' },
        { label: 'Check-In', icon: 'login', route: '/check-in' },
        { label: 'Huấn luyện viên', icon: 'sports', route: '/trainers' },
      ]
    },
    {
      title: 'Dịch vụ & Tài chính',
      items: [
        { label: 'Gói dịch vụ', icon: 'inventory_2', route: '/service-packages' },
        { label: 'Đăng ký DV', icon: 'assignment', route: '/member-data-service' },
        { label: 'Hóa đơn', icon: 'receipt_long', route: '/invoices' },
        { label: 'Chi tiết HĐ', icon: 'receipt', route: '/invoice-items' },
        { label: 'Chi phí', icon: 'payments', route: '/expenses' },
      ]
    },
    {
      title: 'Điểm danh',
      items: [
        { label: 'Tổng quan', icon: 'dashboard', route: '/attendance-dashboard' },
        { label: 'Vân tay', icon: 'fingerprint', route: '/fingerprint-attendance' },
        { label: 'Thiết bị', icon: 'devices', route: '/attendance-devices' },
      ]
    }
  ];
}
