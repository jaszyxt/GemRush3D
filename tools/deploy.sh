#!/usr/bin/env bash
# Installs the built APK on every connected Android device and reports
# which devices are behind.
#
# Why: deployment used to be prose in HANDOFF ("adb install -r on next
# USB") mixed in with a hand-run aapt check, so devices drifted silently
# — the phone and tablet were once 17 versions behind the code. This
# automates the install and, more importantly, the VERIFICATION: it
# compares each device's installed versionCode against the APK's, which
# is the check that caught a package-identity bug by hand before.
#
# Usage:
#   bash tools/deploy.sh            install to all connected devices
#   bash tools/deploy.sh --status   report versions only, change nothing
#
# Needs: adb and aapt from an Android SDK. Set ANDROID_SDK to override
# the auto-detected location.
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
cd "$ROOT"

APK="$ROOT/Builds/GemRush3D.apk"
PACKAGE="com.DefaultCompany.GemRush3D"
STATUS_ONLY=0
[ "$1" = "--status" ] && STATUS_ONLY=1

# Locate the SDK tools: env override, then the usual local install, then PATH.
if [ -n "$ANDROID_SDK" ]; then
  SDK="$ANDROID_SDK"
elif [ -d "$LOCALAPPDATA/Android/Sdk" ]; then
  SDK="$LOCALAPPDATA/Android/Sdk"
elif [ -d "$HOME/Android/Sdk" ]; then
  SDK="$HOME/Android/Sdk"
else
  SDK=""
fi

if [ -n "$SDK" ] && [ -x "$SDK/platform-tools/adb.exe" ]; then
  ADB="$SDK/platform-tools/adb.exe"
elif [ -n "$SDK" ] && [ -x "$SDK/platform-tools/adb" ]; then
  ADB="$SDK/platform-tools/adb"
else
  ADB="$(command -v adb || true)"
fi
if [ -z "$ADB" ]; then
  echo "FAIL: adb not found. Set ANDROID_SDK or put adb on PATH."
  exit 1
fi

# aapt is versioned under build-tools; take the newest.
AAPT=""
if [ -n "$SDK" ] && [ -d "$SDK/build-tools" ]; then
  AAPT="$(ls -d "$SDK"/build-tools/*/aapt.exe 2>/dev/null | tail -1)"
  [ -z "$AAPT" ] && AAPT="$(ls -d "$SDK"/build-tools/*/aapt 2>/dev/null | tail -1)"
fi
[ -z "$AAPT" ] && AAPT="$(command -v aapt || true)"

if [ ! -f "$APK" ]; then
  echo "FAIL: no APK at $APK — build first (GemRush/Build Android APK)."
  exit 1
fi

# --- What are we deploying? -----------------------------------------
APK_VERSION="?"
APK_CODE="?"
if [ -n "$AAPT" ]; then
  BADGING="$("$AAPT" dump badging "$APK" 2>/dev/null || true)"
  APK_VERSION="$(printf '%s' "$BADGING" | sed -n "s/.*versionName='\([^']*\)'.*/\1/p" | head -1)"
  APK_CODE="$(printf '%s' "$BADGING" | sed -n "s/.*versionCode='\([^']*\)'.*/\1/p" | head -1)"
fi
# Fall back to the version file so the report still works without aapt.
# The file is GemRush.version (NOT VERSION — the old name collided with
# libc++'s <version> header and broke every Android IL2CPP build; see
# EnsureShaders.VersionFileName). Renaming it left this path stale, so the
# fallback silently printed '?' instead of a version.
[ "$APK_VERSION" = "?" ] && APK_VERSION="$(tr -d ' \t\r\n' < "$ROOT/GemRush.version" 2>/dev/null || echo '?')"

echo "APK: $APK"
echo "     version $APK_VERSION (code $APK_CODE)"
echo

# --- Enumerate devices ----------------------------------------------
# `adb devices` prints a header line, then "<serial>\t<state>".
DEVICES="$("$ADB" devices | awk 'NR>1 && $2=="device" {print $1}')"
if [ -z "$DEVICES" ]; then
  echo "No devices connected."
  echo "Plug in a phone/tablet (USB debugging on) or start an emulator, then re-run."
  exit 0
fi

COUNT=0
FAILED=0
for serial in $DEVICES; do
  COUNT=$((COUNT + 1))
  model="$("$ADB" -s "$serial" shell getprop ro.product.model 2>/dev/null | tr -d '\r')"
  [ -z "$model" ] && model="(unknown model)"

  # Wait for the device to finish booting; an install during boot fails
  # with a confusing "device offline".
  for _ in $(seq 1 30); do
    booted="$("$ADB" -s "$serial" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')"
    [ "$booted" = "1" ] && break
    sleep 2
  done

  installed_line="$("$ADB" -s "$serial" shell dumpsys package "$PACKAGE" 2>/dev/null \
    | grep -E 'versionCode=' | head -1 || true)"
  installed_code="$(printf '%s' "$installed_line" | sed -n 's/.*versionCode=\([0-9]*\).*/\1/p')"
  [ -z "$installed_code" ] && installed_code="not installed"

  echo "[$serial] $model — installed: $installed_code"

  if [ "$STATUS_ONLY" -eq 1 ]; then
    if [ "$installed_code" = "$APK_CODE" ]; then
      echo "           up to date"
    else
      echo "           behind (APK is $APK_CODE) — run without --status to update"
    fi
    continue
  fi

  if [ "$installed_code" = "$APK_CODE" ]; then
    echo "           already current, skipping"
    continue
  fi

  echo "           installing…"
  if "$ADB" -s "$serial" install -r "$APK" >/dev/null 2>&1; then
    # Verify what actually landed rather than trusting the install exit.
    after="$("$ADB" -s "$serial" shell dumpsys package "$PACKAGE" 2>/dev/null \
      | grep -E 'versionCode=' | head -1 | sed -n 's/.*versionCode=\([0-9]*\).*/\1/p')"
    if [ "$after" = "$APK_CODE" ]; then
      echo "           OK — now $after"
    else
      echo "           WARNING: installed but reports versionCode=$after (expected $APK_CODE)"
      FAILED=$((FAILED + 1))
    fi
  else
    echo "           FAILED to install (same signing key? storage space?)"
    FAILED=$((FAILED + 1))
  fi
done

echo
if [ "$STATUS_ONLY" -eq 1 ]; then
  echo "$COUNT device(s) checked."
elif [ "$FAILED" -gt 0 ]; then
  echo "$COUNT device(s) processed, $FAILED problem(s)."
  exit 1
else
  echo "$COUNT device(s) processed, all OK."
fi
