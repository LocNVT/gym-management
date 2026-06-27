import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AttendanceDashboardComponent } from './components/attendance-dashboard/attendance-dashboard.component';

const routes: Routes = [
    { path: '', component: AttendanceDashboardComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class AttendanceDashboardRoutingModule { }
