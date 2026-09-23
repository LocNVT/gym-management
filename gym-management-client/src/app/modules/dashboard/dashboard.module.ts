import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { KpiCardComponent } from './components/kpi-card/kpi-card.component';

/**
 * The plan's `<app-loading>` isn't reachable here: `LoadingComponent` is declared
 * in `AppModule`, which has no `exports` array, and this module is lazily loaded
 * (so it never imports `AppModule`). `MatProgressSpinnerModule` gives the same
 * result and this module already depends on Material.
 */
@NgModule({
    declarations: [DashboardComponent, KpiCardComponent],
    imports: [
        CommonModule,
        MatIconModule,
        MatCardModule,
        MatTableModule,
        MatProgressSpinnerModule,
        DashboardRoutingModule,
    ]
})
export class DashboardModule { }
