import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { BaseApiService } from './base-api.service';
import { QueryParams } from '../models/common.model';

export interface AttendanceRecord {
  id: number;
  customerId: number;
  customerName: string;
  checkInTime: Date;
  checkOutTime?: Date;
  duration?: number; // minutes
  status: 'checked-in' | 'checked-out';
  notes?: string;
  createdAt: Date;
}

export interface AttendanceStats {
  todayCheckIns: number;
  currentlyInGym: number;
  averageDailyCheckIns: number;
  peakHour: string;
  totalVisitsThisMonth: number;
  averageSessionDuration: number;
}

@Injectable({
  providedIn: 'root'
})
export class AttendanceApiService extends BaseApiService {

  // Check-in customer
  checkIn(customerId: number, notes?: string): Observable<AttendanceRecord> {
    return this.post<AttendanceRecord>('attendance/check-in', { customerId, notes });
  }

  // Check-in by phone number
  checkInByPhone(phone: string, notes?: string): Observable<AttendanceRecord> {
    return this.post<AttendanceRecord>('attendance/check-in-phone', { phone, notes });
  }

  // Check-out customer
  checkOut(attendanceId: number, notes?: string): Observable<AttendanceRecord> {
    return this.post<AttendanceRecord>(`attendance/${attendanceId}/check-out`, { notes });
  }

  // Get today's attendance
  getTodayAttendance(): Observable<AttendanceRecord[]> {
    return this.get<AttendanceRecord[]>('attendance/today');
  }

  // Get attendance history
  getAttendanceHistory(params?: QueryParams): Observable<AttendanceRecord[]> {
    return this.get<AttendanceRecord[]>('attendance/history', params);
  }

  // Get customer attendance history
  getCustomerAttendance(customerId: number, params?: QueryParams): Observable<AttendanceRecord[]> {
    return this.get<AttendanceRecord[]>(`attendance/customer/${customerId}`, params);
  }

  // Get attendance statistics
  getAttendanceStats(dateFrom?: Date, dateTo?: Date): Observable<AttendanceStats> {
    return this.get<AttendanceStats>('attendance/stats', { dateFrom, dateTo });
  }

  // Get currently checked-in customers
  getCurrentlyInGym(): Observable<AttendanceRecord[]> {
    return this.get<AttendanceRecord[]>('attendance/current');
  }

  // Get attendance report
  getAttendanceReport(params: QueryParams): Observable<any> {
    return this.get<any>('attendance/report', params);
  }
}
