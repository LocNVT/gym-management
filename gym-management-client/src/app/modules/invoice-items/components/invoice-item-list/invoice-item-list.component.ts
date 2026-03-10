import { Component, OnInit } from '@angular/core';
import { InvoiceItemService, InvoiceService, ServicePackageService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-invoice-item-list',
    standalone: false,
    templateUrl: './invoice-item-list.component.html',
    styleUrls: ['./invoice-item-list.component.scss']
})
export class InvoiceItemListComponent implements OnInit {
    dataSource: any;
    invoices: any[] = [];
    servicePackages: any[] = [];

    constructor(
        private svc: InvoiceItemService,
        private invoiceService: InvoiceService,
        private spService: ServicePackageService
    ) { }

    ngOnInit(): void {
        this.invoiceService.getAll(1, 1000).subscribe(data => this.invoices = data.items || []);
        this.spService.getAll(1, 1000).subscribe(data => this.servicePackages = data.items || []);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.svc.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.svc.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                this.svc.getById(key).toPromise()
                    .then((e: any) => this.svc.update(key, { ...e, ...values }).toPromise()) as Promise<any>,
            remove: (key: string) => this.svc.delete(key).toPromise() as Promise<any>
        });
    }
}
