import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InvoiceItemsRoutingModule } from './invoice-items-routing.module';
import { InvoiceItemListComponent } from './components/invoice-item-list/invoice-item-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';

@NgModule({
    declarations: [InvoiceItemListComponent],
    imports: [CommonModule, InvoiceItemsRoutingModule, DxDataGridModule]
})
export class InvoiceItemsModule { }
