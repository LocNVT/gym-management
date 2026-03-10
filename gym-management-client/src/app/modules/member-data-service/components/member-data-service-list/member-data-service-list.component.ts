import { Component, OnInit } from '@angular/core';
import { MemberDataServiceService, MemberService, ServicePackageService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-member-data-service-list',
    standalone: false,
    templateUrl: './member-data-service-list.component.html',
    styleUrls: ['./member-data-service-list.component.scss']
})
export class MemberDataServiceListComponent implements OnInit {
    dataSource: any;
    members: any[] = [];
    servicePackages: any[] = [];
    statusOptions = [
        { value: 0, text: 'Hoạt động' },
        { value: 1, text: 'Hết hạn' },
        { value: 2, text: 'Đã hủy' }
    ];

    constructor(
        private svc: MemberDataServiceService,
        private memberService: MemberService,
        private spService: ServicePackageService
    ) { }

    ngOnInit(): void {
        this.memberService.getAll(1, 1000).subscribe(data => this.members = data.items || []);
        this.spService.getAll(1, 1000).subscribe(data => this.servicePackages = data.items || []);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.svc.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => {
                values.createdAt = new Date().toISOString();
                return this.svc.create(values).toPromise() as Promise<any>;
            },
            update: (key: string, values: any) =>
                this.svc.getById(key).toPromise()
                    .then((e: any) => this.svc.update(key, { ...e, ...values, updatedAt: new Date().toISOString() }).toPromise()) as Promise<any>,
            remove: (key: string) => this.svc.delete(key).toPromise() as Promise<any>
        });
    }
}
