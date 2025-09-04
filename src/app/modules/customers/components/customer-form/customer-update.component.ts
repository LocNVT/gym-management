import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import { Customer } from '../../models/customer.model';
import notify from 'devextreme/ui/notify';

@Component({
  selector: 'app-customer-update',
  templateUrl: './customer-update.component.html',
  styleUrls: ['./customer-update.component.scss'],
})
export class CustomerUpdateComponent implements OnInit {
  @Input() customerId?: number;
  @Input() mode: 'view' | 'edit' | 'create' = 'view';
  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() customerSaved = new EventEmitter<Customer>();

  visible: boolean = false;
  customer: Customer | undefined = {
    id: '',
    personalInfo: {
      firstName: '',
      lastName: '',
      dateOfBirth: new Date(),
      gender: 'male',
      phone: '',
      email: '',
      address: '',
      emergencyContact: '',
      emergencyPhone: '',
    },
    membership: {
      membershipType: 'basic',
      startDate: new Date(),
      endDate: new Date(),
      status: 'active',
      paymentStatus: 'pending',
      autoRenewal: false,
    },
    healthInfo: {
      medicalConditions: [],
      allergies: [],
      fitnessGoals: [],
      previousInjuries: '',
      notes: '',
    },
    attendance: {
      totalVisits: 0,
      lastVisit: new Date(),
      averageVisitsPerWeek: 0,
      favoriteTimeSlot: '',
    },
    financial: {
      totalPaid: 0,
      outstandingBalance: 0,
      paymentMethod: 'cash',
      lastPaymentDate: new Date(),
    },
    createdAt: new Date(),
    updatedAt: undefined,
  };
  originalCustomer: Customer = this.getEmptyCustomer();

  // Form validation rules
  validationRules = {
    required: { type: 'required', message: 'Trường này là bắt buộc' },
    email: { type: 'email', message: 'Email không hợp lệ' },
    phone: {
      type: 'pattern',
      pattern: /^[0-9]{10,11}$/,
      message: 'Số điện thoại phải có 10-11 chữ số',
    },
  };

  tabsData = [];

  // Dropdown data sources
  genderOptions = [
    { value: 'male', text: 'Nam' },
    { value: 'female', text: 'Nữ' },
    { value: 'other', text: 'Khác' },
  ];

  membershipOptions = [
    { value: 'basic', text: 'Basic - 500K/tháng' },
    { value: 'premium', text: 'Premium - 800K/tháng' },
    { value: 'vip', text: 'VIP - 1.2M/tháng' },
  ];

  statusOptions = [
    { value: 'active', text: 'Hoạt động' },
    { value: 'inactive', text: 'Tạm dừng' },
    { value: 'expired', text: 'Hết hạn' },
    { value: 'suspended', text: 'Bị đình chỉ' },
  ];

  paymentStatusOptions = [
    { value: 'paid', text: 'Đã thanh toán' },
    { value: 'pending', text: 'Chờ thanh toán' },
    { value: 'overdue', text: 'Quá hạn' },
  ];

  paymentMethodOptions = [
    { value: 'cash', text: 'Tiền mặt' },
    { value: 'card', text: 'Thẻ' },
    { value: 'bank_transfer', text: 'Chuyển khoản' },
    { value: 'mobile_payment', text: 'Ví điện tử' },
  ];

  medicalConditionsOptions = [
    'Tim mạch',
    'Tiểu đường',
    'Cao huyết áp',
    'Hen suyễn',
    'Đau lưng mãn tính',
    'Viêm khớp',
    'Khác',
  ];

  fitnessGoalsOptions = [
    'Giảm cân',
    'Tăng cân',
    'Tăng cơ bắp',
    'Cải thiện sức khỏe tim mạch',
    'Tăng sức mạnh',
    'Cải thiện độ linh hoạt',
    'Giảm stress',
    'Thi đấu thể thao',
  ];
  new: any;

  ngOnInit() {
    if (this.customerId) {
      this.loadCustomer(this.customerId);
    } else if (this.mode === 'create') {
      this.customer = this.getEmptyCustomer();
    }
  }

  loadCustomer(id: number) {
    // Mock data - replace with actual API call
    // this.customer = {
    //   id: id,
    //   personalInfo: {
    //     firstName: 'Nguyễn Văn',
    //     lastName: 'An',
    //     dateOfBirth: new Date('1990-05-15'),
    //     gender: 'male',
    //     phone: '0123456789',
    //     email: 'nguyenvanan@gmail.com',
    //     address: '123 Đường ABC, Quận 1, TP.HCM',
    //     emergencyContact: 'Nguyễn Thị Bình',
    //     emergencyPhone: '0987654321',
    //   },
    //   membership: {
    //     membershipType: 'premium',
    //     startDate: new Date('2024-01-15'),
    //     endDate: new Date('2024-07-15'),
    //     status: 'active',
    //     paymentStatus: 'paid',
    //     autoRenewal: true,
    //   },
    //   healthInfo: {
    //     medicalConditions: ['Cao huyết áp'],
    //     allergies: ['Đậu phộng'],
    //     fitnessGoals: ['Giảm cân', 'Cải thiện sức khỏe tim mạch'],
    //     previousInjuries: 'Chấn thương đầu gối năm 2022',
    //     notes: 'Khách hàng rất tích cực, cần theo dõi huyết áp',
    //   },
    //   attendance: {
    //     totalVisits: 145,
    //     lastVisit: new Date('2024-09-03'),
    //     averageVisitsPerWeek: 4.2,
    //     favoriteTimeSlot: '18:00 - 20:00',
    //   },
    //   financial: {
    //     totalPaid: 4800000,
    //     outstandingBalance: 0,
    //     paymentMethod: 'bank_transfer',
    //     lastPaymentDate: new Date('2024-08-01'),
    //   },
    //   createdAt: new Date('2024-01-15'),
    //   updatedAt: new Date('2024-09-01'),
    // };

    this.originalCustomer = JSON.parse(JSON.stringify(this.customer));
  }

  openPopup(data: any) {
    console.log('Open popup', data);
    this.visible = true;
    this.loadCustomer(data.id);
  }

  getEmptyCustomer(): Customer {
    return {
      id: '',
      personalInfo: {
        firstName: '',
        lastName: '',
        dateOfBirth: new Date(),
        gender: 'male',
        phone: '',
        email: '',
        address: '',
        emergencyContact: '',
        emergencyPhone: '',
      },
      membership: {
        membershipType: 'basic',
        startDate: new Date(),
        endDate: new Date(),
        status: 'active',
        paymentStatus: 'pending',
        autoRenewal: false,
      },
      healthInfo: {
        medicalConditions: [],
        allergies: [],
        fitnessGoals: [],
        previousInjuries: '',
        notes: '',
      },
      attendance: {
        totalVisits: 0,
        lastVisit: new Date(),
        averageVisitsPerWeek: 0,
        favoriteTimeSlot: '',
      },
      financial: {
        totalPaid: 0,
        outstandingBalance: 0,
        paymentMethod: 'cash',
        lastPaymentDate: new Date(),
      },
      createdAt: new Date(),
      updatedAt: undefined,
    };
  }

  onSave() {
    // Validate form
    if (!this.validateForm()) {
      return;
    }

    // Emit save event
    this.customerSaved.emit(this.customer);

    // Show success notification
    notify('Lưu thông tin khách hàng thành công!', 'success', 3000);

    // Close popup
    this.onCancel();
  }

  onCancel() {
    if (this.mode === 'edit') {
      // Restore original data
      this.customer = JSON.parse(JSON.stringify(this.originalCustomer));
    }

    this.visible = false;
    this.visibleChange.emit(false);
  }

  validateForm(): boolean {
    if (!this.customer) {
 return false;
    }

    const { personalInfo } = this.customer;

    if (
      !personalInfo.firstName ||
      !personalInfo.lastName ||
      !personalInfo.phone ||
      !personalInfo.email
    ) {
      notify('Vui lòng điền đầy đủ thông tin bắt buộc', 'error', 3000);
      return false;
    }

    return true;
  }

  getFullName(): string {
    const { firstName, lastName } = this.customer?.personalInfo || {};
    return `${firstName} ${lastName}`.trim();
  }

  getStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      active: '#4CAF50',
      inactive: '#FF9800',
      expired: '#F44336',
      suspended: '#9C27B0',
    };
    return colors[status] || '#666';
  }

  getPaymentStatusColor(status: string): string {
    const colors: { [key: string]: string } = {
      paid: '#4CAF50',
      pending: '#FF9800',
      overdue: '#F44336',
    };
    return colors[status] || '#666';
  }

  calculateAge(): number {
    const today = new Date();
    const birthDate = new Date(this.customer?.personalInfo.dateOfBirth ?? '');
    let age = today.getFullYear() - birthDate.getFullYear();
    const monthDiff = today.getMonth() - birthDate.getMonth();

    if (
      monthDiff < 0 ||
      (monthDiff === 0 && today.getDate() < birthDate.getDate())
    ) {
      age--;
    }

    return age;
  }

  getDaysUntilExpiry(): number {
    const today = new Date();
    const endDate = new Date(this.customer?.membership.endDate ?? '');
    const diffTime = endDate.getTime() - today.getTime();
    return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
  }

  onMembershipTypeChange(e: any) {
    // Auto-calculate end date based on membership type
    const startDate = new Date(this.customer?.membership.startDate ?? '');
    const months = e.value === 'basic' ? 1 : e.value === 'premium' ? 3 : 6;

    const endDate = new Date(startDate);
    endDate.setMonth(endDate.getMonth() + months);
    if(!this.customer){
      return
    }

    this.customer.membership.endDate = endDate ?? '';
  }

  printCustomerInfo() {
    window.print();
  }

  exportToPdf() {
    // Implementation for PDF export
    notify('Xuất PDF thành công!', 'success', 3000);
  }

  getStatusText = () => {
    return (
      this.statusOptions.find(
        (s) => s.value === this.customer?.membership.status
      )?.text ?? ''
    );
  };

  getPaymentStatusText = () => {
    return (
      this.paymentStatusOptions.find(
        (s) => s.value === this.customer?.membership.paymentStatus
      )?.text ?? ''
    );
  };
}
