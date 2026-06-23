export type ChatSessionStatus = 'Queued' | 'Active' | 'Ended';

export type ChatSenderRole = 'Customer' | 'Agent';

export interface ChatSessionDto {
  readonly sessionId: string;
  readonly topic: string;
  readonly customerId: string;
  readonly customerDisplayName: string;
  readonly agentId: string | null;
  readonly agentDisplayName: string | null;
  readonly status: ChatSessionStatus;
  readonly startedOnUtc: string;
}

export interface ChatMessageDto {
  readonly messageId: string;
  readonly sessionId: string;
  readonly senderId: string;
  readonly senderDisplayName: string;
  readonly senderRole: ChatSenderRole;
  readonly text: string;
  readonly sentOnUtc: string;
}
