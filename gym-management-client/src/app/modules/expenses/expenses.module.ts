import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExpensesRoutingModule } from './expenses-routing.module';
import { ExpenseListComponent } from './components/expense-list/expense-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [ExpenseListComponent],
    imports: [CommonModule, ExpensesRoutingModule, DxDataGridModule]
})
export class ExpensesModule { }
