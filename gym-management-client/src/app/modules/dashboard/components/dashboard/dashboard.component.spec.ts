import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { Component } from '@angular/core';
import { MatIconModule } from '@angular/material/icon';
import { DashboardComponent } from './dashboard.component';
import { KpiCardComponent } from '../kpi-card/kpi-card.component';
import { DashboardService, Kpi } from '../../../../shared/services/dashboard.service';

// Minimal stand-ins for the Task 6 widgets so this spec can compile
// DashboardComponent's template without NO_ERRORS_SCHEMA, which would also
// silence unknown-property errors on the app-kpi-card bindings this spec exists
// to exercise (e.g. a typo'd [higherIsBetter] would compile silently).
@Component({ selector: 'app-revenue-chart', template: '', standalone: false })
class StubRevenueChartComponent { }
@Component({ selector: 'app-growth-chart', template: '', standalone: false })
class StubGrowthChartComponent { }
@Component({ selector: 'app-peak-hours-chart', template: '', standalone: false })
class StubPeakHoursChartComponent { }
@Component({ selector: 'app-expiring-table', template: '', standalone: false })
class StubExpiringTableComponent { }

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
        };
        TestBed.configureTestingModule({
            declarations: [
                DashboardComponent,
                KpiCardComponent,
                StubRevenueChartComponent,
                StubGrowthChartComponent,
                StubPeakHoursChartComponent,
                StubExpiringTableComponent,
            ],
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

    it('hides the financial cards when the server blanked them', () => {
        configure({ ...kpi, revenue: null, profit: null, expense: null, unpaidTotal: null });
        expect(fixture.componentInstance.showFinancials).toBeFalse();
    });
});
