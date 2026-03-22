# CI での macOS dylib 署名と notarization

このリポジトリでは、GitHub Actions 上で macOS ネイティブプラグイン `EffekseerUnity.dylib` を署名して notarization しています。

- Workflow: `.github/workflows/build.yml`
- Script: `ci/notarize_macos_dylib.sh`

`mac_ios` ジョブでは次の順で処理します。

1. `EffekseerUnity.dylib` をビルドする
2. `.p12` 証明書を一時 keychain に import する
3. `security find-identity` で利用可能な署名 identity を表示する
4. keychain に入った identity の SHA-1 hash を使って `codesign` する
5. Apple Notary Service に `xcrun notarytool submit --wait` で提出する
6. notarization 済みの dylib を `mac_iOS` artifact の一部としてアップロードする

## 必要な GitHub Secrets

`push` と `pull_request` のどちらでも、署名ステップを実行するには次の secrets が必要です。

1. `APPLE_CERT_P12_BASE64`: `Developer ID Application` 証明書の `.p12` を Base64 化した値
2. `APPLE_CERT_P12_PASSWORD`: `.p12` のエクスポート時に設定したパスワード
3. `APPLE_SIGNING_IDENTITY`: 署名 identity の名前
4. `APPLE_NOTARY_APPLE_ID`: notarization に使う Apple ID
5. `APPLE_NOTARY_TEAM_ID`: Apple Developer Team ID
6. `APPLE_NOTARY_APP_PASSWORD`: Apple ID の app-specific password

`APPLE_SIGNING_IDENTITY` は、`security find-identity` の出力と一致する必要があります。実際の CI では、`codesign` はこの名前ではなく、keychain から取得した SHA-1 hash を使って署名します。

## `.p12` の Base64 文字列の作成方法

macOS の例:

```bash
base64 -i developer_id_application.p12 | pbcopy
```

Linux の例:

```bash
base64 -w 0 developer_id_application.p12
```

出力した文字列を `APPLE_CERT_P12_BASE64` に設定してください。

## ワークフローの挙動

- `push` では、必要な secret が 1 つでも欠けていると `mac_ios` ジョブが失敗します。
- `pull_request` でも同じチェックを行います。secret が揃っていれば署名と notarization を実行し、足りなければ不足している secret 名を出して失敗します。
- `security default-keychain` と `security list-keychains` を使って一時 keychain を署名対象として明示しています。

## 補足

- このフローでは、対象が単体 `.dylib` なので `xcrun stapler` は使いません。
- merge 後のパッケージジョブは `Dev/Plugin/Assets/Effekseer/Plugins/macOS/EffekseerUnity.dylib` の成果物を利用します。
