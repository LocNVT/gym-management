import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ExpensesRoutingModule } from './expenses-routing.module';
import { ExpenseListComponent } from './components/expense-list/expense-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { ExportButtonModule } from '../../shared/components/export-button/export-button.module';

@NgModule({
    declarations: [ExpenseListComponent],
    imports: [CommonModule, ExpensesRoutingModule, DxDataGridModule, ExportButtonModule]
})
export class ExpensesModule { }
