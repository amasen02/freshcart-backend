export const SupportHubClientMethods = {
  requestChat: 'RequestChat',
  sendMessage: 'SendMessage',
  setTyping: 'SetTyping',
  endChat: 'EndChat',
} as const;

export const SupportHubServerEvents = {
  chatAssigned: 'chatAssigned',
  messageReceived: 'messageReceived',
  participantTyping: 'participantTyping',
  chatEnded: 'chatEnded',
  queuePositionChanged: 'queuePositionChanged',
} as const;
