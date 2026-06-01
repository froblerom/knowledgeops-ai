import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { adminVisibilityGuard } from './core/guards/admin-visibility.guard';
import { dashboardVisibilityGuard } from './core/guards/dashboard-visibility.guard';
import { processingFailuresVisibilityGuard } from './core/guards/processing-failures-visibility.guard';

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
        path: 'new',
        loadComponent: () =>
          import('./features/documents/pages/document-upload-page').then(m => m.DocumentUploadPage)
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
    canActivate: [authGuard],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./features/chat/pages/chat-page').then(m => m.ChatPage)
      },
      {
        path: 'history',
        loadComponent: () =>
          import('./features/chat/pages/chat-history-page').then(m => m.ChatHistoryPage)
      },
      {
        path: 'history/:chatSessionId',
        loadComponent: () =>
          import('./features/chat/pages/chat-session-detail-page').then(m => m.ChatSessionDetailPage)
      },
      {
        path: 'interactions/:chatInteractionId',
        loadComponent: () =>
          import('./features/chat/pages/chat-interaction-detail-page').then(m => m.ChatInteractionDetailPage)
      }
    ]
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/pages/dashboard-page').then(m => m.DashboardPage),
    canActivate: [authGuard, dashboardVisibilityGuard]
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      { path: '', redirectTo: 'users', pathMatch: 'full' },
      {
        path: 'users',
        canActivate: [adminVisibilityGuard],
        loadComponent: () =>
          import('./features/admin/pages/admin-page').then(m => m.AdminPage)
      },
      {
        path: 'users/new',
        canActivate: [adminVisibilityGuard],
        loadComponent: () =>
          import('./features/admin/pages/admin-user-create-page').then(m => m.AdminUserCreatePage)
      },
      {
        path: 'users/:userId',
        canActivate: [adminVisibilityGuard],
        loadComponent: () =>
          import('./features/admin/pages/admin-user-detail-page').then(m => m.AdminUserDetailPage)
      },
      {
        path: 'processing-failures',
        canActivate: [processingFailuresVisibilityGuard],
        loadComponent: () =>
          import('./features/admin/pages/processing-failures-page').then(m => m.ProcessingFailuresPage)
      },
      {
        path: 'audit-log',
        canActivate: [adminVisibilityGuard],
        loadComponent: () =>
          import('./features/admin/pages/audit-log-page').then(m => m.AuditLogPage)
      }
    ]
  },
  {
    path: '**',
    redirectTo: 'login'
  }
];
