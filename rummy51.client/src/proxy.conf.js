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
  {
    // Where Google sends the browser back. It has to arrive on this origin:
    // the whole sign-in is a sequence of navigations, and a redirect that
    // changed host midway would leave the player on the wrong port.
    context: ['/signin-google'],
    target,
    secure: false,
  },
];

module.exports = PROXY_CONFIG;
