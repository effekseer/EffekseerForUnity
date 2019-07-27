using UnityEngine;
using System.Collections.Generic;

namespace Effekseer
{
	/// <summary xml:lang="en">
	/// A emitter of the Effekseer effect
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトの発生源
	/// </summary>
	[AddComponentMenu("Effekseer/Effekseer Emitter")]
	public class EffekseerEmitter : MonoBehaviour
	{
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
			h.SetRotation(transform.rotation);
			h.SetScale(transform.localScale);
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
			foreach (var handle in handles) {
				handle.Stop();
			}
			handles.Clear();
		}

		/// <summary>
		/// Don't touch it!!
		/// </summary>
		public void StopImmediate()
		{
			foreach (var handle in handles) {
				handle.Stop();
				handle.UpdateHandle(1);
			}
			handles.Clear();
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
			foreach (var handle in handles) {
				handle.StopRoot();
			}
			handles.Clear();
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
			foreach (var handle in handles) {
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
			foreach (var handle in handles) {
				handle.SetTargetLocation(targetLocation);
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
			set {
				_paused = value;
				foreach (var handle in handles) {
					Plugin.EffekseerSetPaused(handle.m_handle, value);
				}
			}
			get {
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
			set {
				_shown = value;
				foreach (var handle in handles) {
					Plugin.EffekseerSetShown(handle.m_handle, value);
				}
			}
			get {
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
			set {
				_speed = value;
				foreach (var handle in handles) {
					Plugin.EffekseerSetSpeed(handle.m_handle, value);
				}
			}
			get {
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
			get {
				bool res = false;
				foreach (var handle in handles) {
					res |= handle.exists;
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
			if (effectAsset && playOnStart) {
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
		public void Update()
		{
			for (int i = 0; i < handles.Count; ) {
				var handle = handles[i];
				if (handle.exists) {
					handle.SetLocation(transform.position);
					handle.SetRotation(transform.rotation);
					handle.SetScale(transform.localScale);
					i++;
				} else if(isLooping && handles.Count == 1)
				{
					handles.RemoveAt(i);
					var newHandle = Play();

					// avoid infinite loop
					if (!newHandle.exists)
					{
						break;
					}
				}
				else {
					handles.RemoveAt(i);
				}
			}
		}
		
		#endregion
	}
}