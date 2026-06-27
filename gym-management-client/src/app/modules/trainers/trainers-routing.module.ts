import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { TrainerListComponent } from './components/trainer-list/trainer-list.component';

const routes: Routes = [{ path: '', component: TrainerListComponent }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class TrainersRoutingModule { }
