import { Component, OnInit } from '@angular/core';
import { AttendanceDeviceService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-device-list',
    standalone: false,
    templateUrl: './device-list.component.html',
    styleUrls: ['./device-list.component.scss']
})
export class DeviceListComponent implements OnInit {
    dataSource: any;

    vendorOptions = [
        { value: 'Mock', text: 'Mock (thử nghiệm)' },
        { value: 'ZKTeco', text: 'ZKTeco' },
        { value: 'Suprema', text: 'Suprema' },
        { value: 'DigitalPersona', text: 'DigitalPersona' }
    ];

    constructor(private deviceService: AttendanceDeviceService) { }

    ngOnInit(): void {
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.deviceService.getAll(page, pageSize).toPromise()
                    .then((result: any) => ({ data: result.items, totalCount: result.totalCount }));
            },
            insert: (values: any) => {
                values.isActive = values.isActive ?? true;
                values.vendor = values.vendor || 'Mock';
                return this.deviceService.create(values).toPromise() as Promise<any>;
            },
            update: (key: string, values: any) => {
                return this.deviceService.getById(key).toPromise()
                    .then((existing: any) => {
                        const updated = { ...existing, ...values };
                        return this.deviceService.update(key, updated).toPromise();
                    }) as Promise<any>;
            },
            remove: (key: string) => {
                return this.deviceService.delete(key).toPromise() as Promise<any>;
            }
        });
    }
}
