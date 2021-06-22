using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer.Internal
{
	[Serializable]
	public class EffekseerSoundResource
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public AudioClip clip;

		public static EffekseerSoundResource FromIntPtr(IntPtr ptr)
		{
			return GCHandle.FromIntPtr(ptr).Target as EffekseerSoundResource;
		}

#if UNITY_EDITOR
		public static EffekseerSoundResource LoadAsset(string dirPath, string resPath)
		{
			AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(EffekseerEffectAsset.NormalizeAssetPath(dirPath + "/" + resPath));

			EffekseerSoundResource res = new EffekseerSoundResource();
			res.path = resPath;
			res.clip = clip;
			return res;
		}
		public static bool InspectorField(EffekseerSoundResource res)
		{
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.clip, typeof(AudioClip), false) as AudioClip;
			if (result != res.clip)
			{
				res.clip = result;
				return true;
			}
			return false;
		}
#endif
	}

	[Serializable]
	public class EffekseerSoundPlayer : IDisposable
	{
		// Singleton instance
		public static EffekseerSoundPlayer Instance { get; private set; }
		public static bool IsValid { get { return Instance != null; } }

		// Pooled instances
		private List<EffekseerSoundInstance> childInstances = new List<EffekseerSoundInstance>();

		List<Action> events = new List<Action>();

		public EffekseerSoundPlayer()
		{
			Instance = this;
		}

		~EffekseerSoundPlayer()
		{
			Dispose();
		}

		public void Dispose()
		{
			Instance = null;
			GC.SuppressFinalize(this);
		}

		internal void Init(GameObject rootObj)
		{
			if (Application.isPlaying)
			{
				var settings = EffekseerSettings.Instance;
				// サウンドインスタンスを作る
				for (int i = 0; i < settings.soundInstances; i++)
				{
					GameObject go = new GameObject();
					go.name = "SoundInstance #" + i;
					go.transform.parent = rootObj.transform;
					childInstances.Add(go.AddComponent<EffekseerSoundInstance>());
				}
			}
		}

		internal void OnEnable()
		{
			Instance = this;
			Plugin.EffekseerSetSoundPlayerEvent(
				SoundPlayerPlay,
				SoundPlayerStopTag,
				SoundPlayerPauseTag,
				SoundPlayerCheckPlayingTag,
				SoundPlayerStopAll);
		}

		internal void Update()
		{
			lock (Instance)
			{
				foreach (var e in events)
				{
					e.Invoke();
				}
				events.Clear();
			}
		}

		internal void OnDisable()
		{
			lock (Instance)
			{
				foreach (var e in events)
				{
					e.Invoke();
				}
				events.Clear();
				Plugin.EffekseerSetSoundPlayerEvent(null, null, null, null, null);
			}

			Instance = null;
		}

		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerPlay))]
		private static void SoundPlayerPlay(IntPtr tag,
				IntPtr data, float volume, float pan, float pitch,
				bool mode3D, float x, float y, float z, float distance)
		{

			lock (Instance)
			{
				Instance.events.Add(() => { Instance.PlaySound(tag, data, volume, pan, pitch, mode3D, x, y, z, distance); });
			}
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerStopTag))]
		private static void SoundPlayerStopTag(IntPtr tag)
		{
			lock (Instance)
			{
				Instance.events.Add(() => { Instance.StopSound(tag); });
			}
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerPauseTag))]
		private static void SoundPlayerPauseTag(IntPtr tag, bool pause)
		{
			lock (Instance)
			{
				Instance.events.Add(() => { Instance.PauseSound(tag, pause); });
			}
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerCheckPlayingTag))]
		private static bool SoundPlayerCheckPlayingTag(IntPtr tag)
		{
			lock (Instance)
			{
				return Instance.CheckSound(tag);
			}
		}
		[AOT.MonoPInvokeCallback(typeof(Plugin.EffekseerSoundPlayerStopAll))]
		private static void SoundPlayerStopAll()
		{
			lock (Instance)
			{
				Instance.events.Add(() => { Instance.StopAllSounds(); });
			}
		}

		private void PlaySound(IntPtr tag,
			IntPtr data, float volume, float pan, float pitch,
			bool mode3D, float x, float y, float z, float distance)
		{
			if (data == IntPtr.Zero)
			{
				return;
			}
			var resource = EffekseerSoundResource.FromIntPtr(EffekseerSystem.GetCachedSound(data));
			if (resource == null)
			{
				return;
			}

			EffekseerSoundInstance optimalInstance = null;
			foreach (var instance in childInstances)
			{
				if (!instance.CheckPlaying())
				{
					optimalInstance = instance;
					break;
				}
				else if (optimalInstance == null || instance.lastPlayTime < optimalInstance.lastPlayTime)
				{
					optimalInstance = instance;
				}
			}
			if (optimalInstance != null)
			{
				optimalInstance.Stop();
				optimalInstance.Play(tag.ToString(), resource, volume, pan, pitch, mode3D, x, y, z, distance);
			}
		}
		private void StopSound(IntPtr tag)
		{
			foreach (var sound in childInstances)
			{
				if (sound.audioTag == tag.ToString())
				{
					sound.Stop();
				}
			}
		}
		private void PauseSound(IntPtr tag, bool paused)
		{
			foreach (var sound in childInstances)
			{
				if (sound.audioTag == tag.ToString())
				{
					sound.Pause(paused);
				}
			}
		}
		private bool CheckSound(IntPtr tag)
		{
			bool playing = false;
			foreach (var sound in childInstances)
			{
				if (sound.audioTag == tag.ToString())
				{
					playing |= sound.CheckPlaying();
				}
			}
			return playing;
		}
		private void StopAllSounds()
		{
			foreach (var sound in childInstances)
			{
				sound.Stop();
			}
		}
	}

	public class EffekseerSoundInstance : MonoBehaviour
	{
		new private AudioSource audio;
		public string audioTag;
		public float lastPlayTime;

		/// <summary>
		/// make thread safe
		/// </summary>
		bool isPlaying;

		void Awake()
		{
			audio = gameObject.AddComponent<AudioSource>();
			audio.playOnAwake = false;
		}
		void Update()
		{
			isPlaying = audio.isPlaying;

			if (audio.clip && !audio.isPlaying)
			{
				audio.clip = null;
			}
		}
		public void Play(string tag, EffekseerSoundResource resource,
			float volume, float pan, float pitch,
			bool mode3D, float x, float y, float z, float distance)
		{
			this.audioTag = tag;
			this.lastPlayTime = Time.time;
			transform.position = new Vector3(x, y, z);
			audio.spatialBlend = (mode3D) ? 1.0f : 0.0f;
			audio.volume = volume;
			audio.pitch = Mathf.Pow(2.0f, pitch);
			audio.panStereo = pan;
			audio.minDistance = distance;
			audio.maxDistance = distance * 2;
			audio.clip = resource.clip;
			audio.Play();
		}
		public void Stop()
		{
			audio.Stop();
		}
		public void Pause(bool paused)
		{
			if (paused) audio.Pause();
			else audio.UnPause();
		}
		public bool CheckPlaying()
		{
			return isPlaying;
		}
	}
}