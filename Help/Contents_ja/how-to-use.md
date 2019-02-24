# 使い方

## サンプルプロジェクト {#example_program}

下記の場所にEffekseerプラグインを使用したサンプルプロジェクトがあります。

- GameEngine/Unity/SampleProject.zip

![](../img/unity_example.png)

## リソースファイルについて {#resource_files}

<font color="red">1.10から変更になりました。</font>

Resources/Effekseer/ 以下に出力済エフェクト(*.efk)やテクスチャ、サウンドを配置します。  
\*.efkファイルをインポートすると、*.bytesにリネームされます。  
上手くいかないときは、Reimportを試してください。  

![](../img/unity_resource.png)

## エミッタを使って再生する方法 {#emitter_playback}

### 準備

エフェクトのエミッタコンポーネントをGameObjectにAddすることで、  
GameObjectに連動したエフェクトの再生を行うことができます。  

Plugin/Effekseer/EffekseerEmitter.csをGameObjectに追加します。

![](../img/unity_emitter.png)

### インスペクタのプロパティ

- Effect Name: エフェクト名を指定します。  
    （エフェクト名はエフェクトファイル名(*.efk)から拡張子を除いた文字列です）
- Play On Start: チェックを入れると、シーン開始時(Start()のタイミング)に自動的に再生します。
- Loop: 再生終了したら自動的に再生をリクエストします。

### 特徴

設置するエフェクトやキャラクターに追従するようなエフェクトに適しています。

## スクリプトから直接再生する方法 {#direct_playback}

### スクリプト

EffekseerSystem.PlayEffect()を使うことで、スクリプトからエフェクトを再生することができます。  

以下サンプルコードです。

```cs
void Start()
{
    // transformの位置でエフェクトを再生する
    EffekseerHandle handle = EffekseerSystem.PlayEffect("Laser01", transform.position);
    // transformの回転を設定する。
    handle.SetRotation(transform.rotation);
}
```

### 特徴

PlayEffect()で再生した場合は自動で位置回転は変わりません。  
もし動かしたいときは手動で設定してやる必要があります。  

ヒットエフェクトや爆発エフェクトなど、シンプルに使いたいときに適しています。

## 事前にリソースをロードする {#preload}

### 自動でロードされるタイミング

エフェクトに必要なリソースファイルがロードされるのは以下のタイミングです。

- EffekseerEmitter.Start()
- EffekseerSystem.PlayEffect()

いずれも指定されたエフェクトのリソースのみがロードされます。  
ロードされたエフェクトは保持され、次回の再生タイミングではロードされません。  

自動ロードはお手軽ですが、ロードするタイミングによってはゲームがフレーム落ちする可能性があります。  

### 事前に明示的にロードする

事前にLoadEffect()することで、ロードの負荷でフレーム落ちすることを防ぐことができます。

```cs
void Start()
{
    // エフェクト"Laser01"をロードする。
    EffekseerSystem.LoadEffect("Laser01");
}
```

ロードされたエフェクトはEffekseerSystemのDestroy時に自動解放されますが、  
不要になったエフェクトをReleaseEffect()で明示的に解放することもできます。  

```cs
void OnDestroy()
{
    // エフェクト"Laser01"を解放する
    EffekseerSystem.ReleaseEffect("Laser01");
}
```

## アセットバンドルからロードする {#assetbundle}

アセットバンドルからエフェクトリソースをロードすることができます。   
なお、アセットバンドルを使う場合は自動ロードすることはできません。   

```cs
IEnumerator Load() {
    string url = "file:///" + Application.streamingAssetsPath + "/effects";
    WWW www = new WWW(url);
    yield return www;
    var assetBundle = www.assetBundle;
    EffekseerSystem.LoadEffect("Laser01", assetBundle);
}
```

通常のロードと同じようにReleaseEffect()してください。

全てのエフェクトのリリースが終わる前にAssetBundleのリリースを行わないでください。

## ネットワーク機能

ネットワーク経由でUnityで再生しているエフェクトを外部からアプリケーションの起動中に編集することができます。

Effekseer SettingsにEffekseerから接続するためのポートを指定します。DoStartNetworkAutomaticallyをOnにするか、EffekseerSystemのStartNetworkを実行します。
そうすると、Effekseerからエフェクトを編集できるようになります。他のコンピューターからエフェクトを編集するためにはファイヤーウォールの設定でポート開放する必要があります。

![](../img/network_ui.png)