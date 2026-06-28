import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatIconModule } from '@angular/material/icon';
import { AttendanceDashboardRoutingModule } from './attendance-dashboard-routing.module';
import { AttendanceDashboardComponent } from './components/attendance-dashboard/attendance-dashboard.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxButtonModule } from 'devextreme-angular/ui/button';

@NgModule({
    declarations: [AttendanceDashboardComponent],
    imports: [
        CommonModule,
        MatIconModule,
        AttendanceDashboardRoutingModule,
        DxDataGridModule,
        DxButtonModule
    ]
})
export class AttendanceDashboardModule { }
