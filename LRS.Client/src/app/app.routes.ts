import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    redirectTo: '/scan',
    pathMatch: 'full'
  },
  {
    path: 'scan',
    loadComponent: () => import('./components/scan/scan.component').then(m => m.ScanComponent)
  },
  {
    path: 'documents',
    loadComponent: () => import('./components/document-list/document-list.component').then(m => m.DocumentListComponent)
  }
];
