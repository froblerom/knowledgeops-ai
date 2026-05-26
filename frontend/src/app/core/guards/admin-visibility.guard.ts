import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RoleVisibilityService } from '../services/role-visibility.service';

// Navigation guidance only. The API remains authoritative for Users.* permissions.
export const adminVisibilityGuard: CanActivateFn = () => {
  const visibility = inject(RoleVisibilityService);
  const router = inject(Router);
  return visibility.canViewAdmin() ? true : router.createUrlTree(['/dashboard']);
};
