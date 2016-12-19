using UnityEngine;
using System;
using System.Collections;

/// <summary xml:lang="en">
/// A emitter of the Effekseer effect
/// </summary>
/// <summary xml:lang="ja">
/// エフェクトの発生源
/// </summary>
public class EffekseerEmitter : MonoBehaviour
{
	/// <summary xml:lang="en">
	/// Effect name
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクト名
	/// </summary>
	public string effectName;

	/// <summary xml:lang="en">
	/// Whether it does play the effect on Start()
	/// </summary>
	/// <summary xml:lang="ja">
	/// Start()時に再生開始するかどうか
	/// </summary>
	public bool playOnStart = false;

	/// <summary xml:lang="ja">
	/// Whether it does loop playback.
	/// </summary>
	/// <summary xml:lang="ja">
	/// ループ再生するかどうか
	/// </summary>
	public bool loop = false;

	/// <summary xml:lang="en">
	/// The last played handle.
	/// </summary>
	/// <summary xml:lang="ja">
	/// 最後にPlayされたハンドル
	/// </summary>
	private EffekseerHandle? handle;
	
	/// <summary xml:lang="en">
	/// Plays the effect.
	/// <param name="name">Effect name</param>
	/// </summary>
	/// <summary xml:lang="ja">
	/// エフェクトを再生
	/// <param name="name">エフェクト名</param>
	/// </summary>
	public void Play(string name)
	{
		effectName = name;
		Play();
	}
	
	/// <summary xml:lang="en">
	/// Plays the effect that has been set.
	/// </summary>
	/// <summary xml:lang="ja">
	/// 設定されているエフェクトを再生
	/// </summary>
	public void Play()
	{
		var h = EffekseerSystem.PlayEffect(effectName, transform.position);
		h.SetRotation(transform.rotation);
		h.SetScale(transform.localScale);
		handle = h;
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
		if (handle.HasValue) {
			handle.Value.Stop();
			handle = null;
		}
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
		if (handle.HasValue) {
			handle.Value.StopRoot();
			handle = null;
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
		if (handle.HasValue) {
			handle.Value.SetTargetLocation(targetLocation);
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
			if (handle.HasValue) {
				var h = handle.Value;
				h.paused = value;
			}
		}
		get {
			return handle.HasValue && handle.Value.paused;
		}
	}
	
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
			if (handle.HasValue) {
				var h = handle.Value;
				h.shown = value;
			}
		}
		get {
			return handle.HasValue && handle.Value.shown;
		}
	}
	
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
			return handle.HasValue && handle.Value.exists;
		}
	}
	
	#region Internal Implimentation

	void Start()
	{
		if (!String.IsNullOrEmpty(effectName)) {
			EffekseerSystem.LoadEffect(effectName);
			if (playOnStart) {
				Play();
			}
		}
	}
	
	void OnDestroy()
	{
		Stop();
	}
	
	void Update()
	{
		if (handle.HasValue) {
			var h = handle.Value;
			if (h.exists) {
				h.SetLocation(transform.position);
				h.SetRotation(transform.rotation);
				h.SetScale(transform.localScale);
			} else if (loop) {
				Play();
			} else {
				Stop();
			}
		}
	}

	#endregion
}
