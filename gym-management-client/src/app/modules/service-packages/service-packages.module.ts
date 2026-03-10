import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ServicePackagesRoutingModule } from './service-packages-routing.module';
import { ServicePackageListComponent } from './components/service-package-list/service-package-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [ServicePackageListComponent],
    imports: [CommonModule, ServicePackagesRoutingModule, DxDataGridModule]
})
export class ServicePackagesModule { }
