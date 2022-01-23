using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PostEffects
{
	public class BloomRenderPass : ScriptableRenderPass, IDisposable
	{
		private static readonly int BloomTargetId = Shader.PropertyToID("_BloomTarget");
		private static readonly int CameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
		private static readonly int CameraColorTextureTempId = Shader.PropertyToID("_CameraColorTextureTemp");
		private Bloom _bloom;
		private RenderTextureDescriptor _bloomTargetDescriptor;
		private Vector2Int _bloomTargetResolution;
		private RenderTextureDescriptor _cameraTargetDescriptor;
		private BloomSettings _settings;

		public void Dispose()
		{
			_bloom?.Dispose();
			_bloom = null;
		}

		public void SetUp(BloomSettings settings)
		{
			_settings = settings;
			_bloom ??= new Bloom(settings.Shader);

			_bloom.Iterations = _settings.Iterations;
			_bloom.Intensity = _settings.Intensity;
			_bloom.Threshold = _settings.Threshold;
			_bloom.SoftKnee = _settings.SoftKnee;
		}

		public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
		{
			_bloomTargetResolution = RenderTextureUtils.GetScreenResolution(_settings.Resolution);
			_bloomTargetDescriptor =
				new RenderTextureDescriptor(_bloomTargetResolution.x, _bloomTargetResolution.y, Ext.argbHalf, 0, 0);
			_cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			// TODO: figure a way to set TextureWrapMode.Clamp
			cmd.GetTemporaryRT(BloomTargetId, _bloomTargetDescriptor, FilterMode.Bilinear);
			cmd.GetTemporaryRT(CameraColorTextureTempId, _cameraTargetDescriptor);
		}

		public override void FrameCleanup(CommandBuffer cmd)
		{
			_bloom.ReleaseBuffers(cmd);
			cmd.ReleaseTemporaryRT(BloomTargetId);
			cmd.ReleaseTemporaryRT(CameraColorTextureTempId);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game) return;

			var cmd = CommandBufferPool.Get("Bloom");
			cmd.Clear();
			cmd.Blit(CameraColorTextureId, CameraColorTextureTempId);

			_bloom.Apply(cmd, CameraColorTextureTempId, BloomTargetId, _bloomTargetResolution,
				renderingData.cameraData.cameraTargetDescriptor.colorFormat
			);
			_bloom.Combine(cmd, CameraColorTextureTempId, CameraColorTextureId, BloomTargetId, _settings.Noise);

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}
	}
}