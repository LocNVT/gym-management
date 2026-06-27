import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AttendanceDevicesRoutingModule } from './attendance-devices-routing.module';
import { DeviceListComponent } from './components/device-list/device-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxButtonModule } from 'devextreme-angular/ui/button';

@NgModule({
    declarations: [DeviceListComponent],
    imports: [
        CommonModule,
        AttendanceDevicesRoutingModule,
        DxDataGridModule,
        DxButtonModule
    ]
})
export class AttendanceDevicesModule { }
