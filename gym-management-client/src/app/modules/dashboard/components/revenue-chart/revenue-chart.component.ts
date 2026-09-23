import { Component, OnInit } from '@angular/core';
import { ChartConfiguration, ChartDataset } from 'chart.js';
import { DashboardService } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-revenue-chart',
    standalone: false,
    templateUrl: './revenue-chart.component.html',
})
export class RevenueChartComponent implements OnInit {
    labels: string[] = [];
    datasets: ChartDataset<'line'>[] = [];
    loading = true;
    /** Staff are refused this endpoint by design; say so rather than showing a failure. */
    forbidden = false;

    readonly options: ChartConfiguration<'line'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: {
            y: {
                beginAtZero: true,
                ticks: { callback: (v) => new Intl.NumberFormat('vi-VN').format(Number(v)) },
            },
        },
    };

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.revenueTrend(12).subscribe({
            next: (points) => {
                this.labels = points.map(p => `${String(p.month).padStart(2, '0')}/${p.year}`);
                this.datasets = [
                    { label: 'Doanh thu', data: points.map(p => p.revenue), borderColor: '#2e7d32', backgroundColor: 'rgba(46,125,50,0.12)', fill: true, tension: 0.3 },
                    { label: 'Chi phí', data: points.map(p => p.expense), borderColor: '#c62828', backgroundColor: 'rgba(198,40,40,0.12)', fill: true, tension: 0.3 },
                ];
                this.loading = false;
            },
            error: (err) => {
                this.forbidden = err.status === 403;
                this.loading = false;
            },
        });
    }
}
