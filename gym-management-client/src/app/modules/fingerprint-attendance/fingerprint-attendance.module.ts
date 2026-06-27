import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FingerprintAttendanceRoutingModule } from './fingerprint-attendance-routing.module';
import { FingerprintAttendanceComponent } from './components/fingerprint-attendance/fingerprint-attendance.component';

import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxButtonModule } from 'devextreme-angular/ui/button';
import { DxSelectBoxModule } from 'devextreme-angular/ui/select-box';
import { DxTextBoxModule } from 'devextreme-angular/ui/text-box';
import { DxNumberBoxModule } from 'devextreme-angular/ui/number-box';

import { MatSnackBarModule } from '@angular/material/snack-bar';

@NgModule({
    declarations: [FingerprintAttendanceComponent],
    imports: [
        CommonModule,
        FormsModule,
        FingerprintAttendanceRoutingModule,
        DxDataGridModule,
        DxButtonModule,
        DxSelectBoxModule,
        DxTextBoxModule,
        DxNumberBoxModule,
        MatSnackBarModule
    ]
})
export class FingerprintAttendanceModule { }
