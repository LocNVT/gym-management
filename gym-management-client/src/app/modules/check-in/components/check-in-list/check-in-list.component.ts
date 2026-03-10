import { Component, OnInit } from '@angular/core';
import { CheckInService, MemberService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-check-in-list',
    standalone: false,
    templateUrl: './check-in-list.component.html',
    styleUrls: ['./check-in-list.component.scss']
})
export class CheckInListComponent implements OnInit {
    dataSource: any;
    members: any[] = [];
    methodOptions = [
        { value: 0, text: 'Thẻ từ' },
        { value: 1, text: 'QR Code' },
        { value: 2, text: 'Vân tay' },
        { value: 3, text: 'Thủ công' }
    ];

    constructor(
        private checkInService: CheckInService,
        private memberService: MemberService
    ) { }

    ngOnInit(): void {
        this.memberService.getAll(1, 1000).subscribe(data => this.members = data.items || []);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.checkInService.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.checkInService.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                this.checkInService.getById(key).toPromise()
                    .then((e: any) => this.checkInService.update(key, { ...e, ...values }).toPromise()) as Promise<any>,
            remove: (key: string) => this.checkInService.delete(key).toPromise() as Promise<any>
        });
    }
}
