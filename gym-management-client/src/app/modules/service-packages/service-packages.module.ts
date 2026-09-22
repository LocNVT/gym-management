import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ServicePackagesRoutingModule } from './service-packages-routing.module';
import { ServicePackageListComponent } from './components/service-package-list/service-package-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { ExportButtonModule } from '../../shared/components/export-button/export-button.module';
import { ImportButtonModule } from '../../shared/components/import-button/import-button.module';

@NgModule({
    declarations: [ServicePackageListComponent],
    imports: [
        CommonModule,
        ServicePackagesRoutingModule,
        DxDataGridModule,
        ExportButtonModule,
        ImportButtonModule
    ]
})
export class ServicePackagesModule { }
