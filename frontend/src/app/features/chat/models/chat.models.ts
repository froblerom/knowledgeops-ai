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
