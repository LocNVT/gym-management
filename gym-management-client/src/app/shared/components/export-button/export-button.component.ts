import { Component, Input } from '@angular/core';
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
            error: (err) => {
                this.busy = false;
                const message = err.status === 403
                    ? 'Bạn không có quyền xuất dữ liệu này.'
                    : err.error?.message ?? 'Xuất Excel thất bại.';
                this.snackBar.open(message, 'OK', { duration: 5000 });
            },
        });
    }
}
