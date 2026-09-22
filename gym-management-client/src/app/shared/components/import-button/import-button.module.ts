import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ImportButtonComponent } from './import-button.component';
import { ImportDialogComponent } from '../import-dialog/import-dialog.component';

/**
 * `ImportButtonComponent` and `ImportDialogComponent` are declared here (not in
 * `AppModule`) because Angular forbids declaring a component in more than one
 * NgModule, and every grid that needs the import button lives in its own
 * lazily-loaded feature module (loaded via `loadChildren`, so it never imports
 * `AppModule`). Each feature module that hosts a grid with an import endpoint
 * imports this module instead — mirrors `ExportButtonModule`.
 */
@NgModule({
    declarations: [ImportButtonComponent, ImportDialogComponent],
    imports: [CommonModule, MatButtonModule, MatIconModule, MatDialogModule, MatSnackBarModule],
    exports: [ImportButtonComponent]
})
export class ImportButtonModule { }
