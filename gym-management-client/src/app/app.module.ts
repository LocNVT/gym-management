import { LOCALE_ID, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';
import { registerLocaleData } from '@angular/common';
import localeVi from '@angular/common/locales/vi';

// Vietnamese number/date formatting (grouping ".", decimal ",") for the whole app —
// otherwise Angular's built-in pipes fall back to en-US and "12.000.000" reads as "12,000,000".
registerLocaleData(localeVi);

// Angular Material imports (for layout shell)
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule } from '@angular/material/menu';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';

// Shared components
import { HeaderComponent } from './shared/components/header/header';
import { SidebarComponent } from './shared/components/sidebar/sidebar';
import { LoadingComponent } from './shared/components/loading/loading';

// Auth
import { AuthInterceptor } from './shared/interceptors/auth.interceptor';

@NgModule({
  declarations: [
    AppComponent,
    HeaderComponent,
    SidebarComponent,
    LoadingComponent
  ],
  imports: [
    BrowserModule,
    BrowserAnimationsModule,
    HttpClientModule,
    AppRoutingModule,

    // Angular Material (layout shell only)
    MatToolbarModule,
    MatSidenavModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatMenuModule
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
    { provide: LOCALE_ID, useValue: 'vi-VN' }
  ],
  bootstrap: [AppComponent]
})
export class AppModule { }