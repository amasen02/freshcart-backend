import { HttpInterceptorFn } from '@angular/common/http';

const SameOriginPrefixes: readonly string[] = ['/api/', '/hubs/'];

export const credentialsInterceptor: HttpInterceptorFn = (request, next) => {
  const isSameOriginGatewayRequest = SameOriginPrefixes.some((prefix) => request.url.startsWith(prefix));
  return isSameOriginGatewayRequest ? next(request.clone({ withCredentials: true })) : next(request);
};
