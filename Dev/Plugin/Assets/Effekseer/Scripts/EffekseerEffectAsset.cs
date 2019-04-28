using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Effekseer.Internal
{
	[Serializable]
	public class EffekseerTextureResource
	{
		[SerializeField]
		public string path;
		[SerializeField]
		public Texture2D texture;
			
#if UNITY_EDITOR
		public static EffekseerTextureResource LoadAsset(string dirPath, string resPath) {
			Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(dirPath + "/" + resPath);

			var res = new EffekseerTextureResource();
			res.path = resPath;
			res.texture = texture;
			return res;
		}
		public static bool InspectorField(EffekseerTextureResource res) {
			EditorGUILayout.LabelField(res.path);
			var result = EditorGUILayout.ObjectField(res.texture, typeof(Texture2D), false) as Texture2D;
			if (result != res.texture) {
				res.texture = result;
				return true;
			}
			return false;
		}
#endif
	};
}

namespace Effekseer
{
	using Internal;

	public class EffekseerEffectAsset : ScriptableObject
	{
		[SerializeField]
		public byte[] efkBytes;

		[SerializeField]
		public EffekseerTextureResource[] textureResources;		
		[SerializeField]
		public EffekseerSoundResource[] soundResources;
		[SerializeField]
		public EffekseerModelResource[] modelResources;

        [SerializeField]
        public float Scale = 1.0f;

        internal static HashSet<EffekseerEffectAsset> enabledAssets = new HashSet<EffekseerEffectAsset>();
		
		void OnEnable()
		{
			if (EffekseerSystem.IsValid) {
				EffekseerSystem.Instance.LoadEffect(this);
			}
			enabledAssets.Add(this);
		}

		void OnDisable()
		{
			enabledAssets.Remove(this);
			if (EffekseerSystem.IsValid) {
				EffekseerSystem.Instance.ReleaseEffect(this);
			}
		}

		public EffekseerTextureResource FindTexture(string path)
		{
			int index = Array.FindIndex(textureResources, (r) => (path == r.path));
			return (index >= 0) ? textureResources[index] : null;
		}
		
		public EffekseerSoundResource FindSound(string path)
		{
			int index = Array.FindIndex(soundResources, (r) => (path == r.path));
			return (index >= 0) ? soundResources[index] : null;
		}
		
		public EffekseerModelResource FindModel(string path)
		{
			int index = Array.FindIndex(modelResources, (r) => (path == r.path));
			return (index >= 0) ? modelResources[index] : null;
		}

#if UNITY_EDITOR
        public static void CreateAsset(string path)
        {
            byte[] data = File.ReadAllBytes(path);
            CreateAsset(path, data);
        }

        public static void CreateAsset(string path, byte[] data)
		{
			if (data.Length < 4 || data[0] != 'S' || data[1] != 'K' || data[2] != 'F' || data[3] != 'E') {
				return;
			}

            float defaultScale = 1.0f;

            string assetPath = Path.ChangeExtension(path, ".asset");
            var baseAsset = AssetDatabase.LoadAssetAtPath<EffekseerEffectAsset>(assetPath);
            if(baseAsset != null)
            {
                defaultScale = baseAsset.Scale;
            }


            int filepos = 4;

			// Get Format Version number
			int version = BitConverter.ToInt32(data, filepos);
			filepos += 4;
		
			// Effect resource paths
			List<string> texturePathList = new List<string>();
			List<string> soundPathList = new List<string>();
			List<string> modelPathList = new List<string>();

			// Get color texture paths
			{
				int colorTextureCount = BitConverter.ToInt32(data, filepos);
				filepos += 4;
				for (int i = 0; i < colorTextureCount; i++) {
					texturePathList.Add(ReadString(data, ref filepos));
				}
			}
		
			if (version >= 9) {
				// Get normal texture paths
				int normalTextureCount = BitConverter.ToInt32(data, filepos);
				filepos += 4;
				for (int i = 0; i < normalTextureCount; i++) {
					texturePathList.Add(ReadString(data, ref filepos));
				}

				// Get normal texture paths
				int distortionTextureCount = BitConverter.ToInt32(data, filepos);
				filepos += 4;
				for (int i = 0; i < distortionTextureCount; i++) {
					texturePathList.Add(ReadString(data, ref filepos));
				}
			}

			if (version >= 1) {
				// Get sound paths
				int soundCount = BitConverter.ToInt32(data, filepos);
				filepos += 4;
				for (int i = 0; i < soundCount; i++) {
					soundPathList.Add(ReadString(data, ref filepos));
				}
			}
		
			if (version >= 6) {
				// Get sound paths
				int modelCount = BitConverter.ToInt32(data, filepos);
				filepos += 4;
				for (int i = 0; i < modelCount; i++) {
					modelPathList.Add(ReadString(data, ref filepos));
				}
			}

			string assetDir = assetPath.Substring(0, assetPath.LastIndexOf('/'));
			
			var asset = CreateInstance<EffekseerEffectAsset>();
			asset.efkBytes = data;
			
			asset.textureResources = new EffekseerTextureResource[texturePathList.Count];
			for (int i = 0; i < texturePathList.Count; i++) {
				asset.textureResources[i] = EffekseerTextureResource.LoadAsset(assetDir, texturePathList[i]);
			}
			
			asset.soundResources = new EffekseerSoundResource[soundPathList.Count];
			for (int i = 0; i < soundPathList.Count; i++) {
				asset.soundResources[i] = EffekseerSoundResource.LoadAsset(assetDir, soundPathList[i]);
			}
			
			asset.modelResources = new EffekseerModelResource[modelPathList.Count];
			for (int i = 0; i < modelPathList.Count; i++) {
				asset.modelResources[i] = EffekseerModelResource.LoadAsset(assetDir, modelPathList[i]);
			}

            asset.Scale = defaultScale;

			AssetDatabase.CreateAsset(asset, assetPath);
			AssetDatabase.Refresh();
		}

		private static string ReadString(byte[] data, ref int filepos)
		{
			int length = BitConverter.ToInt32(data, filepos);
			filepos += 4;
			string str = Encoding.Unicode.GetString(data, filepos, (length - 1) * 2);
			filepos += length * 2;
			return str;
		}
#endif
	}
}
