import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ServicePackageListComponent } from './components/service-package-list/service-package-list.component';

const routes: Routes = [{ path: '', component: ServicePackageListComponent }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class ServicePackagesRoutingModule { }
