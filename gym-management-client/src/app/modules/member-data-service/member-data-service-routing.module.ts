import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { MemberDataServiceListComponent } from './components/member-data-service-list/member-data-service-list.component';

const routes: Routes = [{ path: '', component: MemberDataServiceListComponent }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class MemberDataServiceRoutingModule { }
