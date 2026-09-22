import { Component, Input } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FileDownloadService } from '../../services/file-download.service';

@Component({
    selector: 'app-export-button',
    standalone: false,
    templateUrl: './export-button.component.html',
    styleUrls: ['./export-button.component.scss'],
})
export class ExportButtonComponent {
    @Input({ required: true }) url!: string;
    @Input({ required: true }) fallbackName!: string;
    /** Optional date range forwarded as ?from=&to= */
    @Input() params?: Record<string, string>;

    busy = false;

    constructor(private downloads: FileDownloadService, private snackBar: MatSnackBar) { }

    export(): void {
        this.busy = true;
        this.downloads.download(this.url, this.fallbackName, this.params).subscribe({
            next: () => { this.busy = false; },
            error: (err: HttpErrorResponse) => {
                this.busy = false;
                if (err.status === 403) {
                    this.showError('Bạn không có quyền xuất dữ liệu này.');
                    return;
                }
                this.resolveErrorMessage(err).then((message) => this.showError(message));
            },
        });
    }

    /**
     * `file-download.service.ts` requests with `responseType: 'blob'`, so Angular never parses
     * the error body for us — `err.error` arrives as a `Blob` even when the server sent JSON
     * (e.g. `ExcelRowLimitExceededException`'s `{ message }` payload). Read it back out by
     * hand, falling back to the generic message if it isn't JSON or has no `message`.
     */
    private async resolveErrorMessage(err: HttpErrorResponse): Promise<string> {
        const fallback = 'Xuất Excel thất bại.';
        const body: unknown = err.error;
        if (!(body instanceof Blob)) {
            return (body as { message?: string } | null)?.message ?? fallback;
        }
        try {
            const text = await body.text();
            const parsed = JSON.parse(text);
            return parsed?.message ?? fallback;
        } catch {
            return fallback;
        }
    }

    private showError(message: string): void {
        this.snackBar.open(message, 'OK', { duration: 5000 });
    }
}
