import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'login',
    pathMatch: 'full'
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/pages/login-page').then(m => m.LoginPage)
  },
  {
    path: 'documents',
    loadComponent: () =>
      import('./features/documents/pages/documents-page').then(m => m.DocumentsPage),
    canActivate: [authGuard]
  },
  {
    path: 'chat',
    loadComponent: () =>
      import('./features/chat/pages/chat-page').then(m => m.ChatPage),
    canActivate: [authGuard]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/pages/dashboard-page').then(m => m.DashboardPage),
    canActivate: [authGuard]
  },
  {
    path: 'admin',
    loadComponent: () =>
      import('./features/admin/pages/admin-page').then(m => m.AdminPage),
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
