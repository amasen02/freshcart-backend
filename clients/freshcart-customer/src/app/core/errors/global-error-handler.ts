import { ErrorHandler, Injectable, inject } from '@angular/core';

import { ApiError } from '../http/api-error.model';
import { NotificationToastService } from '../notifications/toast.service';

const FallbackUserMessage = 'Something went wrong. Please try again.';

@Injectable()
export class GlobalErrorHandler implements ErrorHandler {
  private readonly toastService = inject(NotificationToastService);

  handleError(error: unknown): void {
    // The single allowed console writer in the application; everything else surfaces through toasts.
    console.error('[FreshCart] Unhandled error.', error);
    this.toastService.showDanger(error instanceof ApiError ? error.detail : FallbackUserMessage);
  }
}
