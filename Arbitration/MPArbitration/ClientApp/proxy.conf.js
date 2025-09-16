const { env } = require('process');

const target = env.ASPNETCORE_HTTPS_PORT ? `https://localhost:${env.ASPNETCORE_HTTPS_PORT}` :
  env.ASPNETCORE_URLS ? env.ASPNETCORE_URLS.split(';')[0] : 'http://localhost:48847';

const PROXY_CONFIG = [
  {
    context: [
      "/api/authorities",
      "/api/batching",
      "/api/briefs",
      "/api/customers",
      "/api/arbitration",
      "/api/arbitrators",
      "/api/benchmark",
      "/api/cases",
      "/api/mde",
      "/api/notes",
      "/api/notifications",
      "/api/payors",
      "/api/procedurecodes",
      "/api/settlements",
      "/api/templates",
      "/api/Dispute"
   ],
    target: target,
    secure: false,
    timeout: 0,
    headers: {
      Connection: 'Keep-Alive'
    }
  }
]

module.exports = PROXY_CONFIG;
