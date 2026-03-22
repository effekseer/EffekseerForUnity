# macOS dylib Notarization in CI

This repository notarizes the macOS native plugin (`EffekseerUnity.dylib`) in GitHub Actions.

- Workflow: `.github/workflows/build.yml`
- Script: `ci/notarize_macos_dylib.sh`

The `mac_ios` job does the following:

1. Build `EffekseerUnity.dylib`
2. Import the `.p12` certificate into a temporary keychain
3. List available signing identities with `security find-identity`
4. Sign the dylib using the SHA-1 hash from the keychain identity list
5. Submit the signed dylib to Apple Notary Service (`xcrun notarytool submit --wait`)
6. Upload the notarized dylib as part of the `mac_iOS` artifact

## Required GitHub Secrets

Set these repository secrets before running `push` or `pull_request` workflows:

1. `APPLE_CERT_P12_BASE64`: Base64-encoded `.p12` file of your `Developer ID Application` certificate (including private key)
2. `APPLE_CERT_P12_PASSWORD`: Password used when exporting the `.p12`
3. `APPLE_SIGNING_IDENTITY`: Signing identity name, for example `Developer ID Application: Example Corp (TEAMID1234)`
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

- On `push`, notarization secrets are mandatory. If any required secret is missing, the `mac_ios` job fails and reports the missing names.
- On `pull_request`, the same validation runs. If all secrets are available, signing and notarization proceed; otherwise the job fails with the missing names.
- The temporary keychain is added to the user keychain search list before `codesign` runs, and the script signs with the SHA-1 hash reported by `security find-identity`.

## Notes

- `xcrun stapler` is not used in this flow because the target artifact is a standalone `.dylib`.
- The merged package job consumes the notarized file at `Dev/Plugin/Assets/Effekseer/Plugins/macOS/EffekseerUnity.dylib`.
