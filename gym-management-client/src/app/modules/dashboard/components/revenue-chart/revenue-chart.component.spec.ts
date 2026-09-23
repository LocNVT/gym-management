import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { RevenueChartComponent } from './revenue-chart.component';
import { DashboardService, MonthPoint } from '../../../../shared/services/dashboard.service';

describe('RevenueChartComponent', () => {
    let fixture: ComponentFixture<RevenueChartComponent>;

    function configure(stub: Partial<DashboardService>) {
        TestBed.configureTestingModule({
            declarations: [RevenueChartComponent],
            providers: [{ provide: DashboardService, useValue: stub }],
            schemas: [NO_ERRORS_SCHEMA],
        });
        fixture = TestBed.createComponent(RevenueChartComponent);
        fixture.detectChanges();
    }

    const points: MonthPoint[] = [
        { year: 2026, month: 8, revenue: 10_000_000, expense: 4_000_000 },
        { year: 2026, month: 9, revenue: 12_000_000, expense: 5_000_000 },
    ];

    it('labels the axis by month and year', () => {
        configure({ revenueTrend: () => of(points) });
        expect(fixture.componentInstance.labels).toEqual(['08/2026', '09/2026']);
    });

    it('plots revenue and expense as two series', () => {
        configure({ revenueTrend: () => of(points) });
        const datasets = fixture.componentInstance.datasets;

        expect(datasets.length).toBe(2);
        expect(datasets[0].data).toEqual([10_000_000, 12_000_000]);
        expect(datasets[1].data).toEqual([4_000_000, 5_000_000]);
    });

    it('shows a permission message instead of an error when staff open it', () => {
        configure({ revenueTrend: () => throwError(() => ({ status: 403 })) });
        expect(fixture.componentInstance.forbidden).toBeTrue();
        expect(fixture.componentInstance.loading).toBeFalse();
    });

    it('shows a failure message (not the staff-only message) on a genuine server error, and hides the chart', () => {
        configure({ revenueTrend: () => throwError(() => ({ status: 500 })) });

        expect(fixture.componentInstance.error).toBeTrue();
        expect(fixture.componentInstance.forbidden).toBeFalse();
        expect(fixture.componentInstance.loading).toBeFalse();

        const el: HTMLElement = fixture.nativeElement;
        expect(el.textContent).toContain('Không thể tải dữ liệu doanh thu và chi phí. Vui lòng thử lại sau.');
        expect(el.textContent).not.toContain('Chỉ quản trị viên xem được số liệu tài chính.');
        expect(el.querySelector('canvas')).toBeNull();
    });
});
