import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InvoicesRoutingModule } from './invoices-routing.module';
import { InvoiceListComponent } from './components/invoice-list/invoice-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { ExportButtonModule } from '../../shared/components/export-button/export-button.module';

@NgModule({
    declarations: [InvoiceListComponent],
    imports: [CommonModule, InvoicesRoutingModule, DxDataGridModule, ExportButtonModule]
})
export class InvoicesModule { }
