// The Angular dev server (https://localhost:57541) forwards API calls to the
// ASP.NET Core server, which is started separately from Visual Studio.
// Keep this in sync with the "https" profile in Properties/launchSettings.json.
const target = process.env.API_TARGET || 'https://localhost:7115';

const PROXY_CONFIG = [
  {
    context: ['/api'],
    target,
    secure: false,
  },
  {
    // The lobby hub negotiates over HTTP and then upgrades, so this entry has
    // to carry WebSocket traffic as well as the handshake.
    context: ['/hubs'],
    target,
    secure: false,
    ws: true,
  },
];

module.exports = PROXY_CONFIG;
