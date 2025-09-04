import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { HttpClientModule } from '@angular/common/http';

// DevExtreme imports
import {
  DxButtonModule,
  DxDataGridModule,
  DxFormModule,
  DxPopupModule,
  DxToolbarModule,
  DxDrawerModule,
  DxListModule,
  DxTreeViewModule,
  DxTileViewModule,
  DxChartModule,
  DxPieChartModule,
  DxBarGaugeModule,
  // DxRangeBarModule,
  DxCalendarModule,
  DxDateBoxModule,
  DxSelectBoxModule,
  DxTextBoxModule,
  DxNumberBoxModule,
  DxTextAreaModule,
  DxCheckBoxModule,
  DxSwitchModule,
  DxTabsModule,
  DxAccordionModule,
  DxScrollViewModule,
  DxLoadPanelModule,
  DxToastModule
} from 'devextreme-angular';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

// Shared components
import { HeaderComponent } from './shared/components/header/header.component';
import { SidebarComponent } from './shared/components/sidebar/sidebar.component';
import { LoadingComponent } from './shared/components/loading/loading.component';
import { DxoRangebarModule } from 'devextreme-angular/ui/nested';

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    SidebarComponent,
    LoadingComponent
  ],
  imports: [
    BrowserModule,
    HttpClientModule,
    AppRoutingModule,

    // DevExtreme modules
    DxButtonModule,
    DxDataGridModule,
    DxFormModule,
    DxPopupModule,
    DxToolbarModule,
    DxDrawerModule,
    DxListModule,
    DxTreeViewModule,
    DxTileViewModule,
    DxChartModule,
    DxPieChartModule,
    DxBarGaugeModule,
    DxoRangebarModule,
    DxCalendarModule,
    DxDateBoxModule,
    DxSelectBoxModule,
    DxTextBoxModule,
    DxNumberBoxModule,
    DxTextAreaModule,
    DxCheckBoxModule,
    DxSwitchModule,
    DxTabsModule,
    DxAccordionModule,
    DxScrollViewModule,
    DxLoadPanelModule,
    DxToastModule,
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
