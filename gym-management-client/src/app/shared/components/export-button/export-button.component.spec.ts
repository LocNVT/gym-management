import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';
import { ExportButtonComponent } from './export-button.component';
import { FileDownloadService } from '../../services/file-download.service';

describe('ExportButtonComponent', () => {
    let component: ExportButtonComponent;
    let fixture: ComponentFixture<ExportButtonComponent>;
    let downloads: jasmine.SpyObj<FileDownloadService>;
    let snackBar: jasmine.SpyObj<MatSnackBar>;

    beforeEach(() => {
        downloads = jasmine.createSpyObj('FileDownloadService', ['download']);
        snackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

        TestBed.configureTestingModule({
            declarations: [ExportButtonComponent],
            imports: [MatButtonModule, MatIconModule],
            providers: [
                { provide: FileDownloadService, useValue: downloads },
                { provide: MatSnackBar, useValue: snackBar },
            ],
        });

        fixture = TestBed.createComponent(ExportButtonComponent);
        component = fixture.componentInstance;
        component.url = '/api/CheckIn/export';
        component.fallbackName = 'diem-danh.xlsx';
    });

    it('clears busy and downloads on success', () => {
        downloads.download.and.returnValue(of(undefined));

        component.export();

        expect(component.busy).toBeFalse();
    });

    it('shows the fixed message for a 403 without reading the body', () => {
        downloads.download.and.returnValue(
            throwError(() => new HttpErrorResponse({ status: 403, error: new Blob(['ignored']) }))
        );

        component.export();

        expect(component.busy).toBeFalse();
        expect(snackBar.open).toHaveBeenCalledWith('Bạn không có quyền xuất dữ liệu này.', 'OK', { duration: 5000 });
    });

    it('surfaces the server message from a JSON blob body on a 400', async () => {
        // `Blob.text()` is a real (not fake-timer-driven) async read, so this assertion
        // needs a genuine await rather than fakeAsync/tick.
        const body = new Blob([JSON.stringify({ message: 'Kết quả có 75.000 dòng, vượt giới hạn 50.000 dòng mỗi lần xuất. Vui lòng thu hẹp bộ lọc.' })], {
            type: 'application/json',
        });
        downloads.download.and.returnValue(
            throwError(() => new HttpErrorResponse({ status: 400, error: body }))
        );

        component.export();
        expect(component.busy).toBeFalse();

        await body.text();
        await Promise.resolve();

        expect(snackBar.open).toHaveBeenCalledWith(
            'Kết quả có 75.000 dòng, vượt giới hạn 50.000 dòng mỗi lần xuất. Vui lòng thu hẹp bộ lọc.',
            'OK',
            { duration: 5000 }
        );
    });

    it('falls back to the generic message when the blob body is not JSON', async () => {
        const body = new Blob(['not json'], { type: 'text/plain' });
        downloads.download.and.returnValue(
            throwError(() => new HttpErrorResponse({ status: 500, error: body }))
        );

        component.export();

        await body.text();
        await Promise.resolve();

        expect(snackBar.open).toHaveBeenCalledWith('Xuất Excel thất bại.', 'OK', { duration: 5000 });
    });
});
