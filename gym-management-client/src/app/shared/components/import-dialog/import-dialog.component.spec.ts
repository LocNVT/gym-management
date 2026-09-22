import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { ImportDialogComponent, ImportDialogData } from './import-dialog.component';
import { ImportResult, ImportService } from '../../services/import.service';
import { FileDownloadService } from '../../services/file-download.service';

describe('ImportDialogComponent', () => {
    let component: ImportDialogComponent;
    let fixture: ComponentFixture<ImportDialogComponent>;
    let imports: jasmine.SpyObj<ImportService>;
    let downloads: jasmine.SpyObj<FileDownloadService>;
    let snackBar: jasmine.SpyObj<MatSnackBar>;
    let dialogRef: jasmine.SpyObj<MatDialogRef<ImportDialogComponent, number>>;

    const cleanResult: ImportResult = {
        totalRows: 2,
        importedRows: 0,
        committed: false,
        isClean: true,
        errors: [],
    };

    const dirtyResult: ImportResult = {
        totalRows: 2,
        importedRows: 0,
        committed: false,
        isClean: false,
        errors: [{ rowNumber: 2, columnHeader: 'Tên gói', message: 'Đã tồn tại.' }],
    };

    const dialogData: ImportDialogData = {
        baseUrl: '/api/ServicePackage',
        title: 'Nhập gói dịch vụ',
        templateName: 'mau.xlsx',
    };

    beforeEach(() => {
        imports = jasmine.createSpyObj('ImportService', ['upload']);
        downloads = jasmine.createSpyObj('FileDownloadService', ['download']);
        snackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
        dialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);

        TestBed.configureTestingModule({
            declarations: [ImportDialogComponent],
            imports: [CommonModule, MatButtonModule, MatIconModule, MatDialogModule],
            providers: [
                { provide: MAT_DIALOG_DATA, useValue: dialogData },
                { provide: MatDialogRef, useValue: dialogRef },
                { provide: ImportService, useValue: imports },
                { provide: FileDownloadService, useValue: downloads },
                { provide: MatSnackBar, useValue: snackBar },
            ],
        });

        fixture = TestBed.createComponent(ImportDialogComponent);
        component = fixture.componentInstance;
    });

    function pickFile(): void {
        const file = new File(['x'], 'members.xlsx');
        // Picking a file only ever reads event.target.files, so a bare object with that shape
        // stands in for a real file-input change event.
        component.pick({ target: { files: [file] } } as unknown as Event);
    }

    // --- item 8: commit returning committed:false keeps the dialog open and shows the errors ---

    it('keeps the dialog open and shows the server errors when commit returns committed:false', () => {
        pickFile();
        imports.upload.and.returnValue(of(dirtyResult));

        component.commit();

        expect(dialogRef.close).not.toHaveBeenCalled();
        expect(component.stage).toBe('checked');
        expect(component.result).toEqual(dirtyResult);
        expect(component.canCommit).toBeFalse();
    });

    it('closes the dialog with the imported row count only when commit returns committed:true', () => {
        pickFile();
        const committed: ImportResult = { totalRows: 2, importedRows: 2, committed: true, isClean: true, errors: [] };
        imports.upload.and.returnValue(of(committed));

        component.commit();

        expect(component.stage).toBe('done');
        expect(dialogRef.close).toHaveBeenCalledWith(2);
    });

    // --- item 8: a failed re-check disables the confirm button ---

    it('disables the confirm button after a re-check fails, even though an earlier check was clean', () => {
        pickFile();
        imports.upload.and.returnValue(of(cleanResult));
        component.check();
        expect(component.canCommit).toBeTrue();

        imports.upload.and.returnValue(throwError(() => ({ error: { message: 'Lỗi mạng.' } })));
        component.check();

        expect(component.failure).toBe('Lỗi mạng.');
        expect(component.result).toBeNull();
        expect(component.canCommit).toBeFalse();
    });

    // --- item 8: picking a new file resets to the unchecked state ---

    it('resets to the unchecked state when a new file is picked after a clean check', () => {
        pickFile();
        imports.upload.and.returnValue(of(cleanResult));
        component.check();
        expect(component.stage).toBe('checked');
        expect(component.canCommit).toBeTrue();

        pickFile();

        expect(component.stage).toBe('pick');
        expect(component.result).toBeNull();
        expect(component.canCommit).toBeFalse();
    });

    // --- item 8: the confirm button is disabled until a clean dry run ---

    it('keeps the confirm button disabled before any check has run', () => {
        expect(component.canCommit).toBeFalse();
        pickFile();
        expect(component.canCommit).toBeFalse();
    });

    it('keeps the confirm button disabled when the dry run came back with errors', () => {
        pickFile();
        imports.upload.and.returnValue(of(dirtyResult));

        component.check();

        expect(component.stage).toBe('checked');
        expect(component.canCommit).toBeFalse();
    });

    it('enables the confirm button only once a dry run comes back clean with rows', () => {
        pickFile();
        imports.upload.and.returnValue(of(cleanResult));

        component.check();

        expect(component.stage).toBe('checked');
        expect(component.canCommit).toBeTrue();
    });

    // --- item 7: downloadTemplate surfaces a failed download instead of failing silently ---

    it('shows a snackbar when the template download fails with a JSON error body', async () => {
        const body = new Blob([JSON.stringify({ message: 'Không tải được file mẫu.' })], {
            type: 'application/json',
        });
        downloads.download.and.returnValue(throwError(() => ({ error: body })));

        component.downloadTemplate();
        await body.text();
        await Promise.resolve();

        expect(snackBar.open).toHaveBeenCalledWith('Không tải được file mẫu.', 'OK', { duration: 5000 });
    });

    it('falls back to a generic message when the template download fails without a JSON body', async () => {
        const body = new Blob(['not json'], { type: 'text/plain' });
        downloads.download.and.returnValue(throwError(() => ({ error: body })));

        component.downloadTemplate();
        await body.text();
        await Promise.resolve();

        expect(snackBar.open).toHaveBeenCalledWith('Tải file mẫu thất bại.', 'OK', { duration: 5000 });
    });
});
