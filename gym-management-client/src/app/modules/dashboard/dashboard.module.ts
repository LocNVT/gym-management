import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { BaseChartDirective, provideCharts, withDefaultRegisterables } from 'ng2-charts';
import { DashboardRoutingModule } from './dashboard-routing.module';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { KpiCardComponent } from './components/kpi-card/kpi-card.component';
import { RevenueChartComponent } from './components/revenue-chart/revenue-chart.component';
import { GrowthChartComponent } from './components/growth-chart/growth-chart.component';
import { PeakHoursChartComponent } from './components/peak-hours-chart/peak-hours-chart.component';
import { ExpiringTableComponent } from './components/expiring-table/expiring-table.component';

/**
 * The plan's `<app-loading>` isn't reachable here: `LoadingComponent` is declared
 * in `AppModule`, which has no `exports` array, and this module is lazily loaded
 * (so it never imports `AppModule`). `MatProgressSpinnerModule` gives the same
 * result and this module already depends on Material.
 *
 * `BaseChartDirective` (ng2-charts v8) is a standalone directive, so it belongs in
 * `imports`, not `declarations`. `provideCharts(withDefaultRegisterables())` registers
 * chart.js's controllers/scales/elements — required for the directive to draw anything.
 */
@NgModule({
    declarations: [
        DashboardComponent,
        KpiCardComponent,
        RevenueChartComponent,
        GrowthChartComponent,
        PeakHoursChartComponent,
        ExpiringTableComponent,
    ],
    imports: [
        CommonModule,
        MatIconModule,
        MatCardModule,
        MatTableModule,
        MatProgressSpinnerModule,
        BaseChartDirective,
        DashboardRoutingModule,
    ],
    providers: [
        provideCharts(withDefaultRegisterables()),
    ],
})
export class DashboardModule { }
