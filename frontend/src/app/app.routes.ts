import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminVisibilityGuard } from './core/guards/admin-visibility.guard';

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
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/documents/pages/documents-page').then(m => m.DocumentsPage)
      },
      {
        path: ':documentId',
        loadComponent: () =>
          import('./features/documents/pages/document-detail-page').then(m => m.DocumentDetailPage)
      }
    ]
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
    canActivate: [authGuard, adminVisibilityGuard],
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      {
        path: 'users',
        loadComponent: () =>
          import('./features/admin/pages/admin-page').then(m => m.AdminPage)
      },
      {
        path: 'users/new',
        loadComponent: () =>
          import('./features/admin/pages/admin-user-create-page').then(m => m.AdminUserCreatePage)
      },
      {
        path: 'users/:userId',
        loadComponent: () =>
          import('./features/admin/pages/admin-user-detail-page').then(m => m.AdminUserDetailPage)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
