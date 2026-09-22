import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MembersRoutingModule } from './members-routing.module';
import { MemberListComponent } from './components/member-list/member-list.component';
import { CameraDialogComponent } from './components/camera-dialog/camera-dialog.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { DxButtonModule } from 'devextreme-angular/ui/button';

import { MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ExportButtonModule } from '../../shared/components/export-button/export-button.module';

@NgModule({
    declarations: [MemberListComponent, CameraDialogComponent],
    imports: [
        CommonModule,
        MembersRoutingModule,
        DxDataGridModule,
        DxButtonModule,
        MatDialogModule,
        MatButtonModule,
        MatIconModule,
        MatSnackBarModule,
        ExportButtonModule
    ]
})
export class MembersModule { }
