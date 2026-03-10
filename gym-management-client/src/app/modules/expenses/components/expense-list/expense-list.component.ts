import { Component, OnInit } from '@angular/core';
import { ExpenseService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-expense-list',
    standalone: false,
    templateUrl: './expense-list.component.html',
    styleUrls: ['./expense-list.component.scss']
})
export class ExpenseListComponent implements OnInit {
    dataSource: any;
    categoryOptions = [
        { value: 'Tiền thuê', text: 'Tiền thuê' },
        { value: 'Điện nước', text: 'Điện nước' },
        { value: 'Thiết bị', text: 'Thiết bị' },
        { value: 'Lương', text: 'Lương' },
        { value: 'Bảo trì', text: 'Bảo trì' },
        { value: 'Khác', text: 'Khác' }
    ];
    paymentMethodOptions = [
        { value: 0, text: 'Tiền mặt' },
        { value: 1, text: 'Chuyển khoản' },
        { value: 2, text: 'Thẻ' }
    ];

    constructor(private expenseService: ExpenseService) { }

    ngOnInit(): void {
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
