import { FormGroup, ValidationErrors } from '@angular/forms';

import { ApiError } from '../../core/http/api-error.model';

export const ServerValidationErrorKey = 'server';

export function applyServerValidationErrors(formGroup: FormGroup, apiError: ApiError): void {
  for (const [fieldName, messages] of Object.entries(apiError.validationErrors)) {
    const control = findControlIgnoringCase(formGroup, fieldName);
    if (control) {
      control.setErrors({ ...control.errors, [ServerValidationErrorKey]: messages.join(' ') });
      control.markAsTouched();
    }
  }
}

export function clearServerValidationErrors(formGroup: FormGroup): void {
  for (const control of Object.values(formGroup.controls)) {
    const currentErrors = control.errors;
    if (currentErrors && ServerValidationErrorKey in currentErrors) {
      const remainingErrors: ValidationErrors = { ...currentErrors };
      delete remainingErrors[ServerValidationErrorKey];
      control.setErrors(Object.keys(remainingErrors).length > 0 ? remainingErrors : null);
    }
  }
}

function findControlIgnoringCase(formGroup: FormGroup, fieldName: string) {
  const matchingControlName = Object.keys(formGroup.controls).find(
    (controlName) => controlName.toLowerCase() === fieldName.toLowerCase(),
  );
  return matchingControlName ? formGroup.controls[matchingControlName] : null;
}
