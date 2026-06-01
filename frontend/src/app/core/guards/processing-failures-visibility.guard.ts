import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RoleVisibilityService } from '../services/role-visibility.service';

// Navigation guidance only. The API remains authoritative for System.ViewProcessingFailures.
export const processingFailuresVisibilityGuard: CanActivateFn = () => {
  const visibility = inject(RoleVisibilityService);
  const router = inject(Router);
  return visibility.canViewProcessingFailures() ? true : router.createUrlTree(['/dashboard']);
};
