import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { GrowthChartComponent } from './growth-chart.component';
import { DashboardService, GrowthPoint } from '../../../../shared/services/dashboard.service';

describe('GrowthChartComponent', () => {
    let fixture: ComponentFixture<GrowthChartComponent>;

    function configure(stub: Partial<DashboardService>) {
        TestBed.configureTestingModule({
            declarations: [GrowthChartComponent],
            providers: [{ provide: DashboardService, useValue: stub }],
            schemas: [NO_ERRORS_SCHEMA],
        });
        fixture = TestBed.createComponent(GrowthChartComponent);
        fixture.detectChanges();
    }

    const points: GrowthPoint[] = [
        { year: 2026, month: 8, newMembers: 5 },
        { year: 2026, month: 9, newMembers: 9 },
    ];

    it('labels the axis by month and year', () => {
        configure({ memberGrowth: () => of(points) });
        expect(fixture.componentInstance.labels).toEqual(['08/2026', '09/2026']);
    });

    it('maps the dataset from newMembers', () => {
        configure({ memberGrowth: () => of(points) });
        const datasets = fixture.componentInstance.datasets;
        expect(datasets.length).toBe(1);
        expect(datasets[0].data).toEqual([5, 9]);
    });

    it('shows a failure message instead of rendering an empty "zero growth" chart on error', () => {
        configure({ memberGrowth: () => throwError(() => ({ status: 500 })) });

        expect(fixture.componentInstance.error).toBeTrue();
        expect(fixture.componentInstance.loading).toBeFalse();

        const el: HTMLElement = fixture.nativeElement;
        expect(el.textContent).toContain('Không thể tải dữ liệu tăng trưởng hội viên. Vui lòng thử lại sau.');
        expect(el.querySelector('canvas')).toBeNull();
    });
});
