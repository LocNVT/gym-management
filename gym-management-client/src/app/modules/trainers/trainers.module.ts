import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TrainersRoutingModule } from './trainers-routing.module';
import { TrainerListComponent } from './components/trainer-list/trainer-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [TrainerListComponent],
    imports: [CommonModule, TrainersRoutingModule, DxDataGridModule]
})
export class TrainersModule { }
