using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor
{
	[CustomEditor(typeof(Effekseer.EffekseerEmitter))]
	public class EffekseerEmitterEditor : UnityEditor.Editor
	{
		//EffekseerEmitter emitter = null;

		void OnEnable()
		{
			//emitter = (EffekseerEmitter)target;
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			//GUILayout.Button("aaa");
		}
	}
}
#endif