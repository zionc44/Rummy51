// The Angular dev server (https://localhost:57541) forwards API calls to the
// ASP.NET Core server, which is started separately from Visual Studio.
// Keep this in sync with the "https" profile in Properties/launchSettings.json.
const target = process.env.API_TARGET || 'https://localhost:7115';

const PROXY_CONFIG = [
  {
    context: [
      "/weatherforecast",
    ],
    target,
    secure: false
  }
]

module.exports = PROXY_CONFIG;
