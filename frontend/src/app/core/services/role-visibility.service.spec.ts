import { TestBed } from '@angular/core/testing';
import { RoleVisibilityService } from './role-visibility.service';
import { AuthSessionService, StoredSession } from './auth-session.service';

// UX visibility tests: these tests prove role-based visibility helpers work correctly.
// They do NOT prove authorization — backend tests in KnowledgeOps.Api.Tests prove enforcement.
// See: tests/KnowledgeOps.Api.Tests/Authorization/AuthorizationApiTests.cs

const makeSession = (roles: string[], expiresAt: Date = new Date(Date.now() + 3600_000)): StoredSession => ({
  accessToken: 'test-token',
  expiresAt: expiresAt.toISOString(),
  userId: '00000000-0000-0000-0000-000000000001',
  email: 'test@example.com',
  displayName: 'Test User',
  organizationId: '11111111-1111-4111-8111-111111111111',
  roles,
});

describe('RoleVisibilityService', () => {
  let service: RoleVisibilityService;
  let sessionService: AuthSessionService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(RoleVisibilityService);
    sessionService = TestBed.inject(AuthSessionService);
  });

  // ── canAskChat ───────────────────────────────────────────────────────────────

  it('canAskChat returns true for all authenticated MVP roles', () => {
    for (const role of ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canAskChat()).toBe(true);
    }
  });

  it('canAskChat returns false when not authenticated', () => {
    localStorage.clear();
    expect(service.canAskChat()).toBe(false);
  });

  // ── canViewDocuments ─────────────────────────────────────────────────────────

  it('canViewDocuments returns true for KnowledgeAdmin', () => {
    sessionService.saveSession(makeSession(['KnowledgeAdmin']));
    expect(service.canViewDocuments()).toBe(true);
  });

  it('canViewDocuments returns true for Manager', () => {
    sessionService.saveSession(makeSession(['Manager']));
    expect(service.canViewDocuments()).toBe(true);
  });

  it('canViewDocuments returns true for Admin', () => {
    sessionService.saveSession(makeSession(['Admin']));
    expect(service.canViewDocuments()).toBe(true);
  });

  it('canViewDocuments returns false for Agent', () => {
    sessionService.saveSession(makeSession(['Agent']));
    expect(service.canViewDocuments()).toBe(false);
  });

  it('canViewDocuments returns false for Supervisor', () => {
    sessionService.saveSession(makeSession(['Supervisor']));
    expect(service.canViewDocuments()).toBe(false);
  });

  // ── canUploadDocuments ───────────────────────────────────────────────────────

  it('canUploadDocuments returns true for KnowledgeAdmin', () => {
    sessionService.saveSession(makeSession(['KnowledgeAdmin']));
    expect(service.canUploadDocuments()).toBe(true);
  });

  it('canUploadDocuments returns true for Admin', () => {
    sessionService.saveSession(makeSession(['Admin']));
    expect(service.canUploadDocuments()).toBe(true);
  });

  it('canUploadDocuments returns false for Agent', () => {
    sessionService.saveSession(makeSession(['Agent']));
    expect(service.canUploadDocuments()).toBe(false);
  });

  it('canUploadDocuments returns false for Supervisor', () => {
    sessionService.saveSession(makeSession(['Supervisor']));
    expect(service.canUploadDocuments()).toBe(false);
  });

  it('canUploadDocuments returns false for Manager', () => {
    sessionService.saveSession(makeSession(['Manager']));
    expect(service.canUploadDocuments()).toBe(false);
  });

  // ── canViewDashboard ─────────────────────────────────────────────────────────

  it('canViewDashboard returns true for KnowledgeAdmin, Manager, Admin', () => {
    for (const role of ['KnowledgeAdmin', 'Manager', 'Admin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewDashboard()).toBe(true);
    }
  });

  it('canViewDashboard returns false for Agent and Supervisor', () => {
    for (const role of ['Agent', 'Supervisor']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewDashboard()).toBe(false);
    }
  });

  // ── canViewFeedbackReview ────────────────────────────────────────────────────

  it('canViewFeedbackReview returns true for Supervisor, Manager, Admin', () => {
    for (const role of ['Supervisor', 'Manager', 'Admin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewFeedbackReview()).toBe(true);
    }
  });

  it('canViewFeedbackReview returns false for Agent and KnowledgeAdmin', () => {
    for (const role of ['Agent', 'KnowledgeAdmin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewFeedbackReview()).toBe(false);
    }
  });

  // ── canViewAdmin ─────────────────────────────────────────────────────────────

  it('canViewAdmin returns true for Admin only', () => {
    sessionService.saveSession(makeSession(['Admin']));
    expect(service.canViewAdmin()).toBe(true);
  });

  it('canViewAdmin returns false for all non-Admin roles', () => {
    for (const role of ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewAdmin()).toBe(false);
    }
  });

  // ── canViewSystemHealth ──────────────────────────────────────────────────────

  it('canViewSystemHealth returns true for all authenticated MVP roles', () => {
    for (const role of ['Agent', 'Supervisor', 'KnowledgeAdmin', 'Manager', 'Admin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canViewSystemHealth()).toBe(true);
    }
  });

  // ── Unknown/empty roles ──────────────────────────────────────────────────────

  // ── canDisableDocumentRetrieval ──────────────────────────────────────────────

  it('canDisableDocumentRetrieval returns true for KnowledgeAdmin and Admin', () => {
    for (const role of ['KnowledgeAdmin', 'Admin']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canDisableDocumentRetrieval()).toBe(true);
    }
  });

  it('canDisableDocumentRetrieval returns false for Manager, Agent, Supervisor', () => {
    for (const role of ['Manager', 'Agent', 'Supervisor']) {
      sessionService.saveSession(makeSession([role]));
      expect(service.canDisableDocumentRetrieval()).toBe(false);
    }
  });

  // ── Unknown/empty roles ──────────────────────────────────────────────────────

  it('unknown role returns false for all restricted visibility helpers', () => {
    sessionService.saveSession(makeSession(['UnknownRole']));
    expect(service.canAskChat()).toBe(false);
    expect(service.canViewDocuments()).toBe(false);
    expect(service.canUploadDocuments()).toBe(false);
    expect(service.canDisableDocumentRetrieval()).toBe(false);
    expect(service.canViewDashboard()).toBe(false);
    expect(service.canViewFeedbackReview()).toBe(false);
    expect(service.canViewAdmin()).toBe(false);
  });

  it('empty roles returns false for restricted visibility helpers', () => {
    sessionService.saveSession(makeSession([]));
    expect(service.canAskChat()).toBe(false);
    expect(service.canViewDocuments()).toBe(false);
    expect(service.canUploadDocuments()).toBe(false);
    expect(service.canDisableDocumentRetrieval()).toBe(false);
    expect(service.canViewDashboard()).toBe(false);
    expect(service.canViewFeedbackReview()).toBe(false);
    expect(service.canViewAdmin()).toBe(false);
  });

  it('no session returns false for all visibility helpers', () => {
    localStorage.clear();
    expect(service.canAskChat()).toBe(false);
    expect(service.canViewDocuments()).toBe(false);
    expect(service.canUploadDocuments()).toBe(false);
    expect(service.canDisableDocumentRetrieval()).toBe(false);
    expect(service.canViewDashboard()).toBe(false);
    expect(service.canViewFeedbackReview()).toBe(false);
    expect(service.canViewAdmin()).toBe(false);
    expect(service.canViewSystemHealth()).toBe(false);
  });
});
