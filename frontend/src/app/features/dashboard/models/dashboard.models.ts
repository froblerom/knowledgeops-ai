// Dashboard API response models.
// All nullable fields are typed as X | null (not undefined).

export interface DashboardPeriod {
  from: string;
  to: string;
}

export interface DashboardCostInfo {
  available: boolean;
  // NEVER show $0.00 when available === false. Show "Not available" instead.
  estimatedTotal: number | null;
}

export interface DashboardTokenInfo {
  input: number | null;
  output: number | null;
  total: number | null;
}

export interface DashboardOverviewResponse {
  period: DashboardPeriod;
  questionsAsked: number;
  activeUsers: number;
  documentsUploaded: number;
  documentsProcessed: number;
  documentsFailed: number;
  // null means unavailable — never show 0 for null
  averageResponseLatencyMs: number | null;
  insufficientContextCount: number;
  providerFailureCount: number;
  usefulFeedbackCount: number;
  notUsefulFeedbackCount: number;
  cost: DashboardCostInfo;
}

export interface DashboardDocumentsResponse {
  period: DashboardPeriod;
  uploaded: number;
  processing: number;
  processed: number;
  failed: number;
  retrievalDisabled: number;
}

export interface DashboardChatResponse {
  period: DashboardPeriod;
  questionsAsked: number;
  activeUsers: number;
  averageResponseLatencyMs: number | null;
  retrievalLatencyMs: number | null;
  generationLatencyMs: number | null;
  totalRagLatencyMs: number | null;
  insufficientContextCount: number;
  providerFailureCount: number;
  tokens: DashboardTokenInfo;
  cost: DashboardCostInfo;
}

export interface DashboardFeedbackResponse {
  period: DashboardPeriod;
  useful: number;
  notUseful: number;
  total: number;
}

export interface DashboardDateParams {
  from?: string;
  to?: string;
}
