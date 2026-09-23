import { Component, OnInit } from '@angular/core';
import { DashboardService, ExpiringSoon, Kpi } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-dashboard',
    standalone: false,
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
    kpi: Kpi | null = null;
    expiring: ExpiringSoon[] = [];
    loadingKpi = true;

    constructor(private dashboard: DashboardService) { }

    /** Staff receive nulls here; there is nothing to show them. */
    get showFinancials(): boolean {
        return this.kpi?.revenue !== null && this.kpi?.revenue !== undefined;
    }

    changePercent(value: number | null, previous: number | null): number | null {
        if (value === null || previous === null || previous === 0) return null;
        return Math.round(((value - previous) / previous) * 100);
    }

    ngOnInit(): void {
        // Each widget loads on its own so the KPI row is not held up by the slower queries.
        this.dashboard.kpi().subscribe({
            next: (kpi) => { this.kpi = kpi; this.loadingKpi = false; },
            error: () => { this.loadingKpi = false; },
        });
        this.dashboard.expiringSoon(30).subscribe(rows => this.expiring = rows);
    }
}
