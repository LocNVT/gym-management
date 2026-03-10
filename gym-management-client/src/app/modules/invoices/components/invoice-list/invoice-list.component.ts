import { Component, OnInit } from '@angular/core';
import { InvoiceService, MemberService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-invoice-list',
    standalone: false,
    templateUrl: './invoice-list.component.html',
    styleUrls: ['./invoice-list.component.scss']
})
export class InvoiceListComponent implements OnInit {
    dataSource: any;
    members: any[] = [];
    statusOptions = [
        { value: 0, text: 'Chờ thanh toán' },
        { value: 1, text: 'Đã thanh toán' },
        { value: 2, text: 'Đã hủy' }
    ];
    paymentMethodOptions = [
        { value: 0, text: 'Tiền mặt' },
        { value: 1, text: 'Chuyển khoản' },
        { value: 2, text: 'Thẻ' }
    ];

    constructor(private invoiceService: InvoiceService, private memberService: MemberService) { }

    ngOnInit(): void {
        this.memberService.getAll(1, 1000).subscribe(data => this.members = data.items || []);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.invoiceService.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.invoiceService.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                this.invoiceService.getById(key).toPromise()
                    .then((e: any) => this.invoiceService.update(key, { ...e, ...values }).toPromise()) as Promise<any>,
            remove: (key: string) => this.invoiceService.delete(key).toPromise() as Promise<any>
        });
    }
}
