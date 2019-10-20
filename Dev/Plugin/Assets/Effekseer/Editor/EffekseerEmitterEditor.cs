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
		private EffekseerEmitter emitter;
		private double lastTime;
		private bool systemInitialized;
		private bool loop;
		
		void OnEnable()
		{
			emitter = (EffekseerEmitter)target;
		}
		
		void OnDisable()
		{
			TermSystem();
		}

		void InitSystem()
		{
			if (EffekseerEditor.instance.inEditor) {
				EffekseerEditor.instance.InitSystem();
				EffekseerSystem.Instance.LoadEffect(emitter.effectAsset);
				lastTime = EditorApplication.timeSinceStartup;
				EditorApplication.update += Update;
			}
			systemInitialized = true;
		}
		
		void TermSystem()
		{
			if (EffekseerEditor.instance.inEditor) {
				EditorApplication.update -= Update;
				emitter.StopImmediate();
				EffekseerEditor.instance.TermSystem();
			}
			systemInitialized = false;
		}

		void Update()
		{
			double currentTime = EditorApplication.timeSinceStartup;
			float deltaTime = (float)(currentTime - lastTime);
			float deltaFrames = Utility.TimeToFrames(deltaTime);
			lastTime = currentTime;
			
			if (emitter.exists) {
				RepaintEffect();
			} else if (loop) {
				emitter.Play();
			}

			foreach (var handle in emitter.handles) {
				handle.UpdateHandle(deltaFrames);
			}
			emitter.Update();
		}

		void RepaintEffect()
		{
			SceneView.RepaintAll();
			var assembly = typeof(EditorWindow).Assembly;
			var type = assembly.GetType("UnityEditor.GameView");
			var gameview = EditorWindow.GetWindow(type, false, null, false);
			if (gameview != null)
			{
				gameview.Repaint();
			}
		}
		
		void OnSceneGUI()
		{
			if (emitter == null)
				return;

			if (!emitter.isActiveAndEnabled)
				return;

			SceneView sceneView = SceneView.currentDrawingSceneView;

			Handles.BeginGUI();

			float screenWidth  = sceneView.camera.pixelWidth;
			float screenHeight = sceneView.camera.pixelHeight;
			
			float width = 160;
			float height = 80;
			var boxRect  = new Rect(screenWidth - width - 30, screenHeight - height - 45, width + 20, height + 40);
			var areaRect = new Rect(screenWidth - width - 20, screenHeight - height - 20, width, height);

			GUI.Box(boxRect, "Effekseer Emitter");
			GUILayout.BeginArea(areaRect);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Play")) {
				if (systemInitialized == false) {
					InitSystem();
				}

				// avoid a bug playing effect sometimes causes craches after window size is changed.
				// Unity, Effekseer, or driver bug
				Effekseer.EffekseerSystem.Instance.renderer.CleanUp();
				emitter.Play();
			}
			if (GUILayout.Button("Stop")) {
				emitter.StopImmediate();

				// just in case
				Effekseer.EffekseerSystem.Instance.renderer.CleanUp();
				RepaintEffect();
			}
			GUILayout.EndHorizontal();
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Loop", GUILayout.Width(50));
			loop = GUILayout.Toggle(loop, "");
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Paused", GUILayout.Width(50));
			emitter.paused = GUILayout.Toggle(emitter.paused, "");
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			GUILayout.Label("Speed", GUILayout.Width(50));
			emitter.speed = GUILayout.HorizontalSlider(emitter.speed, 0.0f, 2.0f);
			GUILayout.EndHorizontal();
			
			GUILayout.EndArea();

			Handles.EndGUI();
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
		}
	}
}
#endif