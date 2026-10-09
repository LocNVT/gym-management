import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { AuthGuard } from './shared/guards/auth.guard';
import { RoleGuard } from './shared/guards/role.guard';

const routes: Routes = [
  {
    path: 'login',
    loadChildren: () => import('./modules/auth/auth.module').then(m => m.AuthModule)
  },
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadChildren: () => import('./modules/dashboard/dashboard.module').then(m => m.DashboardModule),
    canActivate: [AuthGuard]
  },
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
  {
    path: 'trainers',
    loadChildren: () => import('./modules/trainers/trainers.module').then(m => m.TrainersModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'attendance-dashboard',
    loadChildren: () => import('./modules/attendance-dashboard/attendance-dashboard.module').then(m => m.AttendanceDashboardModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'fingerprint-attendance',
    loadChildren: () => import('./modules/fingerprint-attendance/fingerprint-attendance.module').then(m => m.FingerprintAttendanceModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'attendance-devices',
    loadChildren: () => import('./modules/attendance-devices/attendance-devices.module').then(m => m.AttendanceDevicesModule),
    canActivate: [AuthGuard]
  },
  {
    path: 'users',
    loadChildren: () => import('./modules/users/users.module').then(m => m.UsersModule),
    canActivate: [AuthGuard, RoleGuard]
  },
  { path: '**', redirectTo: '/dashboard' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }