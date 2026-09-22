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
        // Some browser/OS combinations only honour the download if the anchor is
        // actually in the DOM when clicked, and if the object URL is still live
        // when the download is scheduled — so revoke it on the next tick, not in
        // the same synchronous breath as click().
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        setTimeout(() => URL.revokeObjectURL(url), 0);
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

        // ASP.NET Core emits `filename="..."` (a plain ASCII fallback) *before*
        // `filename*=UTF-8''...` (RFC 5987) whenever the real name has a diacritic, so the
        // RFC 5987 value must be tried first and preferred — a single unanchored regex would
        // otherwise always grab the leftmost (plain) match.
        const extended = /filename\*\s*=\s*UTF-8''([^;]+)/i.exec(header);
        if (extended) {
            const value = extended[1].trim().replace(/^"|"$/g, '');
            try {
                // Only the RFC 5987 value is percent-encoded, so only it is decoded.
                return decodeURIComponent(value);
            } catch {
                // A malformed escape here degrades to the caller's fallback name rather
                // than throwing inside the pipe and failing an otherwise-successful download.
                return null;
            }
        }

        const plain = /filename\s*=\s*"?([^";]+)"?/i.exec(header);
        // Plain `filename=` values are not percent-encoded — used as-is, never decoded.
        return plain ? plain[1].trim() : null;
    }
}
