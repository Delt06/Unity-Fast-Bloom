using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace PostEffects
{
	public class BloomRenderPass : ScriptableRenderPass, IDisposable
	{
		private static readonly int CameraColorTextureId = Shader.PropertyToID("_CameraColorTexture");
		private Bloom _bloom;
		private RenderTextureDescriptor _bloomTargetDescriptor;
		private Vector2Int _bloomTargetResolution;

		private RenderTexture _bloomTargetTexture;
		private RenderTexture _cameraTempTexture;
		private BloomSettings _settings;

		public void Dispose()
		{
			_bloom?.Dispose();
			_bloom = null;

			if (_cameraTempTexture)
			{
				RenderTexture.ReleaseTemporary(_cameraTempTexture);
				_cameraTempTexture = null;
			}

			if (_bloomTargetTexture)
			{
				RenderTexture.ReleaseTemporary(_bloomTargetTexture);
				_bloomTargetTexture = null;
			}
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
		}

		public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
		{
			EnsureBloomTargetIsCreated(_bloomTargetDescriptor);
			EnsureCameraTempTextureIsCreated(cameraTextureDescriptor);
		}

		private void EnsureBloomTargetIsCreated(in RenderTextureDescriptor descriptor)
		{
			var needToGet = false;
			if (_bloomTargetTexture == null)
			{
				needToGet = true;
			}
			else if (_bloomTargetTexture.width != descriptor.width || _bloomTargetTexture.height != descriptor.height)
			{
				RenderTexture.ReleaseTemporary(_bloomTargetTexture);
				needToGet = true;
			}

			if (!needToGet) return;

			_bloomTargetTexture = RenderTexture.GetTemporary(descriptor);
			_bloomTargetTexture.filterMode = FilterMode.Bilinear;
		}

		private void EnsureCameraTempTextureIsCreated(in RenderTextureDescriptor cameraTextureDescriptor)
		{
			var needToGet = false;
			if (_cameraTempTexture == null)
			{
				needToGet = true;
			}
			else if (!Ext.MainDescParametersMatch(_cameraTempTexture.descriptor, cameraTextureDescriptor))
			{
				RenderTexture.ReleaseTemporary(_cameraTempTexture);
				needToGet = true;
			}

			if (!needToGet) return;

			var desc = cameraTextureDescriptor;
			desc.depthBufferBits = 0;
			desc.mipCount = 0;
			_cameraTempTexture = RenderTexture.GetTemporary(desc);
		}

		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
			if (renderingData.cameraData.cameraType != CameraType.Game) return;

			var cmd = CommandBufferPool.Get("Bloom");
			cmd.Clear();
			cmd.Blit(CameraColorTextureId, _cameraTempTexture);

			_bloom.Apply(cmd, _cameraTempTexture, _bloomTargetTexture, _bloomTargetResolution,
				renderingData.cameraData.cameraTargetDescriptor
			);
			_bloom.Combine(cmd, _cameraTempTexture, CameraColorTextureId, _bloomTargetTexture, _settings.Noise);

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}
	}
}