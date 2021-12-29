# 使い方

## サンプルプロジェクト

下記の場所にEffekseerプラグインを使用したサンプルシーンがあります。

- Example/EfkBasic
- Example/EfkTimeline

![](../img/unity_example.png)

## リソースファイルについて

Unityのプロジェクトにエフェクトファイル(*.efkproj, *.efk, *.efkefc)やテクスチャ、サウンドを配置します。  
\*.efk,efkproj,efkefcファイルをインポートすると、.efk,efkproj,efkefcファイルのほかにEffectAssetが生成されます。 

![](../img/unity_resource.png)

.efk,efkproj,efkefcファイルは削除しても問題ありません。
また、現在はカスタムパッケージ作成の時に.efk,efkproj,efkefcファイルを含めてはいけません。

マテリアルおよびそのキャッシュの.efkmat、.efkmatdにも対応しています。

テクスチャやエフェクト、マテリアル等を同時にインポートするとリソースの割り当てができず、見た目がおかしくなることがあります。
そのような場合、Reimportしてください。

### Scale

読み込んだエフェクトの大きさが小さいことがあります。その場合は、EffectAsssetを選択し、**Scale** のパラメーターを変更します。
EffectEmitterのScaleを変更することでエフェクトの大きさを変更することもできますが、この方法だとエフェクトの設定によっては拡大されないことがあります。

![](../img/EffectAsset_Scale.png)


## エミッタを使って再生する方法

### 準備

エフェクトのエミッタコンポーネントをGameObjectにAddすることで、  
GameObjectに連動したエフェクトの再生を行うことができます。  

GameObjectに対してEffekseerEmitterを追加します。

![](../img/add_component.png)

### インスペクタのプロパティ

- Effect Asset: 先ほどインポートしたエフェクトアセットを指定します。
- Play On Start: チェックを入れると、シーン開始時(Start()のタイミング)に自動的に再生します。
- IsLooping: 再生終了したら自動的に再生をリクエストします。

![](../img/unity_emitter.png)

### プレビュー

EffekseerEmitterコンポーネントを設定するとシーンビューにプレビュー用のコントローラーが表示されます。
プレイを押さなくともシーンビューから操作してエフェクトをゲームビューでプレビューできます。

![](../img/unity_emitter_component_scene_view.png)


### 特徴

設置するエフェクトやキャラクターに追従するようなエフェクトに適しています。

## スクリプトから直接再生する方法

### スクリプト

EffekseerSystem.PlayEffect()を使うことで、スクリプトからエフェクトを再生することができます。  

以下サンプルコードです。

```
void Start()
{
    // エフェクトを取得する。
    EffekseerEffectAsset effect = Resources.Load<EffekseerEffectAsset> ("Laser01");
    // transformの位置でエフェクトを再生する
    EffekseerHandle handle = EffekseerSystem.PlayEffect(effect, transform.position);
    // transformの回転を設定する。
    handle.SetRotation(transform.rotation);
}
```

### 特徴

PlayEffect()で再生した場合は自動で位置回転は変わりません。  
もし動かしたいときは手動で設定してやる必要があります。  

ヒットエフェクトや爆発エフェクトなど、シンプルに使いたいときに適しています。

## 設定ファイル

設定ファイルを作成すると、Effekseerの詳細な挙動を設定できます。

Assets -> Create -> Effekseer -> Effekseer Settings を選択します。

メニューからEffekseer Settingsを作成した場合、preload assetsに自動的に登録されます。

もしEffekseer Settingsが読み込まれない場合、Project Settings -> preload assets にEffekseer Settingsが含まれているか確認してください。

## Universal Render Pipeline

Effekseer は Universal Render Pipeline に対応しています。

* 1.5からアップグレードする場合、ScriptExternalディレクトリを削除してください。

現在使用しているScriptableRenderPipelineSettingsを確認するためにGraphics Settingsを見ます。

既に存在していたらそれを選択します。

存在しない場合、作成して選択します。

![](../img/URP/Create_Pipeline.png)

Pipelineで使用されているForwardRendererを選択します。

![](../img/URP/Pipeline.png)

使用されていない場合、作成してPipelineに設定し、選択します。

![](../img/URP/Create_ForwardRenderer.png)

![](../img/URP/ForwardRenderer.png)

先ほど選択した *ForwardRenderer Asset* の *Render Features* に *EffekseerRenderPassFeature* を追加します。

![](../img/URP/RenderPassFeature.png)

## High Definition Render Pipeline

Effekseer は High Definition Render Pipeline に対応しています。

* 1.5からアップグレードする場合、ScriptExternalディレクトリを削除してください。

カメラに *CustomPassVolume* コンポーネントを追加します。

![](../img/HDRP/CustomPassVolume.png)

*CustomPasses* に *EffekseerRendererHDRP* を追加します。

![](../img/HDRP/CustomPassVolumeSelect.png)

![](../img/HDRP/CustomPassVolumeAdd.png)

*Injection Point* を *Before Post Process* に変更します。

![](../img/HDRP/CustomPassVolumeInjectionPoint.png)

## PostProcessingStack (1.53以降)

EffekseerはPostProcessingStackのポストプロセスとしても描画できます。

* 1.5からアップグレードする場合、ScriptExternalディレクトリを削除してください。

PostProcessingをインストールし、Post-Process VolumeとPost-Process Layerを設定します。

![](../img/PostProcessingStack/pps_install.png)

EffekseerSettingsから、RenderAsPostProcessingStackをOnにします。

![](../img/PostProcessingStack/pps_settings.png)

Post-Process Volumeにeffectを追加します。BeforeStackとAfterStackがありますが、基本的にBeforeStackを選択します。
詳細は、PostProcessingStackのヘルプを読んでください。

![](../img/PostProcessingStack/pps_ppv.png)

エフェクトを有効にします。

![](../img/PostProcessingStack/pps_make_enable.png)

ポストプロセスとして描画されるため、CustomEffectSortingから描画順序を変更することができます。

![](../img/PostProcessingStack/pps_sorting.png)

## モバイル環境

EffekseerSettingsから歪みや深度を無効化すると高速化します。

## ネットワーク機能

ネットワーク経由でUnityで再生しているエフェクトを外部からアプリケーションの起動中に編集することができます。

![](../img/network.png)

Effekseer SettingsにEffekseerから接続するためのポートを指定します。DoStartNetworkAutomaticallyをOnにするか、EffekseerSystemのStartNetworkを実行します。
そうすると、Effekseerからエフェクトを編集できるようになります。他のコンピューターからエフェクトを編集するためにはファイヤーウォールの設定でポート開放する必要があります。

![](../img/network_ui.png)
