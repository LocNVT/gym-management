import { Component, OnInit } from '@angular/core';
import { ServicePackageService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-service-package-list',
    standalone: false,
    templateUrl: './service-package-list.component.html',
    styleUrls: ['./service-package-list.component.scss']
})
export class ServicePackageListComponent implements OnInit {
    dataSource: any;

    constructor(private svc: ServicePackageService) { }

    ngOnInit(): void {
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
