import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { InvoiceItemListComponent } from './components/invoice-item-list/invoice-item-list.component';

const routes: Routes = [{ path: '', component: InvoiceItemListComponent }];

@NgModule({
    imports: [RouterModule.forChild(routes)],
    exports: [RouterModule]
})
export class InvoiceItemsRoutingModule { }
