using UnityEngine;
using System.Collections.Generic;

namespace Effekseer
{
	/// <summary xml:lang="en">
	/// Which scale is A scale of effect based on. 
	/// </summary>
	/// <summary xml:lang="ja">
	/// どのスケールをエフェクトのスケールの元にするか
	/// </summary>
	public enum EffekseerEmitterScale
	{
		Local,
		Global,
	}

	/// <summary xml:lang="en">
	/// Timing of the update
	/// </summary>
	/// <summary xml:lang="ja">
	/// 更新のタイミング
	/// </summary>
	public enum EffekseerEmitterTimingOfUpdate
	{
		Update,
		FixedUpdate,
	}


	/// <summary xml:lang="en">
	/// A emitter of the Effekseer effect
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの発生源
	/// </summary>
	[AddComponentMenu("Effekseer/Effekseer Emitter")]
	public class EffekseerEmitter : MonoBehaviour
	{
		float cachedMagnification = 0.0f;

		/// <summary xml:lang="en">
		/// Timing of the update
		/// </summary>
		/// <summary xml:lang="ja">
		/// 更新のタイミング
		/// </summary>
		public EffekseerEmitterTimingOfUpdate TimingOfUpdate = EffekseerEmitterTimingOfUpdate.Update;

		/// <summary xml:lang="en">
		/// Which scale is A scale of effect based on. 
		/// </summary>
		/// <summary xml:lang="ja">
		/// どのスケールをエフェクトのスケールの元にするか
		/// </summary>
		public EffekseerEmitterScale EmitterScale = EffekseerEmitterScale.Local;

		/// <summary xml:lang="en">
		/// TimeScale where the effect is played
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトが再生されるタイムスケール
		/// </summary>
		public EffekseerTimeScale TimeScale = EffekseerTimeScale.Scale;

		/// <summary xml:lang="en">
		/// Effect name
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクト名
		/// </summary>
		//public string effectName;

		/// <summary xml:lang="en">
		/// Effect name
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクト名
		/// </summary>
		public EffekseerEffectAsset effectAsset;

		/// <summary xml:lang="en">
		/// Whether it does play the effect on Start()
		/// </summary>
		/// <summary xml:lang="ja">
		/// Start()時に再生開始するかどうか
		/// </summary>
		public bool playOnStart = false;

		/// <summary xml:lang="ja">
		/// Whether it does loop playback when all effects are stopped
		/// </summary>
		/// <summary xml:lang="ja">
		/// 全てのエフェクトが停止した後に、ループ再生するかどうか
		/// </summary>
		public bool isLooping = false;

		/// <summary xml:lang="en">
		/// The last played handle.
		/// Don't touch it!!
		/// </summary>
		/// <summary xml:lang="ja">
		/// 最後にPlayされたハンドル
		/// Effekseer開発者以外は触ってはいけない
		/// </summary>
		public List<EffekseerHandle> handles = new List<EffekseerHandle>();

		/// <summary xml:lang="en">
		/// Plays the effect.
		/// <param name="name">Effect name</param>
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトを再生
		/// <param name="name">エフェクト名</param>
		/// </summary>
		public EffekseerHandle Play(EffekseerEffectAsset effectAsset)
		{
			var h = EffekseerSystem.PlayEffect(effectAsset, transform.position);

			// must run after loading
			cachedMagnification = effectAsset.Magnification;

			ApplyRotationAndScale(ref h);

			h.layer = gameObject.layer;
			if (speed != 1.0f) h.speed = speed;
			if (paused) h.paused = paused;
			if (shown) h.shown = shown;
			handles.Add(h);
			return h;
		}

		/// <summary xml:lang="en">
		/// Plays the effect that has been set.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 設定されているエフェクトを再生
		/// </summary>
		public EffekseerHandle Play()
		{
			return Play(effectAsset);
		}

		/// <summary xml:lang="en">
		/// Stops the played effect.
		/// All nodes will be destroyed.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中のエフェクトを停止
		/// 全てのノードが即座に消える
		/// </summary>
		public void Stop()
		{
			foreach (var handle in handles)
			{
				handle.Stop();
			}
			handles.Clear();
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void StopImmediate()
		{
			foreach (var handle in handles)
			{
				handle.Stop();
				handle.UpdateHandle(1);
			}
			handles.Clear();
			Plugin.EffekseerResetTime();
		}

		/// <summary xml:lang="en">
		/// Stops the root node of the played effect.
		/// The root node will be destroyed. Then children also will be destroyed by their lifetime.
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中のエフェクトのルートノードだけを停止
		/// ルートノードを削除したことで子ノード生成が停止され寿命で徐々に消える
		/// </summary>
		public void StopRoot()
		{
			foreach (var handle in handles)
			{
				handle.StopRoot();
			}
		}

		/// <summary xml:lang="en">
		/// Specify the color of overall effect.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクト全体の色を指定する。
		/// </summary>
		/// <param name="color">Color</param>
		public void SetAllColor(Color color)
		{
			foreach (var handle in handles)
			{
				handle.SetAllColor(color);
			}
		}

		/// <summary xml:lang="en">
		/// Sets the target location of the played effects.
		/// <param name="targetLocation" xml:lang="en">Target location</param>
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中のエフェクトのターゲット位置を設定
		/// <param name="targetLocation" xml:lang="ja">ターゲット位置</param>
		/// </summary>
		public void SetTargetLocation(Vector3 targetLocation)
		{
			foreach (var handle in handles)
			{
				handle.SetTargetLocation(targetLocation);
			}
		}

		/// <summary xml:lang="en">
		/// get first effect's dynamic parameter, which changes effect parameters dynamically while playing
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中にエフェクトのパラメーターを変更する最初のエフェクトの動的パラメーターを取得する。
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public float GetDynamicInput(int index)
		{
			foreach (var handle in handles)
			{
				return handle.GetDynamicInput(index);
			}

			return 0.0f;
		}

		/// <summary xml:lang="en">
		/// specfiy a dynamic parameter, which changes effect parameters dynamically while playing
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中にエフェクトのパラメーターを変更する動的パラメーターを設定する。
		/// </summary>
		/// <param name="index"></param>
		/// <param name="value"></param>
		public void SetDynamicInput(int index, float value)
		{
			foreach (var handle in handles)
			{
				handle.SetDynamicInput(index, value);
			}
		}

		/// <summary xml:lang="en">
		/// specify a dynamic parameters x,y,z with a world position converting into local position considering effect magnification.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの拡大率を考慮しつつ、ローカル座標に変換しつつ、ワールド座標で動的パラメーターx,y,zを設定する。
		/// </summary>
		/// <param name="worldPos"></param>
		public void SetDynamicInputWithWorldPosition(ref Vector3 worldPos)
		{
			var localPos = transform.InverseTransformPoint(worldPos);
			SetDynamicInputWithLocalPosition(ref localPos);
		}

		/// <summary xml:lang="en">
		/// specify a dynamic parameters x,y,z with a local position considering effect magnification.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの拡大率を考慮しつつ、ローカル座標座標で動的パラメーターx,y,zを設定する。
		/// </summary>
		/// <param name="worldPos"></param>
		public void SetDynamicInputWithLocalPosition(ref Vector3 localPos)
		{
			SetDynamicInput(0, localPos.x / cachedMagnification);
			SetDynamicInput(1, localPos.y / cachedMagnification);

			if (EffekseerSettings.Instance.isRightEffekseerHandledCoordinateSystem)
			{
				SetDynamicInput(2, localPos.z / cachedMagnification);
			}
			else
			{
				SetDynamicInput(2, -localPos.z / cachedMagnification);
			}
		}

		/// <summary xml:lang="en">
		/// specify a dynamic parameters x with distance to a world position converting into local position considering effect magnification.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの拡大率を考慮しつつ、ローカル座標に変換しつつ、ワールド座標への距離で動的パラメーターxを設定する。
		/// </summary>
		/// <param name="worldPos"></param>
		public void SetDynamicInputWithWorldDistance(ref Vector3 worldPos)
		{
			var localPos = transform.InverseTransformPoint(worldPos);
			SetDynamicInputWithLocalDistance(ref localPos);
		}

		/// <summary xml:lang="en">
		/// specify a dynamic parameters x with distance to a world position converting into local position considering effect magnification.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの拡大率を考慮しつつ、ローカル座標への距離で動的パラメーターxを設定する。
		/// </summary>
		/// <param name="localPos"></param>
		public void SetDynamicInputWithLocalDistance(ref Vector3 localPos)
		{
			SetDynamicInput(0, localPos.magnitude / cachedMagnification);
		}

		/// <summary xml:lang="en">
		/// Send a trigger signal
		/// </summary>
		/// <summary xml:lang="ja">
		/// トリガーの信号を送信する。
		/// </summary>
		/// <param name="index"></param>
		public void SendTrigger(int index)
		{
			foreach (var handle in handles)
			{
				Plugin.EffekseerSendTrigger(handle.m_handle, index);
			}
		}

		/// <summary xml:lang="en">
		/// Pausing the effect
		/// <para>true:  It will update on Update()</para>
		/// <para>false: It will not update on Update()</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// ポーズ設定
		/// <para>true:  Updateで更新しない</para>
		/// <para>false: Updateで更新する</para>
		/// </summary>
		public bool paused
		{
			set
			{
				_paused = value;
				foreach (var handle in handles)
				{
					Plugin.EffekseerSetPaused(handle.m_handle, value);
				}
			}
			get
			{
				return _paused;
			}
		}
		private bool _paused = false;

		/// <summary xml:lang="en">
		/// Showing the effect
		/// <para>true:  It will be rendering.</para>
		/// <para>false: It will not be rendering.</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// 表示設定
		/// <para>true:  描画する</para>
		/// <para>false: 描画しない</para>
		/// </summary>
		public bool shown
		{
			set
			{
				_shown = value;
				foreach (var handle in handles)
				{
					Plugin.EffekseerSetShown(handle.m_handle, value);
				}
			}
			get
			{
				return _shown;
			}
		}
		private bool _shown = true;

		/// <summary xml:lang="en">
		/// Playback speed
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生速度
		/// </summary>
		public float speed
		{
			set
			{
				_speed = value;
				foreach (var handle in handles)
				{
					Plugin.EffekseerSetSpeed(handle.m_handle, value);
				}
			}
			get
			{
				return _speed;
			}
		}
		private float _speed = 1.0f;

		/// <summary xml:lang="en">
		/// Existing state
		/// <para>true:  It's existed.</para>
		/// <para>false: It isn't existed or stopped.</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中のエフェクトが存在しているか
		/// <para>true:  存在している</para>
		/// <para>false: 再生終了で破棄。もしくはStopで停止された</para>
		/// </summary>
		public bool exists
		{
			get
			{
				bool res = false;
				foreach (var handle in handles)
				{
					res |= handle.exists;
				}
				return res;
			}
		}

		/// <summary xml:lang="en">
		/// Get the number of instance which is used in this effect including root
		/// </summary>
		/// <summary xml:lang="ja">
		/// Rootを含んだエフェクトに使用されているインスタンス数を取得する。
		/// </summary>
		public int instanceCount
		{
			get
			{
				int res = 0;
				foreach (var handle in handles)
				{
					res += handle.instanceCount;
				}
				return res;
			}
		}

		#region Internal Implimentation

		void OnEnable()
		{
			foreach (var handle in handles)
			{
				Plugin.EffekseerSetPaused(handle.m_handle, _paused);
			}

			foreach (var handle in handles)
			{
				Plugin.EffekseerSetShown(handle.m_handle, _shown);
			}
		}

		void OnDisable()
		{
			foreach (var handle in handles)
			{
				Plugin.EffekseerSetPaused(handle.m_handle, true);
			}

			foreach (var handle in handles)
			{
				Plugin.EffekseerSetShown(handle.m_handle, false);
			}
		}

		void Start()
		{
			if (effectAsset && playOnStart)
			{
				Play();
			}
		}

		void OnDestroy()
		{
			Stop();
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void UpdateSelf()
		{
			for (int i = 0; i < handles.Count;)
			{
				var handle = handles[i];
				if (handle.exists)
				{
					handle.SetLocation(transform.position);

					ApplyRotationAndScale(ref handle);

					i++;
				}
				else if (isLooping && handles.Count == 1)
				{
					handles.RemoveAt(i);
					var newHandle = Play();

					// avoid infinite loop
					if (!newHandle.exists)
					{
						break;
					}
				}
				else
				{
					handles.RemoveAt(i);
				}
			}
		}

		public void Update()
		{
			if (TimingOfUpdate == EffekseerEmitterTimingOfUpdate.Update)
			{
				UpdateSelf();
			}
		}

		public void FixedUpdate()
		{
			if (TimingOfUpdate == EffekseerEmitterTimingOfUpdate.FixedUpdate)
			{
				UpdateSelf();
			}
		}

		#endregion

		void ApplyRotationAndScale(ref EffekseerHandle handle)
		{
			handle.SetRotation(transform.rotation);

			if (EmitterScale == EffekseerEmitterScale.Local)
			{
				handle.SetScale(transform.localScale);
			}
			else if (EmitterScale == EffekseerEmitterScale.Global)
			{
				handle.SetScale(transform.lossyScale);
			}

			handle.TimeScale = TimeScale;
		}
	}
}