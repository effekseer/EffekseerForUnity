# 使い方

## サンプルプロジェクト

下記の場所にEffekseerプラグインを使用したサンプルシーンがあります。

- Example/EfkBasic
- Example/EfkTimeline

![](../img/unity_example.png)

## リソースファイルについて

Unityのプロジェクトにエフェクトプロジェクトファイル(*.efkproj)、出力済エフェクト(*.efk)やテクスチャ、サウンドを配置します。  
\*.efk,efkprojファイルをインポートすると、.efk,efkprojファイルのほかにEffectAssetが生成されます。 

![](../img/unity_resource.png)

.efk,efkprojファイルは削除しても問題ありません。
また、現在はカスタムパッケージ作成の時に.efk,efkprojファイルを含めてはいけません。

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

## Light Weight Render Pipeline

Effekseer は Light Weight Render Pipeline に対応しています。.
LWRPはUnityでは標準に含まれていないため、 *ScriptsExternal/EffekseerRendererLWRP.cs* からコメントアウトを外してください。

![](../img/LWRP_Code.png)

Effekseerのエフェクトを表示するために *Custom Forward Render* を使用します。

*Assets -> Create -> Rendering -> Lightweight Render Pipeline -> Forward Render* から *Forward Render Asset* を作成します。

![](../img/LWRP_ForwardRenderer1.png)

![](../img/LWRP_ForwardRenderer2.png)

現在使用している *Pipeline Asset* の *Renderer Type* を *Custom* に変更します。 *Data* に先ほど作成した *Forward Render Asset* を設定します。

![](../img/LWRP_Custom1.png)

![](../img/LWRP_Custom2.png)

先ほど作成した *Forward Render Asset* の *Render Features* に *EffekseerRenderer* を追加します。

![](../img/LWRP_RenderFeatures1.png)

![](../img/LWRP_RenderFeatures2.png)

古いLWRPでは、* Effekseer/Effekseer RendererLWRP * コンポーネントをカメラがあるGameObjectに設定してください。

## High Definition Render Pipeline

対応予定です。

## モバイル環境

EffekseerSettingsから歪みを無効化すると高速化します。

## ネットワーク機能

ネットワーク経由でUnityで再生しているエフェクトを外部からアプリケーションの起動中に編集することができます。

![](../img/network.png)

Effekseer SettingsにEffekseerから接続するためのポートを指定します。DoStartNetworkAutomaticallyをOnにするか、EffekseerSystemのStartNetworkを実行します。
そうすると、Effekseerからエフェクトを編集できるようになります。他のコンピューターからエフェクトを編集するためにはファイヤーウォールの設定でポート開放する必要があります。

![](../img/network_ui.png)
