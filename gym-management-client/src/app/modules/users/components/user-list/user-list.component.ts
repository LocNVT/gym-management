import { Component, OnInit } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { UserService } from '../../../../shared/services/api.service';
import CustomStore from 'devextreme/data/custom_store';

/** 0 = Staff, 1 = Admin - not a server-driven enum (User.Role is a plain byte), so hardcoded here. */
const ROLE_OPTIONS = [
    { value: 0, text: 'Nhân viên' },
    { value: 1, text: 'Quản trị viên' },
];

@Component({
    selector: 'app-user-list',
    standalone: false,
    templateUrl: './user-list.component.html',
    styleUrls: ['./user-list.component.scss']
})
export class UserListComponent implements OnInit {
    dataSource: any;
    roleOptions = ROLE_OPTIONS;

    constructor(private userService: UserService, private snackBar: MatSnackBar) { }

    ngOnInit(): void {
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 20)) + 1 : 1;
                const pageSize = loadOptions.take || 20;
                return this.userService.getAll(page, pageSize).toPromise()
                    .then((r: any) => ({ data: r.items, totalCount: r.totalCount }));
            },
            insert: (values: any) => this.userService.create(values).toPromise() as Promise<any>,
            update: (key: string, values: any) =>
                // Role is the only editable grid column (see template) - the server has no
                // generic "update a user" endpoint, only this dedicated one.
                this.userService.setRole(key, values.role).toPromise() as Promise<any>
        });
    }

    /** Arrow function (not a method) so `this` stays bound when DevExtreme invokes it directly. */
    onToggleActiveClick = (e: any): void => this.toggleActive(e.row.data);

    toggleActive(row: any): void {
        this.userService.setActive(row.id, !row.isActive).subscribe({
            next: () => {
                this.notify(row.isActive ? 'Đã khóa tài khoản' : 'Đã mở khóa tài khoản');
                this.dataSource.reload();
            },
            error: () => this.notify('Lỗi khi cập nhật trạng thái tài khoản')
        });
    }

    private notify(message: string): void {
        this.snackBar.open(message, 'OK', { duration: 3000 });
    }
}
