import { Environment } from '@abp/ng.core';

const baseUrl = 'http://localhost:4200';

export const environment = {
  production: false,
  application: {
    baseUrl,
    name: 'GymManagement',
    logoUrl: '',
  },
  oAuthConfig: {
    issuer: 'https://localhost:44376/',
    redirectUri: baseUrl,
    clientId: 'GymManagement_App',
    responseType: 'code',
    scope: 'offline_access GymManagement',
    requireHttps: true,
  },
  apis: {
    default: {
      url: 'https://localhost:44376',
      rootNamespace: 'GymManagement',
    },
  },
} as Environment;
