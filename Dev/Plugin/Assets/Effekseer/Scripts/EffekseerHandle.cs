using UnityEngine;

namespace Effekseer
{
	public enum EffekseerTimeScale
	{
		Scale,
		Unscale,
	}

	/// <summary xml:lang="ja">
	/// A instance handle of played effect
	/// </summary>
	/// <summary xml:lang="ja">
	/// 再生したエフェクトのインスタンスハンドル
	/// </summary>
	public struct EffekseerHandle
	{
		internal int m_handle;

		EffekseerTimeScale timeScale;

		public EffekseerHandle(int handle = -1)
		{
			m_handle = handle;
			layer_ = 0;
			timeScale = EffekseerTimeScale.Scale;
			ApplyTimeScale();
		}

		/// <summary>
		/// Update a single effect (almost for Editor)
		/// </summary>
		public void UpdateHandle(float deltaFrame)
		{
			Plugin.EffekseerSetTimeScaleByGroup(1, 1);
			Plugin.EffekseerSetTimeScaleByGroup(2, 1);

			Plugin.EffekseerUpdateHandle(m_handle, deltaFrame);
		}

		/// <summary>
		/// Update to move the specified frame (almost for Editor)
		/// </summary>
		/// <param name="frame"></param>
		public void UpdateHandleToMoveToFrame(float frame)
		{
			Plugin.EffekseerUpdateHandleToMoveToFrame(m_handle, frame);
		}

		/// <summary xml:lang="en">
		/// Stops the played effect.
		/// All nodes will be destroyed.
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトを停止する
		/// 全てのエフェクトが瞬時に消える
		/// </summary>
		public void Stop()
		{
			Plugin.EffekseerStopEffect(m_handle);
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
			Plugin.EffekseerStopRoot(m_handle);
		}

		/// <summary xml:lang="en">
		/// Sets the effect location
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの位置を設定
		/// </summary>
		/// <param name="location">位置</param>
		public void SetLocation(Vector3 location)
		{
			Plugin.EffekseerSetLocation(m_handle, location.x, location.y, location.z);
		}

		/// <summary xml:lang="en">
		/// Sets the effect rotation
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの回転を設定
		/// </summary>
		/// <param name="rotation">回転</param>
		public void SetRotation(Quaternion rotation)
		{
			Vector3 axis;
			float angle;
			rotation.ToAngleAxis(out angle, out axis);

			if (float.IsNaN(axis.x) || float.IsInfinity(axis.x))
			{
				Plugin.EffekseerSetRotation(m_handle, 0.0f, -1.0f, 0.0f, 360.0f * Mathf.Deg2Rad);
			}
			else
			{
				Plugin.EffekseerSetRotation(m_handle, axis.x, axis.y, axis.z, angle * Mathf.Deg2Rad);
			}
		}

		/// <summary xml:lang="en">
		/// Sets the effect scale
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトの拡縮を設定
		/// </summary>
		/// <param name="scale">拡縮</param>
		public void SetScale(Vector3 scale)
		{
			Plugin.EffekseerSetScale(m_handle, scale.x, scale.y, scale.z);
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
			Plugin.EffekseerSetAllColor(m_handle, (byte)(color.r * 255), (byte)(color.g * 255), (byte)(color.b * 255), (byte)(color.a * 255));
		}

		/// <summary xml:lang="en">
		/// Sets the effect target location
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトのターゲット位置を設定
		/// </summary>
		/// <param name="targetLocation">ターゲット位置</param>
		public void SetTargetLocation(Vector3 targetLocation)
		{
			Plugin.EffekseerSetTargetLocation(m_handle, targetLocation.x, targetLocation.y, targetLocation.z);
		}

		/// <summary xml:lang="en">
		/// get a dynamic parameter, which changes effect parameters dynamically while playing
		/// </summary>
		/// <summary xml:lang="ja">
		/// 再生中にエフェクトのパラメーターを変更する動的パラメーターを取得する。
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public float GetDynamicInput(int index)
		{
			return Plugin.EffekseerGetDynamicInput(m_handle, index);
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
			Plugin.EffekseerSetDynamicInput(m_handle, index, value);
		}

		int layer_;

		/// <summary>
		/// layer to show specified effect
		/// </summary>
		public int layer
		{
			get
			{
				return layer_;
			}

			set
			{
				layer_ = value;
				Plugin.EffekseerSetLayer(m_handle, value);
			}
		}

		/// <summary xml:lang="en">
		/// Pausing the effect
		/// <para>true:  It will update on Update()</para>
		/// <para>false: It will not update on Update()</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// ポーズ設定
		/// <para>true:  停止中。Updateで更新しない</para>
		/// <para>false: 再生中。Updateで更新する</para>
		/// </summary>
		public bool paused
		{
			set
			{
				Plugin.EffekseerSetPaused(m_handle, value);
			}
			get
			{
				return Plugin.EffekseerGetPaused(m_handle);
			}
		}

		/// <summary xml:lang="en">
		/// Showing the effect
		/// <para>true:  It will be rendering.</para>
		/// <para>false: It will not be rendering.</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// 表示設定
		/// <para>true:  表示ON。Drawで描画する</para>
		/// <para>false: 表示OFF。Drawで描画しない</para>
		/// </summary>
		public bool shown
		{
			set
			{
				Plugin.EffekseerSetShown(m_handle, value);
			}
			get
			{
				return Plugin.EffekseerGetShown(m_handle);
			}
		}

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
				Plugin.EffekseerSetSpeed(m_handle, value);
			}
			get
			{
				return Plugin.EffekseerGetSpeed(m_handle);
			}
		}

		/// <summary xml:lang="ja">
		/// Whether the effect instance is enabled<br/>
		/// <para>true:  enabled</para>
		/// <para>false: disabled</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// インスタンスハンドルが有効かどうか<br/>
		/// <para>true:  有効</para>
		/// <para>false: 無効</para>
		/// </summary>
		public bool enabled
		{
			get
			{
				return m_handle >= 0;
			}
		}

		/// <summary xml:lang="en">
		/// Existing state
		/// <para>true:  It's existed.</para>
		/// <para>false: It isn't existed or stopped.</para>
		/// </summary>
		/// <summary xml:lang="ja">
		/// エフェクトのインスタンスが存在しているかどうか
		/// <para>true:  存在している</para>
		/// <para>false: 再生終了で破棄。もしくはStopで停止された</para>
		/// </summary>
		public bool exists
		{
			get
			{
				return Plugin.EffekseerExists(m_handle);
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
				return Plugin.EffekseerGetInstanceCount(m_handle);
			}
		}

		public EffekseerTimeScale TimeScale
		{
			get
			{
				return timeScale;
			}
			set
			{
				if (timeScale == value)
					return;

				timeScale = value;

				ApplyTimeScale();
			}
		}

		void ApplyTimeScale()
		{
			if (timeScale == EffekseerTimeScale.Scale)
			{
				Plugin.EffekseerSetGroupMask(m_handle, 1);
			}
			else if (timeScale == EffekseerTimeScale.Unscale)
			{
				Plugin.EffekseerSetGroupMask(m_handle, 2);
			}
		}
	}
}