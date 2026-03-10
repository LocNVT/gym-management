import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_BASE = '/api';

export interface PaginatedResult<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

@Injectable({ providedIn: 'root' })
export class MemberService {
    private url = `${API_BASE}/Member`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
    uploadAvatar(id: string, file: File | Blob): Observable<any> {
        const formData = new FormData();
        formData.append('file', file);
        return this.http.post<any>(`${this.url}/${id}/avatar`, formData);
    }
    getAvatarUrl(id: string): string { return `${this.url}/${id}/avatar`; }
}

@Injectable({ providedIn: 'root' })
export class CheckInService {
    private url = `${API_BASE}/CheckIn`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class ServicePackageService {
    private url = `${API_BASE}/ServicePackage`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getAllNoPaging(): Observable<any[]> { return this.http.get<any[]>(this.url); }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class MemberDataServiceService {
    private url = `${API_BASE}/MemberDataService`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class InvoiceService {
    private url = `${API_BASE}/Invoice`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class InvoiceItemService {
    private url = `${API_BASE}/InvoiceItem`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}

@Injectable({ providedIn: 'root' })
export class ExpenseService {
    private url = `${API_BASE}/Expense`;
    constructor(private http: HttpClient) { }
    getAll(page = 1, pageSize = 10): Observable<PaginatedResult<any>> {
        const params = new HttpParams().set('pageNumber', page).set('pageSize', pageSize);
        return this.http.get<PaginatedResult<any>>(this.url, { params });
    }
    getById(id: string): Observable<any> { return this.http.get<any>(`${this.url}/${id}`); }
    create(data: any): Observable<any> { return this.http.post<any>(this.url, data); }
    update(id: string, data: any): Observable<any> { return this.http.put<any>(`${this.url}/${id}`, data); }
    delete(id: string): Observable<any> { return this.http.delete<any>(`${this.url}/${id}`); }
}
