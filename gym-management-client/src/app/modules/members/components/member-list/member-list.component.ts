import { Component, OnInit, ViewChild } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { DxDataGridComponent } from 'devextreme-angular/ui/data-grid';
import { MemberService } from '../../../../shared/services/api.service';
import { EnumService, LookupOption } from '../../../../shared/services/enum.service';
import { Member } from '../../../../shared/models';
import { CameraDialogComponent } from '../camera-dialog/camera-dialog.component';
import CustomStore from 'devextreme/data/custom_store';

@Component({
    selector: 'app-member-list',
    standalone: false,
    templateUrl: './member-list.component.html',
    styleUrls: ['./member-list.component.scss']
})
export class MemberListComponent implements OnInit {
    @ViewChild(DxDataGridComponent) grid!: DxDataGridComponent;

    dataSource: any;
    genderOptions: LookupOption[] = [];
    statusOptions: LookupOption[] = [];

    constructor(
        private memberService: MemberService,
        private enumService: EnumService,
        private dialog: MatDialog,
        private snackBar: MatSnackBar
    ) { }

    ngOnInit(): void {
        this.enumService.options('gender').subscribe(o => this.genderOptions = o);
        this.enumService.options('memberStatus').subscribe(o => this.statusOptions = o);
        this.dataSource = new CustomStore({
            key: 'id',
            load: (loadOptions: any) => {
                const page = loadOptions.skip ? (loadOptions.skip / (loadOptions.take || 10)) + 1 : 1;
                const pageSize = loadOptions.take || 10;
                return this.memberService.getAll(page, pageSize).toPromise()
                    .then((result: any) => ({
                        data: result.items,
                        totalCount: result.totalCount
                    }));
            },
            insert: (values: any) => {
                values.createdAt = new Date().toISOString();
                values.isDeleted = false;
                values.registrationDate = values.registrationDate || new Date().toISOString();
                return this.memberService.create(values).toPromise() as Promise<any>;
            },
            update: (key: string, values: any) => {
                return this.memberService.getById(key).toPromise()
                    .then((existing: any) => {
                        const updated = { ...existing, ...values, updatedAt: new Date().toISOString() };
                        return this.memberService.update(key, updated).toPromise();
                    }) as Promise<any>;
            },
            remove: (key: string) => {
                return this.memberService.delete(key).toPromise() as Promise<any>;
            }
        });
    }

    reload(): void {
        this.grid?.instance?.refresh();
    }

    openCamera(member: any): void {
        const dialogRef = this.dialog.open(CameraDialogComponent, {
            width: '640px',
            data: { memberId: member.id, memberName: member.fullName || 'Thành viên' }
        });

        dialogRef.afterClosed().subscribe((blob: Blob | null) => {
            if (blob) {
                this.memberService.uploadAvatar(member.id, blob).subscribe({
                    next: () => {
                        this.snackBar.open('Đã lưu ảnh thành công!', 'OK', { duration: 3000 });
                    },
                    error: () => {
                        this.snackBar.open('Lỗi khi lưu ảnh', 'OK', { duration: 3000 });
                    }
                });
            }
        });
    }

    onFileSelected(event: Event, member: any): void {
        const input = event.target as HTMLInputElement;
        if (input.files && input.files[0]) {
            const file = input.files[0];
            this.memberService.uploadAvatar(member.id, file).subscribe({
                next: () => {
                    this.snackBar.open('Đã tải ảnh lên thành công!', 'OK', { duration: 3000 });
                },
                error: () => {
                    this.snackBar.open('Lỗi khi tải ảnh lên', 'OK', { duration: 3000 });
                }
            });
            input.value = '';
        }
    }
}
