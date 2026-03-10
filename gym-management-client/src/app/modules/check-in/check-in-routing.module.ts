import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { CheckInListComponent } from './components/check-in-list/check-in-list.component';

const routes: Routes = [{ path: '', component: CheckInListComponent }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class CheckInRoutingModule { }
