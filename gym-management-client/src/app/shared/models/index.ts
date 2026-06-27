// ============ CheckIn ============
export interface CheckIn {
    id: string;
    memberId: string;
    checkInTime: string;
    method: number;
    notes?: string;
}

export interface CheckInCreateInput {
    memberId: string;
    checkInTime: string;
    method: number;
    notes?: string;
}

export interface CheckInUpdateInput {
    memberId: string;
    checkInTime: string;
    method: number;
    notes?: string;
}

// ============ Expense ============
export interface Expense {
    id: string;
    expenseDate: string;
    category?: string;
    description?: string;
    amount: number;
    paymentMethod: number;
    notes?: string;
}

export interface ExpenseInput {
    expenseDate: string;
    category?: string;
    description?: string;
    amount: number;
    paymentMethod: number;
    notes?: string;
}

// ============ Invoice ============
export interface Invoice {
    id: string;
    invoiceNumber?: string;
    memberId?: string;
    invoiceDate: string;
    totalAmount: number;
    status: number;
    paymentMethod: number;
    notes?: string;
}

export interface InvoiceInput {
    invoiceNumber?: string;
    memberId?: string;
    invoiceDate: string;
    totalAmount: number;
    status: number;
    paymentMethod: number;
    notes?: string;
}

// ============ InvoiceItem ============
export interface InvoiceItem {
    id: string;
    invoiceId: string;
    description?: string;
    quantity: number;
    unitPrice: number;
    servicePackageId?: number;
}

export interface InvoiceItemInput {
    invoiceId: string;
    description?: string;
    quantity: number;
    unitPrice: number;
    servicePackageId?: number;
}

// ============ Member ============
export interface Member {
    id: string;
    fullName?: string;
    dateOfBirth?: string;
    gender?: number;
    phoneNumber?: string;
    email?: string;
    address?: string;
    emergencyName?: string;
    emergencyPhone?: string;
    registrationDate: string;
    status: number;
    notes?: string;
    createdAt: string;
    updatedAt?: string;
    isDeleted: boolean;
    avatarUrl?: string;
}

export interface MemberInput {
    fullName?: string;
    dateOfBirth?: string;
    gender?: number;
    phoneNumber?: string;
    email?: string;
    address?: string;
    emergencyName?: string;
    emergencyPhone?: string;
    registrationDate: string;
    status: number;
    notes?: string;
    createdAt: string;
    updatedAt?: string;
    isDeleted: boolean;
    avatarUrl?: string;
}

// ============ MemberDataService ============
export interface MemberDataService {
    id: string;
    memberId: string;
    servicePackageId: string;
    startDate: string;
    endDate: string;
    priceAtPurchase: number;
    remainingCheckins?: number;
    status: number;
    createdAt: string;
    updatedAt?: string;
}

export interface MemberDataServiceInput {
    id: string;
    memberId: string;
    servicePackageId: string;
    startDate: string;
    endDate: string;
    priceAtPurchase: number;
    remainingCheckins?: number;
    status: number;
    createdAt: string;
    updatedAt?: string;
}

// ============ Trainer ============
export interface Trainer {
    id: string;
    fullName: string;
    phoneNumber?: string;
    email?: string;
    specialty?: string;
    hourlyRate: number;
    status: number;
    notes?: string;
}

export interface TrainerInput {
    fullName: string;
    phoneNumber?: string;
    email?: string;
    specialty?: string;
    hourlyRate: number;
    status: number;
    notes?: string;
}

// ============ ServicePackage ============
export interface ServicePackage {
    id: string;
    name?: string;
    description?: string;
    price: number;
    durationDays: number;
    maxCheckins?: number;
    isActive: boolean;
}

export interface ServicePackageInput {
    name?: string;
    description?: string;
    price: number;
    durationDays: number;
    maxCheckins?: number;
    isActive: boolean;
}

// ============ Fingerprint Attendance ============
export interface FingerprintTemplate {
    id: string;
    memberId: string;
    fingerPosition: number;
    vendor: string;
    quality: number;
    createdAt: string;
    updatedAt?: string;
}

export interface FingerprintTemplateInput {
    memberId: string;
    fingerPosition: number;
    capturedTemplate: string; // base64 template bytes (never an image)
    vendor: string;
    quality: number;
}

export interface VerifyFingerprintInput {
    deviceId: string;
    capturedTemplate: string;
    operatorUserId?: string;
}

export interface VerifyFingerprintResult {
    matched: boolean;
    score: number;
    memberId?: string;
    memberName?: string;
    action: 'check-in' | 'check-out' | 'none';
    checkInId?: string;
    timestamp?: string;
}

export interface AttendanceDevice {
    id: string;
    name: string;
    location?: string;
    vendor: string;
    serialNumber?: string;
    isActive: boolean;
    createdAt: string;
    updatedAt?: string;
}

export interface AttendanceDeviceInput {
    name: string;
    location?: string;
    vendor: string;
    serialNumber?: string;
    isActive: boolean;
}

export interface Attendance {
    id: string;
    memberId: string;
    checkInTime: string;
    checkOutTime?: string;
    method: number;
    deviceId?: string;
    operatorUserId?: string;
    notes?: string;
}
