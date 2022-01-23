using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PostEffects
{
	public class Bloom
	{
		private const int PREFILTER = 0;
		private const int DOWNSAMPLE = 1;
		private const int UPSAMPLE = 2;
		private const int FINAL = 3;
		private const int COMBINE = 4;
		private readonly int _Curve = Shader.PropertyToID("_Curve");
		private readonly int _Intensity = Shader.PropertyToID("_Intensity");
		private readonly int _NoiseTex = Shader.PropertyToID("_NoiseTex");
		private readonly int _NoiseTexScale = Shader.PropertyToID("_NoiseTexScale");
		private readonly int _SourceTex = Shader.PropertyToID("_SourceTex");

		private readonly int _Threshold = Shader.PropertyToID("_Threshold");
		private readonly List<int> bloomBufferIds = new List<int>();
		private readonly List<int> buffers = new List<int>();

		private bool inited;

		public float intensity = 0.8f;

		private Material material;

		private int mIterations = 8;
		private Shader shader;
		public float softKnee = 0.7f;
		public float threshold = 0.6f;

		public Bloom(Shader shader) => this.shader = shader;

		public int iterations
		{
			get => mIterations;
			set
			{
				if (value != mIterations) { }

				mIterations = value;
			}
		}

		public void Dispose()
		{
			CoreUtils.Destroy(material);
		}

		private void Init()
		{
			if (inited) return;
			if (shader == null) return;
			inited = true;
			material = CreateMaterial(shader);
			shader = null;
		}

		public void Apply(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
			Vector2Int resolution, RenderTextureFormat destinationFormat)
		{
			Init();
			if (!inited) return;

			buffers.Clear();

			for (var i = 0; i < iterations; i++)
			{
				var w = resolution.x >> (i + 1);
				var h = resolution.y >> (i + 1);

				if (w < 2 || h < 2) break;

				var id = GetBufferId(i);
				cmd.GetTemporaryRT(id, w, h, 0, FilterMode.Bilinear, destinationFormat);
				buffers.Add(id);
			}

			cmd.SetGlobalFloat(_Threshold, threshold);
			var knee = Mathf.Max(threshold * softKnee, 0.0001f);
			var curve = new Vector3(threshold - knee, knee * 2, 0.25f / knee);
			cmd.SetGlobalVector(_Curve, curve);

			var last = destination;
			cmd.Blit(source, last, material, PREFILTER);

			foreach (var dest in buffers)
			{
				cmd.Blit(last, dest, material, DOWNSAMPLE);
				last = dest;
			}

			for (var i = buffers.Count - 2; i >= 0; i--)
			{
				var dest = buffers[i];
				cmd.Blit(last, dest, material, UPSAMPLE);
				last = dest;
			}

			cmd.SetGlobalFloat(_Intensity, intensity);
			cmd.Blit(last, destination, material, FINAL);

			foreach (var id in buffers)
			{
				cmd.ReleaseTemporaryRT(id);
			}
		}

		private int GetBufferId(int index)
		{
			while (index >= bloomBufferIds.Count)
			{
				bloomBufferIds.Add(Shader.PropertyToID($"_BloomBuffer{bloomBufferIds.Count}"));
			}

			return bloomBufferIds[index];
		}

		public void Combine(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination,
			RenderTargetIdentifier bloom, Texture2D noise)
		{
			cmd.SetGlobalTexture(_NoiseTex, noise);
			cmd.SetGlobalVector(_NoiseTexScale, RenderTextureUtils.GetTextureScreenScale(noise));
			cmd.SetGlobalTexture(_SourceTex, source);
			cmd.Blit(bloom, destination, material, COMBINE);
		}

		private static Material CreateMaterial(Shader s) =>
			new Material(s)
			{
				hideFlags = HideFlags.HideAndDontSave,
			};
	}
}