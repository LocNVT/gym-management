import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { CheckInRoutingModule } from './check-in-routing.module';
import { CheckInListComponent } from './components/check-in-list/check-in-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [CheckInListComponent],
    imports: [CommonModule, CheckInRoutingModule, DxDataGridModule]
})
export class CheckInModule { }
