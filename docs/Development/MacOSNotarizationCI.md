# macOS dylib Notarization in CI

This repository notarizes the macOS native plugin (`EffekseerUnity.dylib`) in GitHub Actions.

- Workflow: `.github/workflows/build.yml`
- Script: `ci/notarize_macos_dylib.sh`

The `mac_ios` job does the following:

1. Build `EffekseerUnity.dylib`
2. Sign the dylib with `Developer ID Application`
3. Submit the signed dylib to Apple Notary Service (`xcrun notarytool submit --wait`)
4. Upload the notarized dylib as part of the `mac_iOS` artifact

## Required GitHub Secrets

Set these repository secrets before running `push` workflows:

1. `APPLE_CERT_P12_BASE64`: Base64-encoded `.p12` file of your `Developer ID Application` certificate (including private key)
2. `APPLE_CERT_P12_PASSWORD`: Password used when exporting the `.p12`
3. `APPLE_SIGNING_IDENTITY`: Full signing identity, for example `Developer ID Application: Example Corp (TEAMID1234)`
4. `APPLE_NOTARY_APPLE_ID`: Apple ID used for notarization
5. `APPLE_NOTARY_TEAM_ID`: Apple Developer Team ID
6. `APPLE_NOTARY_APP_PASSWORD`: App-specific password for the Apple ID above

## How to prepare the `.p12` secret value

Example (macOS):

```bash
base64 -i developer_id_application.p12 | pbcopy
```

Example (Linux):

```bash
base64 -w 0 developer_id_application.p12
```

Paste the Base64 result into `APPLE_CERT_P12_BASE64`.

## Workflow behavior

- On `push`, notarization secrets are mandatory. If any required secret is missing, the `mac_ios` job fails.
- On `pull_request`, notarization runs only when all notarization secrets are available. Otherwise it is skipped.

## Notes

- `xcrun stapler` is not used in this flow because the target artifact is a standalone `.dylib`.
- The merged package job consumes the notarized file at `Dev/Plugin/Assets/Effekseer/Plugins/macOS/EffekseerUnity.dylib`.
