import { Component, EventEmitter, Input, Output } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ImportDialogComponent, ImportDialogData } from '../import-dialog/import-dialog.component';

@Component({
    selector: 'app-import-button',
    standalone: false,
    templateUrl: './import-button.component.html',
})
export class ImportButtonComponent {
    @Input({ required: true }) baseUrl!: string;
    @Input({ required: true }) title!: string;
    @Input({ required: true }) templateName!: string;
    @Output() imported = new EventEmitter<number>();

    constructor(private dialog: MatDialog, private snackBar: MatSnackBar) { }

    open(): void {
        const data: ImportDialogData = {
            baseUrl: this.baseUrl,
            title: this.title,
            templateName: this.templateName,
        };

        this.dialog
            .open(ImportDialogComponent, { width: '760px', data })
            .afterClosed()
            .subscribe((importedRows?: number) => {
                if (!importedRows) return;
                this.snackBar.open(`Đã nhập ${importedRows} dòng.`, 'OK', { duration: 4000 });
                this.imported.emit(importedRows);
            });
    }
}
