import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminGuard } from './core/guards/admin.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () => import('./features/auth/pages/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'scan',
    canActivate: [authGuard],
    loadComponent: () => import('./features/scanning/pages/scan/scan.component').then(m => m.ScanComponent)
  },
  {
    path: 'documents',
    canActivate: [authGuard],
    loadComponent: () => import('./features/documents/pages/document-list/document-list.component').then(m => m.DocumentListComponent)
  },
  {
    path: 'admin/register',
    canActivate: [authGuard, adminGuard],
    loadComponent: () =>
      import('./features/auth/pages/register-admin/register-admin.component').then(m => m.RegisterAdminComponent)
  }
];
