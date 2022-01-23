using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PostEffects
{
	public class BloomRenderPass : ScriptableRenderPass, IDisposable
	{
		private readonly int _bloomTargetId = Shader.PropertyToID("_BloomTarget");
		private readonly int _cameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
		private readonly int _cameraColorTextureTempId = Shader.PropertyToID("_CameraColorTextureTemp");
		private Bloom _bloom;
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

			_bloom.iterations = _settings.Iterations;
			_bloom.intensity = _settings.Intensity;
			_bloom.threshold = _settings.Threshold;
			_bloom.softKnee = _settings.SoftKnee;
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game) return;

			var res = RenderTextureUtils.GetScreenResolution(_settings.Resolution);
			var cmd = CommandBufferPool.Get();
			cmd.Clear();

			var desc = new RenderTextureDescriptor(res.x, res.y, Ext.argbHalf, 0, 0);
			cmd.GetTemporaryRT(_bloomTargetId, desc, FilterMode.Bilinear
			); // TODO: figure a way to set TextureWrapMode.Clamp
			cmd.GetTemporaryRT(_cameraColorTextureTempId, renderingData.cameraData.cameraTargetDescriptor);
			cmd.Blit(_cameraColorTextureId, _cameraColorTextureTempId);

			_bloom.Apply(cmd, _cameraColorTextureTempId, _bloomTargetId, res,
				renderingData.cameraData.cameraTargetDescriptor.colorFormat
			);
			_bloom.Combine(cmd, _cameraColorTextureTempId, _cameraColorTextureId, _bloomTargetId, _settings.Noise);


			cmd.ReleaseTemporaryRT(_bloomTargetId);
			cmd.ReleaseTemporaryRT(_cameraColorTextureTempId);
			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();
			CommandBufferPool.Release(cmd);
		}
	}
}