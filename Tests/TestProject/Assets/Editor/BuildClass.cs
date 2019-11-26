using UnityEditor;
using UnityEngine;

public class BuildClass
{
#if UNITY_EDITOR
    public static void Build()
    {
        // ビルド対象シーンリスト
        string[] sceneList = {
            "./Assets/Scenes/SampleScene.unity",
            
        };


        // 実行
        string errorMessage = BuildPipeline.BuildPlayer(
                sceneList,                          //!< ビルド対象シーンリスト
                "./Build/Android.apk",   //!< 出力先
                BuildTarget.Android,      //!< ビルド対象プラットフォーム
                BuildOptions.Development            //!< ビルドオプション
        ).ToString();


        // 結果出力
        if (!string.IsNullOrEmpty(errorMessage))
            Debug.LogError("[Error!] " + errorMessage);
        else
            Debug.Log("[Success!]");
    }
#endif
}
