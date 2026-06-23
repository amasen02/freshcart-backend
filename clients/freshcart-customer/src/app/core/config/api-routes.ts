export const ApiRoutes = {
  auth: {
    signIn: '/api/auth/sign-in',
    signUp: '/api/auth/sign-up',
    signOut: '/api/auth/sign-out',
    antiForgeryToken: '/api/auth/anti-forgery-token',
  },
  account: {
    me: '/api/account/me',
    mfaEnroll: '/api/account/mfa/enroll',
    mfaVerify: '/api/account/mfa/verify',
    mfaDisable: '/api/account/mfa/disable',
  },
  basket: {
    root: '/api/basket',
    items: '/api/basket/items',
    item: (productId: string) => `/api/basket/items/${productId}`,
    coupon: '/api/basket/coupon',
    checkout: '/api/basket/checkout',
  },
  catalog: {
    products: '/api/products',
    productBySlug: (slug: string) => `/api/products/${slug}`,
    productSearch: '/api/products/search',
    categories: '/api/categories',
    brands: '/api/brands',
  },
  orders: {
    root: '/api/orders',
    byId: (orderId: string) => `/api/orders/${orderId}`,
    cancel: (orderId: string) => `/api/orders/${orderId}/cancel`,
  },
  delivery: {
    byOrderId: (orderId: string) => `/api/delivery/orders/${orderId}`,
  },
  reviews: {
    root: '/api/reviews',
    byProductSku: (productSku: string) => `/api/reviews/product/${productSku}`,
  },
  reporting: {
    salesOverview: '/api/reporting/dashboards/sales/overview',
    salesTimeSeries: '/api/reporting/dashboards/sales/time-series',
    salesBreakdown: '/api/reporting/dashboards/sales/breakdown',
    topProducts: '/api/reporting/reports/products/top',
  },
  notifications: {
    root: '/api/notifications',
    unreadCount: '/api/notifications/unread-count',
    markAsRead: (notificationId: string) => `/api/notifications/${notificationId}/read`,
  },
  support: {
    activeSessions: '/api/support/sessions/active',
    sessionMessages: (sessionId: string) => `/api/support/sessions/${sessionId}/messages`,
  },
  hubs: {
    notifications: '/hubs/notifications',
    support: '/hubs/support',
  },
} as const;
