# How to use

## Sample Project {#example_program}

There is a sample project using the Effekseer plugin in the following places.

- GameEngine/Unity/SampleProject.zip

![](../img/unity_example.png)

## About resource files {#resource_files}

Place the output effect (*.efk), texture, sound in Unity project.  
When importing the *.efk file, EffectAsset is generated in addition of *.efk file.

![](../img/unity_resource.png)

## Play by Emitter {#emitter_playback}

### Introduction

Add EffekseerEmitter to GameObject.  
In that case will play the effect linked to GameObject.

![](../img/unity_emitter.png)

### Properties

- Effect Asset: Specifies the effect asset which is imported
- Play On Start: Plays on Start() when it is checked.
- IsLooping: When playback ends, it will automatically request playback.

![](../img/unity_emitter.png)

### Preview

A controller for preview is shown in Scene View when EffekseerEmitter is specified. Effects can be previewed in Game View without playing.

![](../img/unity_emitter_component_scene_view.png)

### Note

It is suitable for effects that follow the installed effects and characters.

## Play by Script {#direct_playback}

### Introduction

Using EffekseerSystem.PlayEffect(), you can play effects from scripts.

The sample code is as follows.

```cs
void Start()
{
    // get an effect
    EffekseerEffectAsset effect = Resources.Load<EffekseerEffectAsset> ("Laser01");
    // Plays effect in transform.position
    EffekseerHandle handle = EffekseerSystem.PlayEffect(effect, transform.position);
    // Sets the rotation of the effect
    handle.SetRotation(transform.rotation);
}
```

### Note

When playing with PlayEffect(), the position rotation does not change automatically.
If you want to move it you need to set it manually.

Suitable for simple use, such as hit effects and explosion effects.

## Network
You can edit the playing effect in Unity via the network from the outside when application is running.

You specify the port to be connected from Effekseer fo Effekseer Setting. Make DoStartNetworkAutomatically On or execute StartNetwork in EffekseerSystem. Then you can edit the effect from Effekseer. In order to edit the effect from another computer, it is necessary to open the port with the setting of the firewall. 

![](../img/network_ui.png)
