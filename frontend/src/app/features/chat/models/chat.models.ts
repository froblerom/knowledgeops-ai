export type ChatAnswerState =
  | 'GroundedAnswer'
  | 'InsufficientContext'
  | 'ProviderFailure';

export interface AskChatQuestionRequest {
  questionText: string;
  chatSessionId?: string;
}

export interface ChatCitation {
  citationId: string;
  documentId: string;
  documentTitle: string | null;
  chunkId: string;
  pageNumber: number | null;
  sectionLabel: string | null;
  score: number | null;
  rank: number;
}

export interface ChatResponseMetadata {
  latencyMs: number | null;
  retrievalResultCount: number;
  estimatedCost: number | null;
}

export interface AskChatQuestionResponse {
  chatSessionId: string;
  chatInteractionId: string;
  answerState: ChatAnswerState;
  answer: string | null;
  insufficientContext: boolean;
  citations: ChatCitation[];
  metadata: ChatResponseMetadata;
  correlationId: string | null;
}

export type AnswerFeedbackRating = 'Useful' | 'NotUseful';

export interface SubmitAnswerFeedbackRequest {
  rating: AnswerFeedbackRating;
}

export interface UpdateAnswerFeedbackRequest {
  rating: AnswerFeedbackRating;
}

export interface AnswerFeedbackResponse {
  feedbackId: string;
  chatInteractionId: string;
  userId: string;
  rating: AnswerFeedbackRating;
  createdAt: string;
  updatedAt: string;
}

export interface AnswerFeedbackReviewItem {
  feedbackId: string;
  chatInteractionId: string;
  userId: string;
  rating: AnswerFeedbackRating;
  createdAt: string;
  updatedAt: string;
}

export interface AnswerFeedbackReviewResponse {
  usefulCount: number;
  notUsefulCount: number;
  items: AnswerFeedbackReviewItem[];
}

export interface CreateChatSessionRequest {
  title?: string;
}

export interface CreateChatSessionResponse {
  chatSessionId: string;
}

export interface ChatSessionSummary {
  chatSessionId: string;
  title: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  lastInteractionAt: string | null;
  interactionCount: number;
}

export interface ChatInteractionSummary {
  chatInteractionId: string;
  answerState: ChatAnswerState;
  insufficientContext: boolean;
  createdAt: string;
}

export interface ChatSessionDetail {
  chatSessionId: string;
  title: string | null;
  status: string;
  createdAt: string;
  updatedAt: string;
  lastInteractionAt: string | null;
  interactions: ChatInteractionSummary[];
}

export interface ChatInteractionMetadata {
  retrievalCandidateCount: number;
  retrievalLatencyMs: number | null;
  generationLatencyMs: number | null;
  totalLatencyMs: number | null;
  tokenUsageInput: number | null;
  tokenUsageOutput: number | null;
  estimatedCost: number | null;
  aiProvider: string | null;
  aiModel: string | null;
  providerFailureCode: string | null;
}

export interface ChatCitationHistory {
  citationId: string;
  chatInteractionId: string;
  documentId: string;
  chunkId: string;
  rank: number;
  documentTitle: string;
  pageNumber: number | null;
  sectionLabel: string | null;
  relevanceScore: number | null;
}

export interface ChatInteractionDetail {
  chatInteractionId: string;
  chatSessionId: string;
  answerState: ChatAnswerState;
  insufficientContext: boolean;
  questionText: string | null;
  answerText: string | null;
  promptVersion: string | null;
  correlationId: string | null;
  metadata: ChatInteractionMetadata;
  citations: ChatCitationHistory[];
  createdAt: string;
}
