import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { ExportButtonComponent } from './export-button.component';

/**
 * `ExportButtonComponent` is declared here (not in `AppModule`) because Angular
 * forbids declaring a component in more than one NgModule, and every grid that
 * needs the button lives in its own lazily-loaded feature module (loaded via
 * `loadChildren`, so it never imports `AppModule`). Each feature module that
 * hosts a grid with an export endpoint imports this module instead.
 */
@NgModule({
    declarations: [ExportButtonComponent],
    imports: [CommonModule, MatButtonModule, MatIconModule, MatSnackBarModule],
    exports: [ExportButtonComponent]
})
export class ExportButtonModule { }
