import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { PeakHoursChartComponent } from './peak-hours-chart.component';
import { DashboardService, HourSlice } from '../../../../shared/services/dashboard.service';

describe('PeakHoursChartComponent', () => {
    let fixture: ComponentFixture<PeakHoursChartComponent>;

    function configure(stub: Partial<DashboardService>) {
        TestBed.configureTestingModule({
            declarations: [PeakHoursChartComponent],
            providers: [{ provide: DashboardService, useValue: stub }],
            schemas: [NO_ERRORS_SCHEMA],
        });
        fixture = TestBed.createComponent(PeakHoursChartComponent);
        fixture.detectChanges();
    }

    // Server always returns 24 entries, quiet hours included as zero.
    const slices: HourSlice[] = Array.from({ length: 24 }, (_, hour) => ({
        hour,
        checkIns: hour === 3 ? 0 : hour + 1,
    }));

    it('labels all 24 hours in order', () => {
        configure({ peakHours: () => of(slices) });
        expect(fixture.componentInstance.labels.length).toBe(24);
        expect(fixture.componentInstance.labels[0]).toBe('00:00');
        expect(fixture.componentInstance.labels[3]).toBe('03:00');
        expect(fixture.componentInstance.labels[23]).toBe('23:00');
    });

    it('plots a zero-valued hour rather than dropping it', () => {
        configure({ peakHours: () => of(slices) });
        const data = fixture.componentInstance.datasets[0].data;
        expect(data.length).toBe(24);
        expect(data[3]).toBe(0);
    });
});
