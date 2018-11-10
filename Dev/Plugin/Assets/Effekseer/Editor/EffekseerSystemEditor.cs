using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
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
}
#endif