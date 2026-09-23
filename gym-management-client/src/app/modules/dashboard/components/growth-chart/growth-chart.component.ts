import { Component, OnInit } from '@angular/core';
import { ChartConfiguration, ChartDataset } from 'chart.js';
import { DashboardService } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-growth-chart',
    standalone: false,
    templateUrl: './growth-chart.component.html',
})
export class GrowthChartComponent implements OnInit {
    labels: string[] = [];
    datasets: ChartDataset<'bar'>[] = [];
    loading = true;

    readonly options: ChartConfiguration<'bar'>['options'] = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { position: 'bottom' } },
        scales: { y: { beginAtZero: true, ticks: { precision: 0 } } },
    };

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.memberGrowth(12).subscribe({
            next: (points) => {
                this.labels = points.map(p => `${String(p.month).padStart(2, '0')}/${p.year}`);
                this.datasets = [
                    { label: 'Hội viên mới', data: points.map(p => p.newMembers), backgroundColor: '#5c6bc0' },
                ];
                this.loading = false;
            },
            error: () => {
                this.loading = false;
            },
        });
    }
}
