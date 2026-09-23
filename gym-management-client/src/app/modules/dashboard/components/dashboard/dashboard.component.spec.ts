import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { MatIconModule } from '@angular/material/icon';
import { DashboardComponent } from './dashboard.component';
import { KpiCardComponent } from '../kpi-card/kpi-card.component';
import { DashboardService, Kpi } from '../../../../shared/services/dashboard.service';

describe('DashboardComponent', () => {
    let fixture: ComponentFixture<DashboardComponent>;

    const kpi: Kpi = {
        year: 2026, month: 9,
        revenue: 12_000_000, previousRevenue: 10_000_000,
        expense: 4_000_000, previousExpense: 4_000_000,
        profit: 8_000_000, unpaidTotal: 1_000_000,
        activeMembers: 120, newMembers: 14, previousNewMembers: 10,
        currentlyInside: 7, expiringIn30Days: 9,
    };

    function configure(value: Kpi) {
        const stub: Partial<DashboardService> = {
            kpi: () => of(value),
            expiringSoon: () => of([]),
        };
        TestBed.configureTestingModule({
            declarations: [DashboardComponent, KpiCardComponent],
            imports: [MatIconModule],
            providers: [{ provide: DashboardService, useValue: stub }],
        });
        fixture = TestBed.createComponent(DashboardComponent);
        fixture.detectChanges();
    }

    it('loads the KPI row on init', () => {
        configure(kpi);
        expect(fixture.componentInstance.kpi?.activeMembers).toBe(120);
    });

    it('reports the month-over-month change as a percentage', () => {
        configure(kpi);
        expect(fixture.componentInstance.changePercent(12_000_000, 10_000_000)).toBe(20);
    });

    it('treats growth from zero as no comparison rather than infinity', () => {
        configure(kpi);
        expect(fixture.componentInstance.changePercent(5_000_000, 0)).toBeNull();
    });

    it('hides the financial cards when the server blanked them', () => {
        configure({ ...kpi, revenue: null, profit: null, expense: null, unpaidTotal: null });
        expect(fixture.componentInstance.showFinancials).toBeFalse();
    });
});
