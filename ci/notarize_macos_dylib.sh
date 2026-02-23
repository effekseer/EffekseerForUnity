#!/usr/bin/env bash
set -euo pipefail

if [ "$#" -ne 1 ]; then
  echo "Usage: $0 <path-to-dylib>"
  exit 1
fi

PLUGIN_PATH="$1"
if [ ! -f "$PLUGIN_PATH" ]; then
  echo "Dylib not found: $PLUGIN_PATH"
  exit 1
fi

required_envs=(
  APPLE_CERT_P12_BASE64
  APPLE_CERT_P12_PASSWORD
  APPLE_SIGNING_IDENTITY
  APPLE_NOTARY_APPLE_ID
  APPLE_NOTARY_TEAM_ID
  APPLE_NOTARY_APP_PASSWORD
)

for name in "${required_envs[@]}"; do
  if [ -z "${!name:-}" ]; then
    echo "Required environment variable is missing: $name"
    exit 1
  fi
done

TMP_DIR="${RUNNER_TEMP:-/tmp}"
CERTIFICATE_PATH="$TMP_DIR/effekseer_apple_certificate.p12"
KEYCHAIN_PATH="$TMP_DIR/effekseer-signing.keychain-db"
NOTARY_ARCHIVE_PATH="$TMP_DIR/effekseer_unity_macos_notary.zip"
KEYCHAIN_PASSWORD="$(uuidgen)"

cleanup() {
  security delete-keychain "$KEYCHAIN_PATH" >/dev/null 2>&1 || true
  rm -f "$CERTIFICATE_PATH" "$NOTARY_ARCHIVE_PATH"
}
trap cleanup EXIT

decode_base64_to_file() {
  local output="$1"
  if printf '%s' "$APPLE_CERT_P12_BASE64" | base64 --decode >"$output" 2>/dev/null; then
    return 0
  fi

  printf '%s' "$APPLE_CERT_P12_BASE64" | base64 -D >"$output"
}

decode_base64_to_file "$CERTIFICATE_PATH"

security create-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
security set-keychain-settings -lut 21600 "$KEYCHAIN_PATH"
security unlock-keychain -p "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"
security import "$CERTIFICATE_PATH" -k "$KEYCHAIN_PATH" -P "$APPLE_CERT_P12_PASSWORD" -T /usr/bin/codesign -T /usr/bin/security
security set-key-partition-list -S apple-tool:,apple:,codesign: -s -k "$KEYCHAIN_PASSWORD" "$KEYCHAIN_PATH"

echo "Signing $PLUGIN_PATH"
codesign --force --keychain "$KEYCHAIN_PATH" --sign "$APPLE_SIGNING_IDENTITY" --options runtime --timestamp "$PLUGIN_PATH"
codesign --verify --verbose=2 "$PLUGIN_PATH"

ditto -c -k --keepParent "$PLUGIN_PATH" "$NOTARY_ARCHIVE_PATH"

echo "Submitting for notarization..."
xcrun notarytool submit "$NOTARY_ARCHIVE_PATH" \
  --apple-id "$APPLE_NOTARY_APPLE_ID" \
  --team-id "$APPLE_NOTARY_TEAM_ID" \
  --password "$APPLE_NOTARY_APP_PASSWORD" \
  --wait

echo "Notarization completed: $PLUGIN_PATH"
