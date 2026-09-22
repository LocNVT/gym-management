import { Component, OnInit } from '@angular/core';
import { TrainerService } from '../../../../shared/services/api.service';
import { EnumService, LookupOption } from '../../../../shared/services/enum.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-trainer-list',
    standalone: false,
    templateUrl: './trainer-list.component.html',
    styleUrls: ['./trainer-list.component.scss']
})
export class TrainerListComponent implements OnInit {
    dataSource: any;
    statusOptions: LookupOption[] = [];

    constructor(private trainerService: TrainerService, private enumService: EnumService) { }

    ngOnInit(): void {
        this.enumService.options('trainerStatus').subscribe(o => this.statusOptions = o);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.trainerService.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.trainerService.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                this.trainerService.getById(key).toPromise()
                    .then((e: any) => this.trainerService.update(key, { ...e, ...values }).toPromise()) as Promise<any>,
            remove: (key: string) => this.trainerService.delete(key).toPromise() as Promise<any>
        });
    }
}
