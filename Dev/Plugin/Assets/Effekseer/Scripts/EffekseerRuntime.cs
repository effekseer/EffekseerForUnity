using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer
{
	using Internal;

	public class EffekseerRuntime : MonoBehaviour
	{
		[SerializeField]
		private EffekseerSystem system;
		[SerializeField]
		private EffekseerSoundPlayer soundPlayer;

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
			soundPlayer = new EffekseerSoundPlayer();
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
	}
}