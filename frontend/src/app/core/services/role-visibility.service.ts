import { Injectable, inject } from '@angular/core';
import { AuthSessionService } from './auth-session.service';

// UX visibility only — backend enforces authorization independently.
// These helpers hide or show UI elements based on roles from the authenticated session.
// They are NOT an authorization boundary. The backend must be called and will enforce
// permission + organization scope for every protected action.
// See docs/16-security-and-permissions.md Section 8.1.
@Injectable({ providedIn: 'root' })
export class RoleVisibilityService {
  private readonly session = inject(AuthSessionService);

  private getRoles(): string[] {
    return this.session.getSession()?.roles ?? [];
  }

  private hasRole(role: string): boolean {
    return this.getRoles().includes(role);
  }

  private hasAnyRole(...roles: string[]): boolean {
    const sessionRoles = this.getRoles();
    return roles.some(role => sessionRoles.includes(role));
  }

  // Chat.AskQuestion: all authenticated MVP roles.
  canAskChat(): boolean {
    return this.session.isAuthenticated()
      && this.hasAnyRole('Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin');
  }

  // Feedback.Submit / Feedback.UpdateOwn: all authenticated MVP roles.
  canSubmitFeedback(): boolean {
    return this.session.isAuthenticated()
      && this.hasAnyRole('Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin');
  }

  // Documents.View: KnowledgeAdmin, Manager, Admin.
  canViewDocuments(): boolean {
    return this.hasAnyRole('KnowledgeAdmin', 'Manager', 'Admin');
  }

  // Documents.Upload: KnowledgeAdmin, Admin.
  canUploadDocuments(): boolean {
    return this.hasAnyRole('KnowledgeAdmin', 'Admin');
  }

  // Documents.Disable: KnowledgeAdmin, Admin.
  canDisableDocumentRetrieval(): boolean {
    return this.hasAnyRole('KnowledgeAdmin', 'Admin');
  }

  // Dashboard.ViewOverview: KnowledgeAdmin, Manager, Admin.
  canViewDashboard(): boolean {
    return this.hasAnyRole('KnowledgeAdmin', 'Manager', 'Admin');
  }

  // Feedback.ViewReviewData: Supervisor, Manager, Admin.
  canViewFeedbackReview(): boolean {
    return this.hasAnyRole('Supervisor', 'Manager', 'Admin');
  }

  // Users.View: Admin only.
  canViewAdmin(): boolean {
    return this.hasRole('Admin');
  }

  // System.ViewProcessingFailures: KnowledgeAdmin, Admin.
  canViewProcessingFailures(): boolean {
    return this.hasAnyRole('KnowledgeAdmin', 'Admin');
  }

  // Audit.View: Admin only.
  canViewAuditLog(): boolean {
    return this.hasRole('Admin');
  }

  // System.ViewBasicHealth: all authenticated MVP roles.
  canViewSystemHealth(): boolean {
    return this.session.isAuthenticated();
  }

  // Chat.ViewOwnHistory: all authenticated MVP roles.
  canViewChatHistory(): boolean {
    return this.session.isAuthenticated()
      && this.hasAnyRole('Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin');
  }

  // Chat.ViewScopedHistory: Supervisor, Manager, Admin only.
  // KnowledgeAdmin is own-only for Sprint 21.
  canViewScopedChatHistory(): boolean {
    return this.hasAnyRole('Supervisor', 'Manager', 'Admin');
  }
}
