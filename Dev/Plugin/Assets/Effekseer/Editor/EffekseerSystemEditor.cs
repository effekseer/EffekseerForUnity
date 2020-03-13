using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#elif UNITY_2018_3_OR_NEWER
using UnityEngine.Experimental.UIElements;
#endif


namespace Effekseer.Editor
{
#if UNITY_2018_3_OR_NEWER
	public class EffekseerSettingProvider : SettingsProvider
	{
		public EffekseerSettingProvider(string path, SettingsScope scope)
			: base(path, scope) { }

		public override void OnActivate
		(
			string searchContext,
			VisualElement rootElement
		)
		{
		}

		public override void OnDeactivate()
		{
		}

		public override void OnTitleBarGUI()
		{
		}

		public override void OnGUI(string searchContext)
		{
			if (GUILayout.Button("Edit Settings"))
			{
				EffekseerSettings.EditOrCreateAsset();
			}
		}

		public override void OnFooterBarGUI()
		{
			EditorGUILayout.LabelField("Effekseer");
		}

		[SettingsProvider]
		private static SettingsProvider Create()
		{
			var path = "Project/Effekseer";
			var provider = new EffekseerSettingProvider(path, SettingsScope.Project);

			provider.keywords = new[] { "Effekseer" };

			return provider;
		}
	}
#else
	[CustomEditor(typeof(EffekseerSystem))]
	public class EffekseerSystemEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			if (GUILayout.Button("Edit Settings")) {
				EffekseerSettings.EditOrCreateAsset();
			}
		}
	}
#endif
}
#endif