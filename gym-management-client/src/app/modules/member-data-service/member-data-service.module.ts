import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MemberDataServiceRoutingModule } from './member-data-service-routing.module';
import { MemberDataServiceListComponent } from './components/member-data-service-list/member-data-service-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [MemberDataServiceListComponent],
    imports: [CommonModule, MemberDataServiceRoutingModule, DxDataGridModule]
})
export class MemberDataServiceModule { }
