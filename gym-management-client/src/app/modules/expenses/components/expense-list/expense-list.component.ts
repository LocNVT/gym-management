import { Component, OnInit } from '@angular/core';
import { ExpenseService } from '../../../../shared/services/api.service';
import { EnumService, LookupOption } from '../../../../shared/services/enum.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-expense-list',
    standalone: false,
    templateUrl: './expense-list.component.html',
    styleUrls: ['./expense-list.component.scss']
})
export class ExpenseListComponent implements OnInit {
    dataSource: any;
    // Free text, not an enum — categories aren't a fixed backend-defined set.
    categoryOptions = [
        { value: 'Tiền thuê', text: 'Tiền thuê' },
        { value: 'Điện nước', text: 'Điện nước' },
        { value: 'Thiết bị', text: 'Thiết bị' },
        { value: 'Lương', text: 'Lương' },
        { value: 'Bảo trì', text: 'Bảo trì' },
        { value: 'Khác', text: 'Khác' }
    ];
    paymentMethodOptions: LookupOption[] = [];

    constructor(private expenseService: ExpenseService, private enumService: EnumService) { }

    ngOnInit(): void {
        this.enumService.options('paymentMethod').subscribe(o => this.paymentMethodOptions = o);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.expenseService.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.expenseService.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                this.expenseService.getById(key).toPromise()
                    .then((e: any) => this.expenseService.update(key, { ...e, ...values }).toPromise()) as Promise<any>,
            remove: (key: string) => this.expenseService.delete(key).toPromise() as Promise<any>
        });
    }
}
