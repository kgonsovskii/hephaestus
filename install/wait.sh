wait_for_apt_dpkg_lock() {
  local max_wait="${WAIT_FOR_APT_LOCK_MAX:-900}"
  local interval=5
  local elapsed=0
  local busy

  while [ "$elapsed" -lt "$max_wait" ]; do
    busy=0
    if command -v fuser >/dev/null 2>&1; then
      fuser /var/lib/dpkg/lock-frontend >/dev/null 2>&1 && busy=1
      fuser /var/lib/dpkg/lock >/dev/null 2>&1 && busy=1
      fuser /var/lib/apt/lists/lock >/dev/null 2>&1 && busy=1
    fi
    if pgrep -x apt-get >/dev/null 2>&1 || pgrep -x apt >/dev/null 2>&1 \
        || pgrep -f '/usr/bin/unattended-upgrade' >/dev/null 2>&1 \
        || pgrep -f '/usr/sbin/unattended-upgrade' >/dev/null 2>&1 \
        || pgrep -f 'apt.systemd.daily' >/dev/null 2>&1; then
      busy=1
    fi
    if [ "$busy" -eq 0 ]; then
      return 0
    fi
    echo "[apt-lock] lock or apt in progress (${elapsed}s / ${max_wait}s)..."
    sleep "$interval"
    elapsed=$((elapsed + interval))
  done
  echo "[apt-lock] timed out after ${max_wait}s (dpkg/apt still busy)." >&2
  return 1
}

apt_get() {
  wait_for_apt_dpkg_lock
  DEBIAN_FRONTEND=noninteractive apt-get -o DPkg::Lock::Timeout=300 "$@"
}
