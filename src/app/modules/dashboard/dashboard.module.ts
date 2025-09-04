
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DxChartModule, DxPieChartModule } from 'devextreme-angular';
import { DashboardComponent } from './dashboard.component';

const routes = [
  { path: '', component: DashboardComponent }
];

@NgModule({
  declarations: [DashboardComponent],
  imports: [
    CommonModule,
    RouterModule.forChild(routes),
    DxChartModule,
    DxPieChartModule
  ]
})
export class DashboardModule { }
