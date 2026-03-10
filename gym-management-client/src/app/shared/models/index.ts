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
