import { Component, OnInit } from '@angular/core';
import { ChartConfiguration, ChartDataset } from 'chart.js';
import { DashboardService } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-peak-hours-chart',
    standalone: false,
    templateUrl: './peak-hours-chart.component.html',
})
export class PeakHoursChartComponent implements OnInit {
    labels: string[] = [];
    datasets: ChartDataset<'bar'>[] = [];
    loading = true;
    /** A failed request must never render as "nobody has entered the gym in 30 days". */
    error = false;

    readonly options: ChartConfiguration<'bar'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
    };

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.peakHours(30).subscribe({
            next: (slices) => {
                this.labels = slices.map(s => `${String(s.hour).padStart(2, '0')}:00`);
                this.datasets = [
                    { label: 'Lượt check-in', data: slices.map(s => s.checkIns), backgroundColor: '#26a69a' },
                ];
                this.loading = false;
            },
            error: () => {
                this.loading = false;
                this.error = true;
            },
        });
    }
}
