import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./modules/auth/auth.module').then(m => m.AuthModule)
  },
  { path: '', redirectTo: '/members', pathMatch: 'full' },
  {
    path: 'members',
    loadChildren: () => import('./modules/members/members.module').then(m => m.MembersModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'check-in',
    loadChildren: () => import('./modules/check-in/check-in.module').then(m => m.CheckInModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'service-packages',
    loadChildren: () => import('./modules/service-packages/service-packages.module').then(m => m.ServicePackagesModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'member-data-service',
    loadChildren: () => import('./modules/member-data-service/member-data-service.module').then(m => m.MemberDataServiceModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'invoices',
    loadChildren: () => import('./modules/invoices/invoices.module').then(m => m.InvoicesModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'invoice-items',
    loadChildren: () => import('./modules/invoice-items/invoice-items.module').then(m => m.InvoiceItemsModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'expenses',
    loadChildren: () => import('./modules/expenses/expenses.module').then(m => m.ExpensesModule),
    canActivate: [AuthGuard]
  },
  { path: '**', redirectTo: '/members' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }