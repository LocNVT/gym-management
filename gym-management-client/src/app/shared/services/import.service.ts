import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface RowError {
    rowNumber: number;
    columnHeader: string | null;
    message: string;
}

export interface ImportResult {
    totalRows: number;
    importedRows: number;
    committed: boolean;
    isClean: boolean;
    errors: RowError[];
}

@Injectable({ providedIn: 'root' })
export class ImportService {
    constructor(private http: HttpClient) { }

    upload(baseUrl: string, file: File, dryRun: boolean): Observable<ImportResult> {
        const form = new FormData();
        form.append('file', file);
        return this.http.post<ImportResult>(`${baseUrl}/import`, form, {
            params: new HttpParams().set('dryRun', String(dryRun)),
        });
    }
}
