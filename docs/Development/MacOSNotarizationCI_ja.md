# CIでのmacOS dylib公証

このリポジトリでは、GitHub Actions上で macOS ネイティブプラグイン（`EffekseerUnity.dylib`）を公証します。
通常はこの公証済み成果物を配布するため、ユーザー側での公証は不要です。
ただし、ユーザー自身でネイティブプラグインをビルドする場合は、公証が必要です。

- ワークフロー: `.github/workflows/build.yml`
- 実行スクリプト: `ci/notarize_macos_dylib.sh`

`mac_ios` ジョブでは次を実行します。

1. `EffekseerUnity.dylib` をビルド
2. `Developer ID Application` 証明書で署名
3. Apple Notary Service に提出（`xcrun notarytool submit --wait`）
4. 公証済み dylib を `mac_iOS` アーティファクトとしてアップロード

## 必須の GitHub Secrets

`push` ワークフロー実行前に、以下のリポジトリシークレットを設定してください。

1. `APPLE_CERT_P12_BASE64`: `Developer ID Application` 証明書（秘密鍵を含む）を `.p12` で書き出し、Base64化した値
2. `APPLE_CERT_P12_PASSWORD`: `.p12` エクスポート時のパスワード
3. `APPLE_SIGNING_IDENTITY`: 署名ID（例: `Developer ID Application: Example Corp (TEAMID1234)`）
4. `APPLE_NOTARY_APPLE_ID`: 公証に使用する Apple ID
5. `APPLE_NOTARY_TEAM_ID`: Apple Developer Team ID
6. `APPLE_NOTARY_APP_PASSWORD`: 上記 Apple ID の app-specific password

## `.p12` の Base64 値を作る方法

macOS の例:

```bash
base64 -i developer_id_application.p12 | pbcopy
```

Linux の例:

```bash
base64 -w 0 developer_id_application.p12
```

出力された文字列を `APPLE_CERT_P12_BASE64` に設定してください。

## ワークフローの挙動

- `push` では公証用シークレットが必須です。1つでも不足していると `mac_ios` ジョブは失敗します。
- `pull_request` では、必要シークレットがすべて利用可能な場合のみ公証を実行し、足りない場合は公証ステップをスキップします。

## 補足

- 対象が単体 `.dylib` のため、このフローでは `xcrun stapler` は使用しません。
- マージジョブは `Dev/Plugin/Assets/Effekseer/Plugins/macOS/EffekseerUnity.dylib` の公証済みファイルを使用します。
