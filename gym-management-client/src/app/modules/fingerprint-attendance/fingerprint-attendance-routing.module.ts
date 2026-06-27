import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { FingerprintAttendanceComponent } from './components/fingerprint-attendance/fingerprint-attendance.component';

const routes: Routes = [
    { path: '', component: FingerprintAttendanceComponent }
];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class FingerprintAttendanceRoutingModule { }
