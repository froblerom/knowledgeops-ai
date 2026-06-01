import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { RoleVisibilityService } from '../services/role-visibility.service';

// UX navigation guidance only. The API remains authoritative for Dashboard.* permissions.
// This guard is NOT a security boundary — backend enforces authorization independently.
export const dashboardVisibilityGuard: CanActivateFn = () => {
  const visibility = inject(RoleVisibilityService);
  const router = inject(Router);
  return visibility.canViewDashboard() ? true : router.createUrlTree(['/chat']);
};
