import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { FileDownloadService } from './file-download.service';

describe('FileDownloadService', () => {
    let service: FileDownloadService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [FileDownloadService, provideHttpClient(), provideHttpClientTesting()],
        });
        service = TestBed.inject(FileDownloadService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('requests the url as a blob', () => {
        service.download('/api/Member/export', 'thanh-vien.xlsx').subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/export');
        expect(req.request.responseType).toBe('blob');
        req.flush(new Blob(['x']), {
            headers: { 'content-disposition': 'attachment; filename=thanh-vien-2026-09-22.xlsx' },
        });
    });

    it('prefers the filename from content-disposition', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        service.download('/api/Member/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Member/export').flush(new Blob(['x']), {
            headers: { 'content-disposition': 'attachment; filename=thanh-vien-2026-09-22.xlsx' },
        });

        expect(used).toBe('thanh-vien-2026-09-22.xlsx');
    });

    it('falls back when the header is absent', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        service.download('/api/Member/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Member/export').flush(new Blob(['x']));

        expect(used).toBe('fallback.xlsx');
    });
});
