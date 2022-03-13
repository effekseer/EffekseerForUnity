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
		EffekseerEffectAsset loadedEffect;

		/// <summary>
		/// Play an effect with delay because an effect is not shown while initializing a system 
		/// </summary>
		float? delayPlay;

		float requiredDelayTime = 0.5f;

		private double lastTime;
		private bool systemInitialized;
		private bool loop;

		void OnEnable()
		{
			emitter = (EffekseerEmitter)target;

#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui += SceneView_duringSceneGui;
#endif
		}

		void OnDisable()
		{
			TermSystem();

#if UNITY_2019_1_OR_NEWER
			SceneView.duringSceneGui -= SceneView_duringSceneGui;
#endif
		}

		void ReloadEffectIfRequired()
		{
			if (loadedEffect != emitter.effectAsset)
			{
				EffekseerSystem.Instance.LoadEffect(emitter.effectAsset);
				loadedEffect = emitter.effectAsset;
				requiredDelayTime = 0.2f;
			}
			else
			{
				requiredDelayTime = 0.1f;
			}
		}

		void PlayEffect()
		{
			ReloadEffectIfRequired();
			emitter.Play();
		}
		void InitSystem()
		{
			if (EffekseerEditor.instance.inEditor && emitter.effectAsset != null)
			{
				EffekseerEditor.instance.InitSystem();
				EffekseerSystem.Instance.LoadEffect(emitter.effectAsset);
				lastTime = EditorApplication.timeSinceStartup;
				EditorApplication.update += Update;
			}
			systemInitialized = true;
		}

		void TermSystem()
		{
			if (EffekseerEditor.instance.inEditor)
			{
				EditorApplication.update -= Update;
				emitter.StopImmediate();
				EffekseerEditor.instance.TermSystem();
				loadedEffect = null;
			}
			systemInitialized = false;
		}

		void Update()
		{
			double currentTime = EditorApplication.timeSinceStartup;
			float deltaTime = (float)(currentTime - lastTime);
			float deltaFrames = Utility.TimeToFrames(deltaTime);
			lastTime = currentTime;

			if (emitter.exists)
			{
				RepaintEffect();
			}

			if ((loop && !emitter.exists) || delayPlay.HasValue && delayPlay.Value < 0.0f)
			{
				EffekseerSystem.Instance.ResetRestTime();
				PlayEffect();
				delayPlay = null;
			}

			foreach (var handle in emitter.handles)
			{
				handle.UpdateHandle(deltaFrames);
			}

			if (EffekseerSystem.Instance != null)
			{
				EffekseerSystem.Instance.UpdateTime(deltaFrames);
				EffekseerSystem.Instance.ApplyLightingToNative();
			}

			emitter.UpdateSelf();

			if (delayPlay.HasValue)
			{
				delayPlay = delayPlay.Value - deltaTime;
			}
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

#if UNITY_2019_1_OR_NEWER
		private void SceneView_duringSceneGui(SceneView obj)
		{
			CallSceneGUI();
		}
#else
		void OnSceneGUI()
		{
			CallSceneGUI();
		}
#endif

		void CallSceneGUI()
		{
			if (emitter == null)
				return;

			if (!emitter.isActiveAndEnabled)
				return;

			SceneView sceneView = SceneView.currentDrawingSceneView;

			Handles.BeginGUI();

			float screenWidth = sceneView.position.size.x;
			float screenHeight = sceneView.position.size.y;

			float width = 160;
			float height = 120;

			var boxRect = new Rect(screenWidth - width - 30, screenHeight - height - 45, width + 20, height + 40);
			var areaRect = new Rect(screenWidth - width - 20, screenHeight - height - 20, width, height);

			GUI.Box(boxRect, "Effekseer Emitter");
			GUILayout.BeginArea(areaRect);

			GUILayout.BeginHorizontal();
			if (GUILayout.Button("Play"))
			{
				if (systemInitialized == false)
				{
					InitSystem();
					loadedEffect = null;
					Effekseer.EffekseerSystem.Instance.renderer.disableCullingMask = true;
				}

				// avoid a bug playing effect sometimes causes craches after window size is changed.
				// Unity, Effekseer, or driver bug
				Effekseer.EffekseerSystem.Instance.renderer.CleanUp();

				// Load an effect actually
				ReloadEffectIfRequired();
				delayPlay = requiredDelayTime;
			}
			if (GUILayout.Button("Stop"))
			{
				emitter.StopImmediate();

				// just in case
				if (Effekseer.EffekseerSystem.Instance != null)
				{
					Effekseer.EffekseerSystem.Instance.renderer.CleanUp();
				}
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
			emitter.speed = GUILayout.HorizontalSlider(emitter.speed, 0.0f, 2.0f, GUILayout.Width(50));
			emitter.speed = EditorGUILayout.FloatField(emitter.speed, GUILayout.Width(50));
			emitter.speed = Mathf.Clamp(emitter.speed, 0, 2);
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("Trigger", GUILayout.Width(50));
			if (GUILayout.Button("0"))
			{
				emitter.SendTrigger(0);
			}
			if (GUILayout.Button("1"))
			{
				emitter.SendTrigger(1);
			}
			if (GUILayout.Button("2"))
			{
				emitter.SendTrigger(2);
			}
			if (GUILayout.Button("3"))
			{
				emitter.SendTrigger(3);
			}
			GUILayout.EndHorizontal();


			GUILayout.BeginHorizontal();
			GUILayout.Label("Instance", GUILayout.Width(80));
			GUILayout.Label(emitter.instanceCount.ToString());
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.Label("RestInstance", GUILayout.Width(80));
			GUILayout.Label(EffekseerSystem.restInstanceCount.ToString());
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