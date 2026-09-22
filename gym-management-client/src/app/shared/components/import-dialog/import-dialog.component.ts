import { Component, Inject } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
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
        private downloads: FileDownloadService
    ) { }

    /** Enabled only once a dry run came back with zero errors. */
    get canCommit(): boolean {
        return this.stage === 'checked' && !!this.result?.isClean && this.result.totalRows > 0;
    }

    downloadTemplate(): void {
        this.downloads
            .download(`${this.data.baseUrl}/import/template`, this.data.templateName)
            .subscribe();
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
            this.stage = 'done';
            this.dialogRef.close(this.result?.importedRows ?? 0);
        });
    }

    private run(dryRun: boolean, onSuccess: () => void): void {
        if (!this.file) return;
        this.busy = true;
        this.failure = null;

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
}
