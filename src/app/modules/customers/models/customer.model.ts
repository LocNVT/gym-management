export interface Customer {
  id: string;
  personalInfo: PersonalInfo;
  membership: MembershipInfo;
  healthInfo: HealthInfo;
  attendance: Attendance;
  financial: Financial;
  createdAt?: Date;
  updatedAt?: Date;
}

export interface PersonalInfo {
  firstName: string;
  lastName: string;
  dateOfBirth: Date;
  gender: 'male' | 'female' | 'other';
  phone: string;
  email: string;
  address: string;
  emergencyContact: string;
  emergencyPhone: string;
}

export interface MembershipInfo {
  membershipType: 'basic' | 'premium' | 'vip';
  startDate: Date;
  endDate: Date;
  status: 'active' | 'inactive' | 'expired' | 'suspended';
  paymentStatus: 'paid' | 'pending' | 'overdue';
  autoRenewal: boolean;
}

export interface HealthInfo {
  medicalConditions: string[];
  allergies: string[];
  fitnessGoals: string[];
  previousInjuries: string;
  notes: string;
}

export interface Attendance {
  totalVisits: number;
  lastVisit: Date;
  averageVisitsPerWeek: number;
  favoriteTimeSlot: string;
}

export interface Financial {
  totalPaid: number;
  outstandingBalance: number;
  paymentMethod: 'cash' | 'card' | 'bank_transfer' | 'mobile_payment';
  lastPaymentDate: Date;
}
