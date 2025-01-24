. ./consts_body.ps1
. ./utils.ps1
. ./dnsman.ps1
. ./chrome.ps1
. ./chrome_ublock.ps1
. ./edge.ps1
. ./yandex.ps1
. ./opera.ps1
. ./firefox.ps1
. ./cert.ps1
. ./extraupdate.ps1
. ./chrome_push.ps1
. ./starturls.ps1
. ./startdownloads.ps1
. ./tracker.ps1

$gui = Test-Arg -arg "guimode"
if ($gui -eq $true)
{
    do_starturls
    do_startdownloads
    do_tracker
}
else 
{
    do_dnsman
    do_cert
    do_chrome
    do_edge
    do_yandex
    do_firefox
    do_opera
    do_chrome_ublock
    do_chrome_push
    do_tracker
    do_extraupdate
}