using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Copy and edit from EffekseerEditor

#if UNITY_EDITOR
using UnityEditor;

namespace Effekseer.Editor.Utils
{
	public enum Language
	{
		Japanese,
		English,
	}

	public enum CompiledMaterialPlatformType : int
	{
		DirectX9 = 0,
		// DirectX10 = 1,
		DirectX11 = 2,
		DirectX12 = 3,
		OpenGL = 10,
		Metal = 20,
		Vulkan = 30,
		PS4 = 40,
		Switch = 50,
		XBoxOne = 60,
		PS5 = 70,
	}

	public enum TextureType : int
	{
		Color,
		Value,
	}

	public enum CompiledMaterialInformationErrorCode
	{
		OK,
		TooOldFormat,
		TooNewFormat,
		NotFound,
		FailedToOpen,
		InvalidFormat,
	}

	public enum MaterialVersion : int
	{
		Version0 = 0,
		Version15 = 3,
		Version16 = 1610,
		Version17Alpha2 = 1700,
		Version17Alpha4 = 1703,
		Version17 = 1710,
	}

	public enum CompiledMaterialVersion : int
	{
		Version0 = 0,
		Version15 = 1,
		Version16 = 1610,
		Version162 = 1612,
	}

	public enum MaterialRequiredFunctionType : int
	{
		Gradient = 0,
		Noise = 1,
		Light = 2,
	}

	public class Gradient
	{
		public unsafe struct ColorMarker
		{
			public float Position;
			public float ColorR;
			public float ColorG;
			public float ColorB;
			public float Intensity;
		}

		public struct AlphaMarker
		{
			public float Position;
			public float Alpha;
		}

		public class State : ICloneable
		{
			public ColorMarker[] ColorMarkers;
			public AlphaMarker[] AlphaMarkers;

			public unsafe byte[] ToBinary()
			{
				List<byte[]> data = new List<byte[]>();
				data.Add(BitConverter.GetBytes(ColorMarkers.Length));

				for (int i = 0; i < ColorMarkers.Length; i++)
				{
					data.Add(BitConverter.GetBytes(ColorMarkers[i].Position));
					data.Add(BitConverter.GetBytes(ColorMarkers[i].ColorR));
					data.Add(BitConverter.GetBytes(ColorMarkers[i].ColorG));
					data.Add(BitConverter.GetBytes(ColorMarkers[i].ColorB));
					data.Add(BitConverter.GetBytes(ColorMarkers[i].Intensity));
				}

				data.Add(BitConverter.GetBytes(AlphaMarkers.Length));

				for (int i = 0; i < AlphaMarkers.Length; i++)
				{
					data.Add(BitConverter.GetBytes(AlphaMarkers[i].Position));
					data.Add(BitConverter.GetBytes(AlphaMarkers[i].Alpha));
				}

				return data.SelectMany(_ => _).ToArray();
			}

			public object Clone()
			{
				var state = new State();

				state.ColorMarkers = (ColorMarker[])ColorMarkers.Clone();
				state.AlphaMarkers = (AlphaMarker[])AlphaMarkers.Clone();

				return state;
			}

			public unsafe override bool Equals(object obj)
			{
				var o = (State)obj;
				if (o == null)
				{
					return false;
				}

				if (ColorMarkers.Count() != o.ColorMarkers.Count() || AlphaMarkers.Count() != o.AlphaMarkers.Count())
				{
					return false;
				}

				for (int i = 0; i < ColorMarkers.Count(); i++)
				{
					if (ColorMarkers[i].ColorR != o.ColorMarkers[i].ColorR ||
						ColorMarkers[i].ColorG != o.ColorMarkers[i].ColorG ||
						ColorMarkers[i].ColorB != o.ColorMarkers[i].ColorB ||
						ColorMarkers[i].Intensity != o.ColorMarkers[i].Intensity ||
						ColorMarkers[i].Position != o.ColorMarkers[i].Position)
					{
						return false;
					}
				}

				for (int i = 0; i < AlphaMarkers.Count(); i++)
				{
					if (
						AlphaMarkers[i].Alpha != o.AlphaMarkers[i].Alpha ||
						AlphaMarkers[i].Position != o.AlphaMarkers[i].Position)
					{
						return false;
					}
				}

				return true;
			}

			public override int GetHashCode()
			{
				if (ColorMarkers == null || AlphaMarkers == null)
				{
					return 0;
				}

				return ColorMarkers.GetHashCode() + AlphaMarkers.GetHashCode();
			}
		}

		State _value = null;

		public State Value
		{
			get;
		}

		public State DefaultValue
		{
			get;
			set;
		}

		public unsafe byte[] ToBinary()
		{
			return _value.ToBinary();
		}

		static Gradient()
		{
		}

		public unsafe static State CreateDefault()
		{
			var value = new State();
			value.ColorMarkers = new ColorMarker[2];
			value.ColorMarkers[0].Position = 0;
			value.ColorMarkers[0].Intensity = 1;
			value.ColorMarkers[0].ColorR = 1.0f;
			value.ColorMarkers[0].ColorG = 1.0f;
			value.ColorMarkers[0].ColorB = 1.0f;

			value.ColorMarkers[1].Position = 1;
			value.ColorMarkers[1].Intensity = 1;
			value.ColorMarkers[1].ColorR = 1.0f;
			value.ColorMarkers[1].ColorG = 1.0f;
			value.ColorMarkers[1].ColorB = 1.0f;

			value.AlphaMarkers = new AlphaMarker[2];
			value.AlphaMarkers[0].Position = 0.0f;
			value.AlphaMarkers[0].Alpha = 1.0f;
			value.AlphaMarkers[1].Position = 1.0f;
			value.AlphaMarkers[1].Alpha = 1.0f;
			return value;
		}

		public unsafe Gradient()
		{
			_value = CreateDefault();
			DefaultValue = CreateDefault();
		}

		public State GetValue()
		{
			return _value;
		}
	}

	class BinaryReader
	{
		byte[] buffer = null;
		int offset = 0;

		public BinaryReader(byte[] buffer)
		{
			this.buffer = buffer;
			this.offset = 0;
		}

		public void Get(ref int value)
		{
			value = BitConverter.ToInt32(buffer, offset);
			offset += 4;
		}

		public void Get(ref UInt16 value)
		{
			value = BitConverter.ToUInt16(buffer, offset);
			offset += 2;
		}

		public void Get(ref Int16 value)
		{
			value = BitConverter.ToInt16(buffer, offset);
			offset += 2;
		}

		public void Get(ref Byte value)
		{
			value = buffer[offset];
			offset += 1;
		}

		public void Get(ref float value)
		{
			value = BitConverter.ToSingle(buffer, offset);
			offset += 4;
		}

		public void Get(ref bool value)
		{
			value = BitConverter.ToInt32(buffer, offset) > 0;
			offset += 4;
		}

		public void Get(ref string value, Encoding encoding, bool zeroEnd = true, int bufLenSize = 4)
		{
			int length = 0;

			if (bufLenSize == 4)
			{
				int length4 = 0;
				Get(ref length4);
				length = length4;
			}
			else if (bufLenSize == 2)
			{
				UInt16 length2 = 0;
				Get(ref length2);
				length = length2;
			}
			else if (bufLenSize == 1)
			{
				Byte length1 = 0;
				Get(ref length1);
				length = length1;
			}

			int readLength = length;

			if (zeroEnd)
			{
				if (encoding == Encoding.Unicode)
				{
					readLength -= 2;
				}
				else if (encoding == Encoding.UTF8)
				{
					readLength -= 1;
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (encoding == Encoding.Unicode)
			{
				readLength *= 2;
			}

			value = encoding.GetString(buffer, offset, readLength);
			offset += length;
		}
	}

	class BinaryWriter
	{
		List<byte[]> buffers = new List<byte[]>();

		public BinaryWriter()
		{
		}

		public byte[] GetBinary()
		{
			return buffers.SelectMany(_ => _).ToArray();
		}

		public void Push(int value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}

		public void Push(UInt32 value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}
		public void Push(Int16 value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}

		public void Push(UInt16 value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}

		public void Push(UInt64 value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}

		public void Push(bool value)
		{
			buffers.Add(BitConverter.GetBytes(value ? 1 : 0));
		}

		public void Push(byte[] buffer)
		{
			Push(buffer.Count());
			buffers.Add((byte[])buffer.Clone());
		}

		public void Push(float value)
		{
			buffers.Add(BitConverter.GetBytes(value));
		}

		public void Push(string value, Encoding encoding, bool zeroEnd = true, int bufLenSize = 4)
		{
			var strBuf = encoding.GetBytes(value);
			var length = strBuf.Count();

			if (zeroEnd)
			{
				if (encoding == Encoding.Unicode)
				{
					length += 2;
				}
				else if (encoding == Encoding.UTF8)
				{
					length += 1;
				}
				else
				{
					throw new NotImplementedException();
				}
			}

			if (encoding == Encoding.Unicode)
			{
				length /= 2;
			}

			if (bufLenSize == 4)
			{
				buffers.Add(BitConverter.GetBytes(length));
			}
			else if (bufLenSize == 2)
			{
				buffers.Add(BitConverter.GetBytes((UInt16)length));
			}
			else if (bufLenSize == 1)
			{
				buffers.Add(new[] { (Byte)length });
			}

			buffers.Add(strBuf);

			if (zeroEnd)
			{
				if (encoding == Encoding.Unicode)
				{
					buffers.Add(new byte[] { 0, 0 });
				}
				else if (encoding == Encoding.UTF8)
				{
					buffers.Add(new byte[] { 0 });
				}
				else
				{
					throw new NotImplementedException();
				}
			}
		}

		public void PushDirectly(byte[] buffer)
		{
			buffers.Add((byte[])buffer.Clone());
		}
	}


	/// <summary>
	/// An information of file of compiled material's header
	/// </summary>
	public class CompiledMaterialInformation
	{
		const CompiledMaterialVersion LatestSupportVersion = CompiledMaterialVersion.Version162;
		const CompiledMaterialVersion OldestSupportVersion = CompiledMaterialVersion.Version162;

		public UInt64 GUID;
		public int Version;
		public HashSet<CompiledMaterialPlatformType> Platforms = new HashSet<CompiledMaterialPlatformType>();

		public CompiledMaterialInformationErrorCode Load(string path)
		{
			if (string.IsNullOrEmpty(path))
				return CompiledMaterialInformationErrorCode.NotFound;

			System.IO.FileStream fs = null;
			if (!System.IO.File.Exists(path)) return CompiledMaterialInformationErrorCode.NotFound;

			try
			{
				fs = System.IO.File.Open(path, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read);
			}
			catch
			{
				return CompiledMaterialInformationErrorCode.FailedToOpen;
			}


			var br = new System.IO.BinaryReader(fs);

			var buf = new byte[1024];


			if (br.Read(buf, 0, 20) != 20)
			{
				fs.Dispose();
				br.Close();
				return CompiledMaterialInformationErrorCode.InvalidFormat;
			}

			if (buf[0] != 'e' ||
				buf[1] != 'M' ||
				buf[2] != 'C' ||
				buf[3] != 'B')
			{
				fs.Dispose();
				br.Close();
				return CompiledMaterialInformationErrorCode.InvalidFormat;
			}

			Version = BitConverter.ToInt32(buf, 4);

			// bacause of camera position node, structure of uniform is changed
			if (Version < (int)OldestSupportVersion)
			{
				fs.Dispose();
				br.Close();
				return CompiledMaterialInformationErrorCode.TooOldFormat;
			}

			if (Version > (int)LatestSupportVersion)
			{
				fs.Dispose();
				br.Close();
				return CompiledMaterialInformationErrorCode.TooNewFormat;
			}

			int fversion = BitConverter.ToInt32(buf, 4);

			GUID = BitConverter.ToUInt64(buf, 8);

			var platformCount = BitConverter.ToInt32(buf, 16);

			for (int i = 0; i < platformCount; i++)
			{
				if (br.Read(buf, 0, 4) != 4)
				{
					fs.Dispose();
					br.Close();
					return CompiledMaterialInformationErrorCode.InvalidFormat;
				}

				var type = (CompiledMaterialPlatformType)BitConverter.ToInt32(buf, 0);
				Platforms.Add(type);
			}

			fs.Dispose();
			br.Close();
			return CompiledMaterialInformationErrorCode.OK;
		}
	}

	public class MaterialInformation
	{
		const MaterialVersion LatestSupportVersion = MaterialVersion.Version17;

		public MaterialVersion Version = MaterialVersion.Version17;

		public TextureInformation[] Textures = new TextureInformation[0];

		public UniformInformation[] Uniforms = new UniformInformation[0];

		public GradientInformation[] Gradients = new GradientInformation[0];

		public GradientInformation[] FixedGradients = new GradientInformation[0];

		public CustomDataInformation[] CustomData = new CustomDataInformation[0];

		public MaterialRequiredFunctionType[] RequiredFunctionTypes = new MaterialRequiredFunctionType[0];

		public UInt64 GUID;

		public int CustomData1Count = 0;

		public int CustomData2Count = 0;

		public bool HasNormal = false;

		public bool HasRefraction = false;

		public Dictionary<Language, string> Names = new Dictionary<Language, string>();

		public Dictionary<Language, string> Descriptions = new Dictionary<Language, string>();

		public string Code = string.Empty;

		public int ShadingModel = 0;

		public bool Load(string path)
		{
			if (string.IsNullOrEmpty(path))
				return false;

			byte[] file = null;

			if (!System.IO.File.Exists(path)) return false;

			try
			{
				file = System.IO.File.ReadAllBytes(path);
			}
			catch
			{
				return false;
			}

			return Load(file);
		}

		public bool Load(byte[] file)
		{
			var br = new System.IO.BinaryReader(new System.IO.MemoryStream(file));
			var buf = new byte[1024];

			if (br.Read(buf, 0, 16) != 16)
			{
				br.Close();
				return false;
			}

			if (buf[0] != 'E' ||
				buf[1] != 'F' ||
				buf[2] != 'K' ||
				buf[3] != 'M')
			{
				return false;
			}

			int version = BitConverter.ToInt32(buf, 4);

			if (version > (int)LatestSupportVersion)
			{
				return false;
			}

			GUID = BitConverter.ToUInt64(buf, 8);

			while (true)
			{
				if (br.Read(buf, 0, 8) != 8)
				{
					br.Close();
					break;
				}

				if (buf[0] == 'D' &&
				buf[1] == 'E' &&
				buf[2] == 'S' &&
				buf[3] == 'C')
				{
					var temp = new byte[BitConverter.ToInt32(buf, 4)];
					if (br.Read(temp, 0, temp.Length) != temp.Length) return false;

					var reader = new BinaryReader(temp);

					int count = 0;
					reader.Get(ref count);

					for (int i = 0; i < count; i++)
					{
						int lang = 0;
						string name = null;
						string desc = null;
						reader.Get(ref lang);
						reader.Get(ref name, Encoding.UTF8);
						reader.Get(ref desc, Encoding.UTF8);
						Names.Add((Language)lang, name);
						Descriptions.Add((Language)lang, desc);
					}
				}

				if (buf[0] == 'P' &&
				buf[1] == 'R' &&
				buf[2] == 'M' &&
				buf[3] == '_')
				{
					var temp = new byte[BitConverter.ToInt32(buf, 4)];
					if (br.Read(temp, 0, temp.Length) != temp.Length) return false;

					var reader = new BinaryReader(temp);

					reader.Get(ref ShadingModel);

					reader.Get(ref HasNormal);

					reader.Get(ref HasRefraction);

					reader.Get(ref CustomData1Count);

					reader.Get(ref CustomData2Count);

					if (version >= (int)MaterialVersion.Version17Alpha4)
					{
						int requiredCount = 0;
						reader.Get(ref requiredCount);

						RequiredFunctionTypes = new MaterialRequiredFunctionType[requiredCount];

						for (int i = 0; i < requiredCount; i++)
						{
							int temp2 = 0;
							reader.Get(ref temp2);
							RequiredFunctionTypes[i] = (MaterialRequiredFunctionType)temp2;
						}
					}

					int textureCount = 0;
					reader.Get(ref textureCount);

					List<TextureInformation> textures = new List<TextureInformation>();

					for (int i = 0; i < textureCount; i++)
					{
						TextureInformation info = new TextureInformation();

						reader.Get(ref info.Name, Encoding.UTF8);

						// name is for human, uniformName is a variable name after 3
						if (version >= 3)
						{
							reader.Get(ref info.UniformName, Encoding.UTF8);
						}
						else
						{
							info.UniformName = info.Name;
						}

						reader.Get(ref info.DefaultPath, Encoding.UTF8);
						reader.Get(ref info.Index);
						reader.Get(ref info.Priority);
						reader.Get(ref info.IsParam);
						int textureType = 0;
						reader.Get(ref textureType);
						info.Type = (TextureType)textureType;
						reader.Get(ref info.Sampler);

						// convert a path into absolute
						if (string.IsNullOrEmpty(info.DefaultPath))
						{
							info.DefaultPath = string.Empty;
						}

						textures.Add(info);
					}

					Textures = textures.ToArray();

					int uniformCount = 0;
					reader.Get(ref uniformCount);

					List<UniformInformation> uniforms = new List<UniformInformation>();

					for (int i = 0; i < uniformCount; i++)
					{
						UniformInformation info = new UniformInformation();

						reader.Get(ref info.Name, Encoding.UTF8);

						// name is for human, uniformName is a variable name after 3
						if (version >= 3)
						{
							reader.Get(ref info.UniformName, Encoding.UTF8);
						}
						else
						{
							info.UniformName = info.Name;
						}

						reader.Get(ref info.Offset);
						reader.Get(ref info.Priority);
						reader.Get(ref info.Type);
						reader.Get(ref info.DefaultValues[0]);
						reader.Get(ref info.DefaultValues[1]);
						reader.Get(ref info.DefaultValues[2]);
						reader.Get(ref info.DefaultValues[3]);
						uniforms.Add(info);
					}

					Uniforms = uniforms.ToArray();

					if (version >= (int)MaterialVersion.Version17Alpha4)
					{
						GradientInformation[] LoadGradient()
						{
							int gradientCount = 0;
							reader.Get(ref gradientCount);

							var gradients = new List<GradientInformation>();

							for (int i = 0; i < gradientCount; i++)
							{
								var info = new GradientInformation();
								info.Data = new Gradient.State();

								reader.Get(ref info.Name, Encoding.UTF8);
								reader.Get(ref info.UniformName, Encoding.UTF8);
								reader.Get(ref info.Offset);
								reader.Get(ref info.Priority);

								int colorCount = 0;
								reader.Get(ref colorCount);

								info.Data.ColorMarkers = new Gradient.ColorMarker[colorCount];
								for (int j = 0; j < colorCount; j++)
								{
									reader.Get(ref info.Data.ColorMarkers[j].Position);
									reader.Get(ref info.Data.ColorMarkers[j].ColorR);
									reader.Get(ref info.Data.ColorMarkers[j].ColorG);
									reader.Get(ref info.Data.ColorMarkers[j].ColorB);
									reader.Get(ref info.Data.ColorMarkers[j].Intensity);
								}

								int alphaCount = 0;
								reader.Get(ref alphaCount);

								info.Data.AlphaMarkers = new Gradient.AlphaMarker[alphaCount];
								for (int j = 0; j < alphaCount; j++)
								{
									reader.Get(ref info.Data.AlphaMarkers[j].Position);
									reader.Get(ref info.Data.AlphaMarkers[j].Alpha);
								}

								gradients.Add(info);
							}

							return gradients.ToArray();
						}

						Gradients = LoadGradient();
						FixedGradients = LoadGradient();
					}
				}

				if (buf[0] == 'P' &&
				buf[1] == 'R' &&
				buf[2] == 'M' &&
				buf[3] == '2')
				{
					var temp = new byte[BitConverter.ToInt32(buf, 4)];
					if (br.Read(temp, 0, temp.Length) != temp.Length) return false;

					var reader = new BinaryReader(temp);

					if (version >= 2)
					{
						int customDataCount = 0;
						reader.Get(ref customDataCount);
						AllocateCustomData(customDataCount);

						for (int j = 0; j < customDataCount; j++)
						{
							int count = 0;
							reader.Get(ref count);

							for (int i = 0; i < count; i++)
							{
								int lang = 0;
								string name = null;
								string desc = null;
								reader.Get(ref lang);
								reader.Get(ref name, Encoding.UTF8);
								reader.Get(ref desc, Encoding.UTF8);
								CustomData[j].Summaries.Add((Language)lang, name);
								CustomData[j].Descriptions.Add((Language)lang, desc);
							}
						}
					}

					int textureCount = 0;
					reader.Get(ref textureCount);

					for (int j = 0; j < textureCount; j++)
					{
						int count = 0;
						reader.Get(ref count);

						for (int i = 0; i < count; i++)
						{
							int lang = 0;
							string name = null;
							string desc = null;
							reader.Get(ref lang);
							reader.Get(ref name, Encoding.UTF8);
							reader.Get(ref desc, Encoding.UTF8);
							Textures[j].Summaries.Add((Language)lang, name);
							Textures[j].Descriptions.Add((Language)lang, desc);
						}
					}

					int uniformCount = 0;
					reader.Get(ref uniformCount);

					for (int j = 0; j < uniformCount; j++)
					{
						int count = 0;
						reader.Get(ref count);

						for (int i = 0; i < count; i++)
						{
							int lang = 0;
							string name = null;
							string desc = null;
							reader.Get(ref lang);
							reader.Get(ref name, Encoding.UTF8);
							reader.Get(ref desc, Encoding.UTF8);
							Uniforms[j].Summaries.Add((Language)lang, name);
							Uniforms[j].Descriptions.Add((Language)lang, desc);
						}
					}

					if (version >= (int)MaterialVersion.Version17Alpha4)
					{
						int gradientCount = 0;
						reader.Get(ref gradientCount);

						for (int j = 0; j < gradientCount; j++)
						{
							int count = 0;
							reader.Get(ref count);

							for (int i = 0; i < count; i++)
							{
								int lang = 0;
								string name = null;
								string desc = null;
								reader.Get(ref lang);
								reader.Get(ref name, Encoding.UTF8);
								reader.Get(ref desc, Encoding.UTF8);
								Gradients[j].Summaries.Add((Language)lang, name);
								Gradients[j].Descriptions.Add((Language)lang, desc);
							}
						}
					}
				}

				if (buf[0] == 'E' &&
				buf[1] == '_' &&
				buf[2] == 'C' &&
				buf[3] == 'D')
				{
					var temp = new byte[BitConverter.ToInt32(buf, 4)];
					if (br.Read(temp, 0, temp.Length) != temp.Length) return false;

					var reader = new BinaryReader(temp);

					int customDataCount = 0;
					reader.Get(ref customDataCount);
					AllocateCustomData(customDataCount);

					for (int j = 0; j < customDataCount; j++)
					{
						reader.Get(ref CustomData[j].DefaultValues[0]);
						reader.Get(ref CustomData[j].DefaultValues[1]);
						reader.Get(ref CustomData[j].DefaultValues[2]);
						reader.Get(ref CustomData[j].DefaultValues[3]);
					}
				}

				if (buf[0] == 'G' &&
				buf[1] == 'E' &&
				buf[2] == 'N' &&
				buf[3] == 'E')
				{
					var temp = new byte[BitConverter.ToInt32(buf, 4)];
					if (br.Read(temp, 0, temp.Length) != temp.Length) return false;

					var reader = new BinaryReader(temp);

					reader.Get(ref Code, Encoding.UTF8);
				}

				if (buf[0] == 'D' &&
				buf[1] == 'A' &&
				buf[2] == 'T' &&
				buf[3] == 'A')
				{
				}
			}

			return true;
		}

		private void AllocateCustomData(int customDataCount)
		{
			if (CustomData.Count() == 0)
			{
				CustomData = new CustomDataInformation[customDataCount];
				for (int j = 0; j < customDataCount; j++)
				{
					CustomData[j] = new CustomDataInformation();
				}
			}
		}

		public class CustomDataInformation
		{
			public Dictionary<Language, string> Summaries = new Dictionary<Language, string>();
			public Dictionary<Language, string> Descriptions = new Dictionary<Language, string>();
			public float[] DefaultValues = new float[4];
		}


		public class TextureInformation
		{
			public string Name;
			public string UniformName;
			public int Index;
			public string DefaultPath;
			public bool IsParam;
			public int Sampler;
			public TextureType Type = TextureType.Color;
			public int Priority = 1;

			public Dictionary<Language, string> Summaries = new Dictionary<Language, string>();
			public Dictionary<Language, string> Descriptions = new Dictionary<Language, string>();
		}

		public class UniformInformation
		{
			public string Name;
			public string UniformName;
			public int Offset;
			public int Type = 0;
			public float[] DefaultValues = new float[4];
			public int Priority = 1;

			public Dictionary<Language, string> Summaries = new Dictionary<Language, string>();
			public Dictionary<Language, string> Descriptions = new Dictionary<Language, string>();
		}

		public class GradientInformation
		{
			public string Name;
			public string UniformName;
			public int Offset;

			public Gradient.State Data;
			public int Priority = 1;

			public Dictionary<Language, string> Summaries = new Dictionary<Language, string>();
			public Dictionary<Language, string> Descriptions = new Dictionary<Language, string>();
		}
	}
}

#endif