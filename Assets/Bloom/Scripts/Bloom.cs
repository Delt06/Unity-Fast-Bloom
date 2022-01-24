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

		private readonly List<RenderTexture> _buffers = new List<RenderTexture>();
		private BufferParams? _bufferParams;

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
			ReleaseBuffers();
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
			Vector2Int resolution, RenderTextureDescriptor destinationDescriptor)
		{
			Init();
			if (!_initialized) return;

			EnsureBuffersAreAllocated(resolution, destinationDescriptor);

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
				cmd.Blit(last.Id, dest, _material, DownsamplePass);
				last = new Buffer
				{
					Id = dest,
					TexelSize = dest.texelSize,
				};
			}

			for (var i = _buffers.Count - 2; i >= 0; i--)
			{
				var dest = _buffers[i];
				cmd.SetGlobalVector(TexelSizeId, last.TexelSize);
				cmd.Blit(last.Id, dest, _material, UpsamplePass);
				last = new Buffer
				{
					Id = dest,
					TexelSize = dest.texelSize,
				};
			}

			cmd.SetGlobalFloat(IntensityId, Intensity);
			cmd.SetGlobalVector(TexelSizeId, last.TexelSize);
			cmd.Blit(last.Id, destination, _material, FinalPass);
		}

		private static Vector2 GetTexelSize(Vector2Int resolution) => new Vector2(1f / resolution.x, 1f / resolution.y);

		private void EnsureBuffersAreAllocated(Vector2Int resolution, in RenderTextureDescriptor destinationDescriptor)
		{
			var needToAllocate = false;

			if (_bufferParams == null)
			{
				needToAllocate = true;
			}
			else
			{
				var bufferParams = _bufferParams.Value;
				if (bufferParams.Iterations != Iterations ||
				    bufferParams.Resolution != resolution ||
				    !Ext.MainDescParametersMatch(bufferParams.DestinationDescriptor, destinationDescriptor))
				{
					ReleaseBuffers();
					needToAllocate = true;
				}
			}

			if (!needToAllocate) return;

			_buffers.Clear();
			for (var i = 0; i < Iterations; i++)
			{
				var w = resolution.x >> (i + 1);
				var h = resolution.y >> (i + 1);

				if (w < 2 || h < 2) break;

				var desc = new RenderTextureDescriptor(w, h, destinationDescriptor.colorFormat, 0, 0);
				var rt = RenderTexture.GetTemporary(desc);
				rt.filterMode = FilterMode.Bilinear;
				_buffers.Add(rt);
			}

			_bufferParams = new BufferParams
			{
				Iterations = Iterations,
				DestinationDescriptor = destinationDescriptor,
				Resolution = resolution,
			};
		}

		private void ReleaseBuffers()
		{
			foreach (var buffer in _buffers)
			{
				RenderTexture.ReleaseTemporary(buffer);
			}

			_buffers.Clear();
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

		private struct BufferParams
		{
			public Vector2Int Resolution;
			public int Iterations;
			public RenderTextureDescriptor DestinationDescriptor;
		}
	}
}