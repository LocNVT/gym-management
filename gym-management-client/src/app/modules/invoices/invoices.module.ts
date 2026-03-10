import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InvoicesRoutingModule } from './invoices-routing.module';
import { InvoiceListComponent } from './components/invoice-list/invoice-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [InvoiceListComponent],
    imports: [CommonModule, InvoicesRoutingModule, DxDataGridModule]
})
export class InvoicesModule { }
