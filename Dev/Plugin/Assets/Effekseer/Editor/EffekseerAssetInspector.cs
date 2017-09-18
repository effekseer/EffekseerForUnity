using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TextAsset))]
public class EffekseerAssetInspector : Editor
{
	bool textureVisible = true;
	bool soundVisible = true;
	bool modelVisible = true;
	
	public override void OnInspectorGUI()
	{
		var asset = target as TextAsset;
		byte[] data = asset.bytes;
		if (data.Length < 4 || data[0] != 'S' || data[1] != 'K' || data[2] != 'F' || data[3] != 'E') {
			return;
		}

		string assetPath = AssetDatabase.GetAssetPath(asset);
		string assetDir = assetPath.Substring(0, assetPath.LastIndexOf('/'));
		
        GUI.enabled = true;
        EditorGUILayout.LabelField("[Effekseer Effect Asset]");

		int filepos = 4;

		// Get Format Version number
		int version = BitConverter.ToInt32(data, filepos);
		filepos += 4;
		
		// Effect resource paths
		List<string> texturePaths = new List<string>();
		List<string> soundPaths = new List<string>();
		List<string> modelPaths = new List<string>();

		// Get color texture paths
		{
			int colorTextureCount = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			for (int i = 0; i < colorTextureCount; i++) {
				texturePaths.Add(ReadString(data, ref filepos));
			}
		}
		
		if (version >= 9) {
			// Get normal texture paths
			int normalTextureCount = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			for (int i = 0; i < normalTextureCount; i++) {
				texturePaths.Add(ReadString(data, ref filepos));
			}

			// Get normal texture paths
			int distortionTextureCount = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			for (int i = 0; i < distortionTextureCount; i++) {
				texturePaths.Add(ReadString(data, ref filepos));
			}
		}
		
		if (version >= 1) {
			// Get sound paths
			int soundCount = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			for (int i = 0; i < soundCount; i++) {
				soundPaths.Add(ReadString(data, ref filepos));
			}
		}
		
		if (version >= 6) {
			// Get sound paths
			int modelCount = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			for (int i = 0; i < modelCount; i++) {
				modelPaths.Add(ReadString(data, ref filepos));
			}
		}
		
		// Display effect informations
		EditorGUILayout.LabelField("Data Size", data.Length.ToString() + " bytes");
		EditorGUILayout.LabelField("Format Version", version.ToString());
		
		// Display texture resources
		textureVisible = EditorGUILayout.Foldout(textureVisible, "Texture Resources (Count = " + texturePaths.Count + ")");
		if (textureVisible) {
			for (int i = 0; i < texturePaths.Count; i++) {
				EditorGUILayout.LabelField("[" + i + "] " + texturePaths[i]);
				Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetDir + "/" + texturePaths[i]);
				EditorGUILayout.ObjectField(texture, typeof(Texture2D), false, GUILayout.Width (64), GUILayout.Height (64));
			}
		}
		
		// Display sound resources
		soundVisible = EditorGUILayout.Foldout(soundVisible, "Sound Resources (Count = " + soundPaths.Count + ")");
		if (soundVisible) {
			for (int i = 0; i < soundPaths.Count; i++) {
				EditorGUILayout.LabelField("[" + i + "] " + soundPaths[i]);
				AudioClip audio = AssetDatabase.LoadAssetAtPath<AudioClip>(assetDir + "/" + soundPaths[i]);
				EditorGUILayout.ObjectField(audio, typeof(AudioClip), false);
			}
		}
		
		// Display model resources
		modelVisible = EditorGUILayout.Foldout(modelVisible, "Model Resources (Count = " + modelPaths.Count + ")");
		if (modelVisible) {
			for (int i = 0; i < modelPaths.Count; i++) {
				EditorGUILayout.LabelField("[" + i + "] " + modelPaths[i]);
				TextAsset model = AssetDatabase.LoadAssetAtPath<TextAsset>(assetDir + "/" + modelPaths[i] + ".bytes");
				EditorGUILayout.ObjectField(model, typeof(TextAsset), false);
			}
		}
	}

	private static string ReadString(byte[] data, ref int filepos)
	{
		int length = BitConverter.ToInt32(data, filepos);
		filepos += 4;
		string str = Encoding.Unicode.GetString(data, filepos, (length - 1) * 2);
		filepos += length * 2;
		return str;
	}
}
