import { Component, OnDestroy, OnInit } from '@angular/core';
import { Subscription, interval, startWith, switchMap } from 'rxjs';
import { AttendanceService } from '../../../../shared/services/api.service';

const REFRESH_MS = 15000;

@Component({
    selector: 'app-attendance-dashboard',
    standalone: false,
    templateUrl: './attendance-dashboard.component.html',
    styleUrls: ['./attendance-dashboard.component.scss']
})
export class AttendanceDashboardComponent implements OnInit, OnDestroy {
    summary: any = { checkInsToday: 0, checkOutsToday: 0, currentlyInside: 0 };
    active: any[] = [];
    recent: any[] = [];
    lastUpdated: Date | null = null;

    private sub = new Subscription();

    constructor(private attendanceService: AttendanceService) { }

    ngOnInit(): void {
        // Poll all three endpoints on an interval, firing immediately via startWith.
        this.sub.add(
            interval(REFRESH_MS).pipe(
                startWith(0),
                switchMap(() => this.attendanceService.getSummary())
            ).subscribe(s => { this.summary = s; this.lastUpdated = new Date(); })
        );
        this.sub.add(
            interval(REFRESH_MS).pipe(
                startWith(0),
                switchMap(() => this.attendanceService.getActive())
            ).subscribe(a => (this.active = a || []))
        );
        this.sub.add(
            interval(REFRESH_MS).pipe(
                startWith(0),
                switchMap(() => this.attendanceService.getRecent(20))
            ).subscribe(r => (this.recent = r || []))
        );
    }

    ngOnDestroy(): void {
        this.sub.unsubscribe();
    }

    refreshNow(): void {
        this.attendanceService.getSummary().subscribe(s => { this.summary = s; this.lastUpdated = new Date(); });
        this.attendanceService.getActive().subscribe(a => (this.active = a || []));
        this.attendanceService.getRecent(20).subscribe(r => (this.recent = r || []));
    }
}
