using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PostEffects
{
	public class Bloom
	{
		private const int PrefilterPass = 0;
		private const int DownsamplePass = 1;
		private const int UpsamplePass = 2;
		private const int FinalPass = 3;
		private const int CombinePass = 4;

		private static readonly int CurveId = Shader.PropertyToID("_Curve");
		private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
		private static readonly int NoiseTexId = Shader.PropertyToID("_NoiseTex");
		private static readonly int NoiseTexScaleId = Shader.PropertyToID("_NoiseTexScale");
		private static readonly int SourceTexId = Shader.PropertyToID("_SourceTex");
		private static readonly int TexelSizeId = Shader.PropertyToID("_TexelSize");
		private static readonly int ThresholdId = Shader.PropertyToID("_Threshold");

		private readonly List<int> _bloomBufferIds = new List<int>();
		private readonly List<Buffer> _buffers = new List<Buffer>();

		private bool _initialized;
		private Material _material;
		private Shader _shader;

		public float Intensity = 0.8f;
		public float SoftKnee = 0.7f;
		public float Threshold = 0.6f;


		public Bloom(Shader shader) => _shader = shader;

		public int Iterations { get; set; } = 8;

		public void Dispose()
		{
			CoreUtils.Destroy(_material);
		}

		private void Init()
		{
			if (_initialized) return;
			if (_shader == null) return;
			_initialized = true;
			_material = CreateMaterial(_shader);
			_shader = null;
		}

		public void Apply(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
			Vector2Int resolution, RenderTextureFormat destinationFormat)
		{
			Init();
			if (!_initialized) return;

			AllocateBuffers(cmd, resolution, destinationFormat);

			cmd.SetGlobalFloat(ThresholdId, Threshold);
			var knee = Mathf.Max(Threshold * SoftKnee, 0.0001f);
			var curve = new Vector3(Threshold - knee, knee * 2, 0.25f / knee);
			cmd.SetGlobalVector(CurveId, curve);

			var last = new Buffer
			{
				Id = destination,
				TexelSize = GetTexelSize(resolution),
			};
			cmd.Blit(source, last.Id, _material, PrefilterPass);

			foreach (var dest in _buffers)
			{
				cmd.SetGlobalVector(TexelSizeId, last.TexelSize);
				cmd.Blit(last.Id, dest.Id, _material, DownsamplePass);
				last = dest;
			}

			for (var i = _buffers.Count - 2; i >= 0; i--)
			{
				var dest = _buffers[i];
				cmd.SetGlobalVector(TexelSizeId, last.TexelSize);
				cmd.Blit(last.Id, dest.Id, _material, UpsamplePass);
				last = dest;
			}

			cmd.SetGlobalFloat(IntensityId, Intensity);
			cmd.SetGlobalVector(TexelSizeId, last.TexelSize);
			cmd.Blit(last.Id, destination, _material, FinalPass);
		}

		private static Vector2 GetTexelSize(Vector2Int resolution) => new Vector2(1f / resolution.x, 1f / resolution.y);

		private void AllocateBuffers(CommandBuffer cmd, Vector2Int resolution, RenderTextureFormat destinationFormat)
		{
			_buffers.Clear();

			for (var i = 0; i < Iterations; i++)
			{
				var w = resolution.x >> (i + 1);
				var h = resolution.y >> (i + 1);

				if (w < 2 || h < 2) break;

				var id = GetBufferId(i);
				cmd.GetTemporaryRT(id, w, h, 0, FilterMode.Bilinear, destinationFormat);
				_buffers.Add(new Buffer
					{
						Id = id,
						TexelSize = GetTexelSize(new Vector2Int(w, h)),
					}
				);
			}
		}

		public void ReleaseBuffers(CommandBuffer cmd)
		{
			for (var index = 0; index < _buffers.Count; index++)
			{
				var id = GetBufferId(index);
				cmd.ReleaseTemporaryRT(id);
			}

			_buffers.Clear();
		}

		private int GetBufferId(int index)
		{
			while (index >= _bloomBufferIds.Count)
			{
				_bloomBufferIds.Add(Shader.PropertyToID($"_BloomBuffer{_bloomBufferIds.Count}"));
			}

			return _bloomBufferIds[index];
		}

		public void Combine(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
			RenderTargetIdentifier bloom, Texture2D noise)
		{
			cmd.SetGlobalTexture(NoiseTexId, noise);
			cmd.SetGlobalVector(NoiseTexScaleId, RenderTextureUtils.GetTextureScreenScale(noise));
			cmd.SetGlobalTexture(SourceTexId, source);
			cmd.Blit(bloom, destination, _material, CombinePass);
		}

		private static Material CreateMaterial(Shader s) =>
			new Material(s)
			{
				hideFlags = HideFlags.HideAndDontSave,
			};

		private struct Buffer
		{
			public RenderTargetIdentifier Id;
			public Vector2 TexelSize;
		}
	}
}