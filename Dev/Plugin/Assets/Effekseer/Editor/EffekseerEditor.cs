using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
	public class EffekseerEditor : ScriptableSingleton<EffekseerEditor>
	{
		[InitializeOnLoadMethod]
		public static void InitializeOnLoad()
		{
			instance.InitEditor();
		}

		public bool inEditor { get; private set; }
		private bool initialized = false;
		private EffekseerSystem system = null;

		public void InitEditor()
		{
			if (initialized) {
				return;
			}
			initialized = true;
			inEditor = !Application.isPlaying;
		}

		public void InitSystem()
		{
			if (!inEditor) {
				return;
			}
			if (system == null) {
				system = new EffekseerSystem();
			}
			if (!system.enabled) {
				system.InitPlugin();
				system.OnEnable();
			}
		}

		public void TermSystem()
		{
			if (!inEditor) {
				return;
			}
			if (system != null && system.enabled) {
				system.OnDisable();
				system.TermPlugin();
			}
		}

		void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlaymodeStateChanged;
		}
		
		void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlaymodeStateChanged;
		}

		public void OnPlaymodeStateChanged(PlayModeStateChange playState)
		{
			switch (playState) {
			case PlayModeStateChange.EnteredEditMode:
				inEditor = true;
				break;
			case PlayModeStateChange.ExitingEditMode:
				TermSystem();
				inEditor = false;
				break;
			}
		}
	}
}
#endif
