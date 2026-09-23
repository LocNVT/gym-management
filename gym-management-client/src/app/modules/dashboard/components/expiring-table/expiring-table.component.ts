import { Component, OnInit } from '@angular/core';
import { DashboardService, ExpiringSoon } from '../../../../shared/services/dashboard.service';

@Component({
    selector: 'app-expiring-table',
    standalone: false,
    templateUrl: './expiring-table.component.html',
    styleUrls: ['./expiring-table.component.scss'],
})
export class ExpiringTableComponent implements OnInit {
    rows: ExpiringSoon[] = [];
    loading = true;
    /**
     * A failed request must never be mistaken for the good-news empty state: `rows` staying
     * empty on error would otherwise print "Không có gói nào sắp hết hạn trong 30 ngày tới."
     * — telling staff there is nobody to call when the truth is the data never arrived.
     */
    error = false;

    constructor(private dashboard: DashboardService) { }

    ngOnInit(): void {
        this.dashboard.expiringSoon(30).subscribe({
            next: (rows) => { this.rows = rows; this.loading = false; },
            error: () => { this.loading = false; this.error = true; },
        });
    }

    urgency(daysLeft: number): 'critical' | 'soon' | 'later' {
        if (daysLeft <= 3) return 'critical';
        if (daysLeft <= 7) return 'soon';
        return 'later';
    }
}
