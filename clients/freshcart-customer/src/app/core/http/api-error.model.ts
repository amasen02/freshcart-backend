import { HttpErrorResponse } from '@angular/common/http';

const NetworkErrorTitle = 'Connection problem.';
const NetworkErrorDetail = 'Unable to reach the server. Check your connection and try again.';
const UnexpectedErrorTitle = 'Something went wrong.';
const UnexpectedErrorDetail = 'An unexpected error occurred. Please try again.';

interface ProblemDetailsBody {
  readonly title?: unknown;
  readonly detail?: unknown;
  readonly traceId?: unknown;
  readonly validationErrors?: unknown;
}

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly detail: string;
  readonly validationErrors: Readonly<Record<string, readonly string[]>>;
  readonly traceId: string | null;

  private constructor(
    status: number,
    title: string,
    detail: string,
    validationErrors: Readonly<Record<string, readonly string[]>>,
    traceId: string | null,
  ) {
    super(detail);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.detail = detail;
    this.validationErrors = validationErrors;
    this.traceId = traceId;
  }

  get hasValidationErrors(): boolean {
    return Object.keys(this.validationErrors).length > 0;
  }

  static fromUnknown(error: unknown): ApiError {
    if (error instanceof ApiError) {
      return error;
    }

    if (error instanceof HttpErrorResponse) {
      return ApiError.fromHttpErrorResponse(error);
    }

    return new ApiError(0, UnexpectedErrorTitle, UnexpectedErrorDetail, {}, null);
  }

  private static fromHttpErrorResponse(response: HttpErrorResponse): ApiError {
    if (response.status === 0) {
      return new ApiError(0, NetworkErrorTitle, NetworkErrorDetail, {}, null);
    }

    const body: ProblemDetailsBody =
      typeof response.error === 'object' && response.error !== null ? (response.error as ProblemDetailsBody) : {};

    return new ApiError(
      response.status,
      typeof body.title === 'string' ? body.title : UnexpectedErrorTitle,
      typeof body.detail === 'string' ? body.detail : UnexpectedErrorDetail,
      ApiError.parseValidationErrors(body.validationErrors),
      typeof body.traceId === 'string' ? body.traceId : null,
    );
  }

  private static parseValidationErrors(value: unknown): Readonly<Record<string, readonly string[]>> {
    if (typeof value !== 'object' || value === null) {
      return {};
    }

    const parsedErrors: Record<string, readonly string[]> = {};
    for (const [fieldName, messages] of Object.entries(value)) {
      if (Array.isArray(messages)) {
        parsedErrors[fieldName] = messages.filter((message): message is string => typeof message === 'string');
      }
    }

    return parsedErrors;
  }
}
