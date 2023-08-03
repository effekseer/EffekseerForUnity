using UnityEngine;

namespace Effekseer
{
	public class PlayingEffectProfile
	{
		public string Name;
		public int ParticleCount;
	}

	public class EffekseerRuntime : MonoBehaviour
	{
		[SerializeField]
		private EffekseerSystem system;
		[SerializeField]
		private Internal.EffekseerSoundPlayer soundPlayer;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void RuntimeInitializeOnLoad()
		{
			// Create singleton instance
			var go = new GameObject("Effekseer");
			go.AddComponent<EffekseerRuntime>();
			DontDestroyOnLoad(go);
		}

		void Awake()
		{
			if (system == null)
			{
				system = new EffekseerSystem();
			}
			system.InitPlugin();
			soundPlayer = new Internal.EffekseerSoundPlayer();
			soundPlayer.Init(gameObject);
		}

		void OnDestroy()
		{
			soundPlayer.Dispose();
			system.TermPlugin();
		}

		void OnEnable()
		{
			system.OnEnable();
			soundPlayer.OnEnable();

			foreach (var effectAsset in EffekseerEffectAsset.enabledAssets)
			{
				EffekseerEffectAsset target = effectAsset.Value.Target as EffekseerEffectAsset;

				if (target != null)
				{
					EffekseerSystem.Instance.LoadEffect(target);
				}
			}
		}

		void OnDisable()
		{
			soundPlayer.OnDisable();
			system.OnDisable();
		}

		void LateUpdate()
		{
			Plugin.UpdateNetwork();
			soundPlayer.Update();
			system.Update(Time.deltaTime, Time.unscaledDeltaTime);
		}

		public static PlayingEffectProfile[] GetPlayingEffectProfiles()
		{
			int[] handles = new int[1024];
			var count = Plugin.Effekseer_Manager_GetEffectHandles(handles, handles.Length);

			var profiles = new PlayingEffectProfile[count];

			for (int i = 0; i < count; i++)
			{
				profiles[i] = new PlayingEffectProfile();
				profiles[i].Name = System.Runtime.InteropServices.Marshal.PtrToStringUni(Plugin.Effekseer_Manager_GetName(handles[i]));
				profiles[i].ParticleCount = Plugin.EffekseerGetInstanceCount(handles[i]);
			}

			return profiles;
		}
	}
}