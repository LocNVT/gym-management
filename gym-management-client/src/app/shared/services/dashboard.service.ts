import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Kpi {
    year: number;
    month: number;
    revenue: number | null;
    previousRevenue: number | null;
    expense: number | null;
    previousExpense: number | null;
    profit: number | null;
    unpaidTotal: number | null;
    activeMembers: number;
    newMembers: number;
    previousNewMembers: number;
    currentlyInside: number;
    expiringIn30Days: number;
}

export interface MonthPoint { year: number; month: number; revenue: number; expense: number; }
export interface GrowthPoint { year: number; month: number; newMembers: number; }
export interface PackageSlice { packageName: string; activeSubscriptions: number; revenue: number; }
export interface HourSlice { hour: number; checkIns: number; }
export interface ExpiringSoon {
    memberId: string; memberName: string; memberPhone: string;
    packageName: string; endDate: string; daysLeft: number;
}

const BASE = '/api/Dashboard';

@Injectable({ providedIn: 'root' })
export class DashboardService {
    constructor(private http: HttpClient) { }

    kpi(year?: number, month?: number): Observable<Kpi> {
        let params = new HttpParams();
        if (year) params = params.set('year', year);
        if (month) params = params.set('month', month);
        return this.http.get<Kpi>(`${BASE}/kpi`, { params });
    }

    revenueTrend(months = 12): Observable<MonthPoint[]> {
        return this.http.get<MonthPoint[]>(`${BASE}/revenue-trend`, {
            params: new HttpParams().set('months', months),
        });
    }

    memberGrowth(months = 12): Observable<GrowthPoint[]> {
        return this.http.get<GrowthPoint[]>(`${BASE}/member-growth`, {
            params: new HttpParams().set('months', months),
        });
    }

    packageDistribution(): Observable<PackageSlice[]> {
        return this.http.get<PackageSlice[]>(`${BASE}/package-distribution`);
    }

    peakHours(days = 30): Observable<HourSlice[]> {
        return this.http.get<HourSlice[]>(`${BASE}/peak-hours`, {
            params: new HttpParams().set('days', days),
        });
    }

    expiringSoon(days = 30): Observable<ExpiringSoon[]> {
        return this.http.get<ExpiringSoon[]>(`${BASE}/expiring-soon`, {
            params: new HttpParams().set('days', days),
        });
    }
}
