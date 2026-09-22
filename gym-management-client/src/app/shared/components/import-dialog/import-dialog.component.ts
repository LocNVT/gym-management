import { Component, Inject } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImportResult, ImportService } from '../../services/import.service';
import { FileDownloadService } from '../../services/file-download.service';

export interface ImportDialogData {
    baseUrl: string;
    title: string;
    templateName: string;
}

type Stage = 'pick' | 'checked' | 'done';

@Component({
    selector: 'app-import-dialog',
    standalone: false,
    templateUrl: './import-dialog.component.html',
    styleUrls: ['./import-dialog.component.scss'],
})
export class ImportDialogComponent {
    stage: Stage = 'pick';
    file: File | null = null;
    result: ImportResult | null = null;
    busy = false;
    failure: string | null = null;

    constructor(
        @Inject(MAT_DIALOG_DATA) public data: ImportDialogData,
        private dialogRef: MatDialogRef<ImportDialogComponent, number>,
        private imports: ImportService,
        private downloads: FileDownloadService,
        private snackBar: MatSnackBar
    ) { }

    /** Enabled only once a dry run came back with zero errors. */
    get canCommit(): boolean {
        return this.stage === 'checked' && !!this.result?.isClean && this.result.totalRows > 0;
    }

    downloadTemplate(): void {
        this.downloads
            .download(`${this.data.baseUrl}/import/template`, this.data.templateName)
            .subscribe({
                error: (err: HttpErrorResponse) =>
                    this.resolveErrorMessage(err).then((message) => this.showError(message)),
            });
    }

    pick(event: Event): void {
        // Picking a new file resets the checked state — otherwise a user could
        // check file A and then commit file B without the button re-disabling.
        this.file = (event.target as HTMLInputElement).files?.[0] ?? null;
        this.stage = 'pick';
        this.result = null;
        this.failure = null;
    }

    check(): void {
        this.run(true, () => { this.stage = 'checked'; });
    }

    commit(): void {
        // The same File object is re-sent: the server keeps no state between the
        // dry run and the commit, so the browser must resubmit the whole file.
        this.run(false, () => {
            // A 200 response does not by itself mean the rows were written: the server's own
            // duplicate check at commit time can find something the dry run did not (a race
            // with another import), or the commit can fail and be reported as a row error
            // instead of an HTTP error. Only committed === true is success — anything else
            // must stay open and show why, exactly like a dry run with errors does, rather
            // than closing silently with nothing imported.
            if (this.result?.committed) {
                this.stage = 'done';
                this.dialogRef.close(this.result.importedRows);
                return;
            }
            this.stage = 'checked';
        });
    }

    private run(dryRun: boolean, onSuccess: () => void): void {
        if (!this.file) return;
        this.busy = true;
        this.failure = null;
        // Clear the previous result as soon as a new run starts, not only when a new file is
        // picked — otherwise a failed re-check (an HTTP error, not a row error) leaves the
        // prior clean result on screen with the confirm button still enabled, even though
        // nothing about the file has been re-verified.
        this.result = null;

        this.imports.upload(this.data.baseUrl, this.file, dryRun).subscribe({
            next: (result) => {
                this.result = result;
                this.busy = false;
                onSuccess();
            },
            error: (err) => {
                this.busy = false;
                this.failure = err.error?.message ?? 'Nhập dữ liệu thất bại.';
            },
        });
    }

    /**
     * Mirrors ExportButtonComponent.resolveErrorMessage: `file-download.service.ts` requests
     * with `responseType: 'blob'`, so Angular never parses the error body for us — `err.error`
     * arrives as a `Blob` even when the server sent JSON. Read it back out by hand, falling
     * back to the generic message if it isn't JSON or has no `message`.
     */
    private async resolveErrorMessage(err: HttpErrorResponse): Promise<string> {
        const fallback = 'Tải file mẫu thất bại.';
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
