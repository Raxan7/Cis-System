export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  correlationId?: string;
  code?: string;
  errors?: Record<string, string[]>;
};

export class ApiError extends Error {
  readonly status: number;
  readonly problem?: ProblemDetails;

  constructor(message: string, status: number, problem?: ProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

export function isProblemDetails(value: unknown): value is ProblemDetails {
  return typeof value === 'object' && value !== null && ('title' in value || 'status' in value || 'detail' in value);
}

export function problemDetailsMessage(problem?: ProblemDetails) {
  if (!problem) {
    return 'The request could not be completed.';
  }

  if (problem.errors) {
    const validationMessage = Object.entries(problem.errors)
      .flatMap(([field, messages]) => messages.map((message) => `${field}: ${message}`))
      .join(' ');
    if (validationMessage) {
      return validationMessage;
    }
  }

  return problem.detail || problem.title || 'The request could not be completed.';
}
