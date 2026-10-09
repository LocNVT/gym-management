import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UsersRoutingModule } from './users-routing.module';
import { UserListComponent } from './components/user-list/user-list.component';
import { DxDataGridModule } from 'devextreme-angular/ui/data-grid';
import { MatSnackBarModule } from '@angular/material/snack-bar';

@NgModule({
    declarations: [UserListComponent],
    imports: [CommonModule, UsersRoutingModule, DxDataGridModule, MatSnackBarModule]
})
export class UsersModule { }
