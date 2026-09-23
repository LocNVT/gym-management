import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExpiringTableComponent } from './expiring-table.component';
import { DashboardService, ExpiringSoon } from '../../../../shared/services/dashboard.service';

describe('ExpiringTableComponent', () => {
    let fixture: ComponentFixture<ExpiringTableComponent>;

    function configure(stub: Partial<DashboardService>) {
        TestBed.configureTestingModule({
            declarations: [ExpiringTableComponent],
            imports: [CommonModule],
            providers: [{ provide: DashboardService, useValue: stub }],
            schemas: [NO_ERRORS_SCHEMA],
        });
        fixture = TestBed.createComponent(ExpiringTableComponent);
        fixture.detectChanges();
    }

    const row: ExpiringSoon = {
        memberId: '1', memberName: 'Nguyen Van A', memberPhone: '0901234567',
        packageName: 'Gói 1 tháng', endDate: '2026-10-01', daysLeft: 5,
    };

    describe('urgency()', () => {
        it('is critical at the boundary (3 days) and soon just past it (4 days)', () => {
            configure({ expiringSoon: () => of([row]) });
            expect(fixture.componentInstance.urgency(3)).toBe('critical');
            expect(fixture.componentInstance.urgency(4)).toBe('soon');
        });

        it('is soon at the boundary (7 days) and later just past it (8 days)', () => {
            configure({ expiringSoon: () => of([row]) });
            expect(fixture.componentInstance.urgency(7)).toBe('soon');
            expect(fixture.componentInstance.urgency(8)).toBe('later');
        });
    });

    it('renders the good-news empty state instead of a bare table when nothing is expiring', () => {
        configure({ expiringSoon: () => of([]) });
        const el: HTMLElement = fixture.nativeElement;
        expect(el.textContent).toContain('Không có gói nào sắp hết hạn trong 30 ngày tới.');
        expect(el.querySelector('table')).toBeNull();
    });

    it('renders the phone number as a tel: link', () => {
        configure({ expiringSoon: () => of([row]) });
        const el: HTMLElement = fixture.nativeElement;
        const link = el.querySelector('a') as HTMLAnchorElement;
        expect(link).not.toBeNull();
        expect(link.getAttribute('href')).toBe('tel:0901234567');
        expect(link.textContent).toContain('0901234567');
    });
});
