export interface ProcessingFailure {
  documentId: string;
  title: string;
  processingStatus: 'Failed';
  failureReason: string | null;
  failedAt: string;
}

export interface AuditLogEntry {
  auditLogEntryId: string;
  eventType: string;
  message: string;
  severity: string;
  userId: string | null;
  entityType: string | null;
  entityId: string | null;
  correlationId: string | null;
  createdAt: string;
}

export interface AuditLogFilters {
  from?: string;
  to?: string;
  eventType?: string;
}
