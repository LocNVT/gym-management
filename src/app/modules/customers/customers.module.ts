import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

// DevExtreme imports
import {
  DxDataGridModule,
  DxButtonModule,
  DxFormModule,
  DxPopupModule,
  DxTextBoxModule,
  DxSelectBoxModule,
  DxDateBoxModule,
  DxToastModule,
  DxLookupModule,
  DxTabsModule
} from 'devextreme-angular';

import { CustomersComponent } from './customers.component';
import { CustomerListComponent } from './components/customer-list/customer-list.component';
import { CustomerUpdateComponent } from './components/customer-form/customer-update.component';

const routes = [
  { path: '', component: CustomersComponent }
];

@NgModule({
  declarations: [
    CustomersComponent,
    CustomerListComponent,
    CustomerUpdateComponent
  ],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    DxDataGridModule,
    DxButtonModule,
    DxFormModule,
    DxPopupModule,
    DxTextBoxModule,
    DxSelectBoxModule,
    DxDateBoxModule,
    DxToastModule,
    DxLookupModule,
    DxTabsModule,
  ]
})
export class CustomersModule { }
