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
