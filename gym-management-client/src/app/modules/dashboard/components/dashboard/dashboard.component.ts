import { Component, OnInit } from '@angular/core';
import { DashboardService, Kpi } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-dashboard',
    standalone: false,
    templateUrl: './dashboard.component.html',
    styleUrls: ['./dashboard.component.scss'],
})
export class DashboardComponent implements OnInit {
    kpi: Kpi | null = null;
    loadingKpi = true;

    constructor(private dashboard: DashboardService) { }

    /** Staff receive nulls here; there is nothing to show them. */
    get showFinancials(): boolean {
        return this.kpi?.revenue !== null && this.kpi?.revenue !== undefined;
    }

    ngOnInit(): void {
        // Each widget loads on its own so the KPI row is not held up by the slower queries.
        // (Task 6 adds a dedicated expiring-soon widget that fetches its own data.)
        this.dashboard.kpi().subscribe({
            next: (kpi) => { this.kpi = kpi; this.loadingKpi = false; },
            error: () => { this.loadingKpi = false; },
        });
    }
}
