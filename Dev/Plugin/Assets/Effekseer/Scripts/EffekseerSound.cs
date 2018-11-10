using System;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer.Internal
{
	[Serializable]
	public class EffekseerSoundResource : IDisposable
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public AudioClip clip;
		
		private GCHandle gch;

		public EffekseerSoundResource() {
			this.gch = GCHandle.Alloc(this);
		}

		public void Dispose() {
			this.gch.Free();
		}
		
		public IntPtr ToIntPtr() {
			return GCHandle.ToIntPtr(this.gch);
		}

		public static EffekseerSoundResource FromIntPtr(IntPtr ptr) {
			return GCHandle.FromIntPtr(ptr).Target as EffekseerSoundResource;
		}

#if UNITY_EDITOR
		public static EffekseerSoundResource LoadAsset(string dirPath, string resPath) {
			AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(dirPath + "/" + resPath);

			EffekseerSoundResource res = new EffekseerSoundResource();
			res.path = resPath;
			res.clip = clip;
			return res;
		}
		public static bool InspectorField(EffekseerSoundResource res) {
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.clip, typeof(AudioClip), false) as AudioClip;
			if (result != res.clip) {
				res.clip = result;
				return true;
			}
			return false;
		}
#endif
	}
	
	public class EffekseerSoundInstance : MonoBehaviour
	{
		new private AudioSource audio;
		public string AudioTag;

		void Awake() {
			audio = gameObject.AddComponent<AudioSource>();
			audio.playOnAwake = false;
		}
		void Update() {
			if (audio.clip && !audio.isPlaying) {
				audio.clip = null;
			}
		}
		public void Play(string tag, EffekseerSoundResource resource, 
			float volume, float pan, float pitch, 
			bool mode3D, float x, float y, float z, float distance)
		{
			this.AudioTag = tag;
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
		public void Stop() {
			audio.Stop();
		}
		public void Pause(bool paused) {
			if (paused) audio.Pause();
			else audio.UnPause();
		}
		public bool CheckPlaying() {
			return audio.isPlaying;
		}
	}
}