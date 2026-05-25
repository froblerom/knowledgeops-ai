import { HttpInterceptorFn } from '@angular/common/http';

// Pass-through only. No JWT, no localStorage, no Authorization header injection.
// Real auth headers will be added in Sprint 6.
export const apiInterceptor: HttpInterceptorFn = (req, next) => next(req);
