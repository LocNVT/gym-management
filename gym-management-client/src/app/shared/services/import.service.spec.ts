import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ImportService } from './import.service';

describe('ImportService', () => {
    let service: ImportService;
    let http: HttpTestingController;

    beforeEach(() => {
        TestBed.configureTestingModule({
            providers: [ImportService, provideHttpClient(), provideHttpClientTesting()],
        });
        service = TestBed.inject(ImportService);
        http = TestBed.inject(HttpTestingController);
    });

    afterEach(() => http.verify());

    it('posts the file as multipart with the dryRun flag', () => {
        const file = new File(['x'], 'members.xlsx');

        service.upload('/api/Member', file, true).subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/import');
        expect(req.request.params.get('dryRun')).toBe('true');
        expect(req.request.body instanceof FormData).toBeTrue();
        expect((req.request.body as FormData).get('file')).toBe(file);
        req.flush({ totalRows: 1, importedRows: 0, committed: false, isClean: true, errors: [] });
    });

    it('sends dryRun=false on commit', () => {
        service.upload('/api/Member', new File(['x'], 'm.xlsx'), false).subscribe();

        const req = http.expectOne(r => r.url === '/api/Member/import');
        expect(req.request.params.get('dryRun')).toBe('false');
        req.flush({ totalRows: 1, importedRows: 1, committed: true, isClean: true, errors: [] });
    });
});
