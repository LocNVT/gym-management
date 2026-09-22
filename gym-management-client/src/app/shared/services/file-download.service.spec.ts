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

    it('prefers the RFC 5987 filename* over the plain filename when both are present', () => {
        // ASP.NET Core emits the plain ASCII `filename=` fallback *before* `filename*=UTF-8''...`
        // whenever the real name has a diacritic, so a naive leftmost match would grab the
        // wrong (ASCII) one.
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        const decoded = 'thanh-viên-2026-09-22.xlsx';
        const header = `attachment; filename="thanh-vien-2026-09-22.xlsx"; filename*=UTF-8''${encodeURIComponent(decoded)}`;

        service.download('/api/Member/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Member/export').flush(new Blob(['x']), {
            headers: { 'content-disposition': header },
        });

        expect(used).toBe(decoded);
    });

    it('decodes a percent-encoded filename*', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        const decoded = 'hoá-đơn-2026-09-22.xlsx';
        const header = `attachment; filename*=UTF-8''${encodeURIComponent(decoded)}`;

        service.download('/api/Invoice/export', 'fallback.xlsx').subscribe();
        http.expectOne('/api/Invoice/export').flush(new Blob(['x']), {
            headers: { 'content-disposition': header },
        });

        expect(used).toBe(decoded);
    });

    it('does not throw and still yields a usable name when filename= contains a bare %', () => {
        let used = '';
        service.saveAs = (_blob: Blob, name: string) => { used = name; };

        // A plain `filename=` value is not percent-encoded, so a literal `%` that is not part
        // of a valid escape must not be run through decodeURIComponent (which would throw).
        const header = 'attachment; filename="Doanh thu 100%.xlsx"';

        expect(() => {
            service.download('/api/Expense/export', 'fallback.xlsx').subscribe();
            http.expectOne('/api/Expense/export').flush(new Blob(['x']), {
                headers: { 'content-disposition': header },
            });
        }).not.toThrow();

        expect(used).toBe('Doanh thu 100%.xlsx');
    });
});
