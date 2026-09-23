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
    /**
     * On error `kpi` stays null, and `*ngIf="kpi as k"` in the template would otherwise drop
     * the whole KPI row with no message anywhere — a failed request must be visible, not silent.
     */
    kpiError = false;

    constructor(private dashboard: DashboardService) { }

    /** Staff receive nulls here; there is nothing to show them. */
    get showFinancials(): boolean {
        return this.kpi?.revenue !== null && this.kpi?.revenue !== undefined;
    }

    /**
     * Whether the KPI row is showing the current, still-in-progress local month, so the
     * month-over-month cards know to suppress their comparison (see KpiCardComponent).
     * Computed against Vietnam's fixed UTC+7 offset rather than the browser's own timezone,
     * matching the server's GymClock.
     */
    get isCurrentMonth(): boolean {
        if (!this.kpi) return false;
        const vnNow = new Date(Date.now() + 7 * 60 * 60 * 1000);
        return this.kpi.year === vnNow.getUTCFullYear() && this.kpi.month === vnNow.getUTCMonth() + 1;
    }

    ngOnInit(): void {
        // Each widget loads on its own so the KPI row is not held up by the slower queries.
        // (Task 6 adds a dedicated expiring-soon widget that fetches its own data.)
        this.dashboard.kpi().subscribe({
            next: (kpi) => { this.kpi = kpi; this.loadingKpi = false; },
            error: () => { this.loadingKpi = false; this.kpiError = true; },
        });
    }
}
