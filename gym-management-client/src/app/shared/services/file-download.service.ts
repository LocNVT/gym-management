import { Injectable } from '@angular/core';
import { HttpClient, HttpParams, HttpResponse } from '@angular/common/http';
import { Observable, map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FileDownloadService {
    constructor(private http: HttpClient) { }

    /** Overridable so tests can assert the filename without touching the DOM. */
    saveAs(blob: Blob, filename: string): void {
        const url = URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = filename;
        link.click();
        URL.revokeObjectURL(url);
    }

    download(url: string, fallbackName: string, params?: Record<string, string>): Observable<void> {
        return this.http
            .get(url, {
                params: new HttpParams({ fromObject: params ?? {} }),
                responseType: 'blob',
                observe: 'response',
            })
            .pipe(
                map((response: HttpResponse<Blob>) => {
                    this.saveAs(response.body!, this.filenameFrom(response) ?? fallbackName);
                })
            );
    }

    private filenameFrom(response: HttpResponse<Blob>): string | null {
        const header = response.headers.get('content-disposition');
        if (!header) return null;
        // Handles both `filename=x.xlsx` and RFC 5987 `filename*=UTF-8''x.xlsx`.
        const match = /filename\*?=(?:UTF-8'')?"?([^";]+)"?/i.exec(header);
        return match ? decodeURIComponent(match[1]) : null;
    }
}
