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
			cmd.GetTemporaryRT(BloomTargetId, _bloomTargetDescriptor, FilterMode.Bilinear);
			EnsureCameraTempTextureIsCreated(cameraTextureDescriptor);
		}

		private void EnsureCameraTempTextureIsCreated(in RenderTextureDescriptor cameraTextureDescriptor)
		{
			var needToGet = false;
			if (_cameraTempTexture == null)
			{
				needToGet = true;
			}
			else if (
				_cameraTempTexture.width != cameraTextureDescriptor.width ||
				_cameraTempTexture.height != cameraTextureDescriptor.height ||
				_cameraTempTexture.format != cameraTextureDescriptor.colorFormat
			)
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
			cmd.Blit(CameraColorTextureId, _cameraTempTexture);

			_bloom.Apply(cmd, _cameraTempTexture, BloomTargetId, _bloomTargetResolution,
				renderingData.cameraData.cameraTargetDescriptor.colorFormat
			);
			_bloom.Combine(cmd, _cameraTempTexture, CameraColorTextureId, BloomTargetId, _settings.Noise);

			context.ExecuteCommandBuffer(cmd);
			cmd.Clear();

			CommandBufferPool.Release(cmd);
		}
	}
}