# How to use

## Sample Project {#example_program}

There is a sample project using the Effekseer plugin in the following places.

- GameEngine/Unity/SampleProject.zip

![](../img/unity_example.png)

## About resource files {#resource_files}

Place the output effect (*.efk), texture, sound in Resources/Effekseer/.  
When importing the *.efk file, it is renamed *.bytes.  
If it does not work, please try Reimport.  

![](../img/unity_resource.png)

## Play by Emitter {#emitter_playback}

### Introduction

Add EffekseerEmitter to GameObject.  
In that case will play the effect linked to GameObject.

![](../img/unity_emitter.png)

### Properties

- Effect Name: Specifies the effect name.<br>(The effect name is a character string excluding the extension from the file name (*.efk))
- Play On Start: Plays on Start() when it is checked.
- Loop: When playback ends, it will automatically request playback.

### Notes

It is suitable for effects that follow the installed effects and characters.

## Play by Script {#direct_playback}

### Introduction

Using EffekseerSystem.PlayEffect(), you can play effects from scripts.

The sample code is as follows.

```cs
void Start()
{
    // Plays effect in transform.position
    EffekseerHandle handle = EffekseerSystem.PlayEffect("Laser01", transform.position);
    // Sets the rotation of the effect
    handle.SetRotation(transform.rotation);
}
```

### Note

When playing with PlayEffect(), the position rotation does not change automatically.
If you want to move it you need to set it manually.

Suitable for simple use, such as hit effects and explosion effects.

## Preload from Resources {#preload}

### Automatic loading

The resource file necessary for the effect is loaded in the following time.

- EffekseerEmitter.Start()
- EffekseerSystem.PlayEffect()

In both cases, only resources of the specified effect are loaded.  
The loaded effect will be retained and will not be loaded the next time it is played.

Automatic loading is easy, but depending on the moment of loading, the game may fall.

### Manual preloading

By LoadEffect() beforehand, you can prevent frame dropping.

```cs
void Start()
{
    // Loads effect "Laser01.efk"
    EffekseerSystem.LoadEffect("Laser01");
}
```

Loaded effects are automatically released at EffekseerSystem's OnDestroy().  
It is also possible to explicitly release an unnecessary effect with ReleaseEffect().

```cs
void OnDestroy()
{
    // Releases effect "Laser01.efk"
    EffekseerSystem.ReleaseEffect("Laser01");
}
```

## Preload from AssetBundle {#assetbundle}

You can load effect resources from asset bundles.  
If you use asset bundle, you can not auto load.

```cs
IEnumerator Load() {
    string url = "file:///" + Application.streamingAssetsPath + "/effects";
    WWW www = new WWW(url);
    yield return www;
    var assetBundle = www.assetBundle;
    EffekseerSystem.LoadEffect("Laser01", assetBundle);
}
```

Please ReleaseEffect() like loading from Resources.

Please do not release AssetBundle before all effects are released.

## Network
You can edit the playing effect in Unity via the network from the outside when application is running.

You specify the port to be connected from Effekseer fo Effekseer Setting. Make DoStartNetworkAutomatically On or execute StartNetwork in EffekseerSystem. Then you can edit the effect from Effekseer. In order to edit the effect from another computer, it is necessary to open the port with the setting of the firewall. 

![](../img/network_ui.png)
