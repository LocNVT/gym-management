import { Component } from '@angular/core';

@Component({
  selector: 'app-sidebar',
  standalone: false,
  templateUrl: './sidebar.html',
  styleUrls: ['./sidebar.scss']
})
export class SidebarComponent {
  menuItems = [
    { label: 'Thành viên', icon: 'people', route: '/members' },
    { label: 'Check-In', icon: 'login', route: '/check-in' },
    { label: 'Gói dịch vụ', icon: 'inventory_2', route: '/service-packages' },
    { label: 'Đăng ký DV', icon: 'assignment', route: '/member-data-service' },
    { label: 'Hóa đơn', icon: 'receipt_long', route: '/invoices' },
    { label: 'Chi tiết HĐ', icon: 'receipt', route: '/invoice-items' },
    { label: 'Chi phí', icon: 'payments', route: '/expenses' },
    { label: 'Huấn luyện viên', icon: 'fitness_center', route: '/trainers' },
    { label: 'Tổng quan điểm danh', icon: 'dashboard', route: '/attendance-dashboard' },
    { label: 'Điểm danh vân tay', icon: 'fingerprint', route: '/fingerprint-attendance' },
    { label: 'Thiết bị điểm danh', icon: 'devices', route: '/attendance-devices' }
  ];
}
