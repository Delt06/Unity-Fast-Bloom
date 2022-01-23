using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace PostEffects
{
	public class BloomRendererFeature : ScriptableRendererFeature
	{
		[SerializeField] private BloomSettings _settings;
		private BloomRenderPass _pass;

		public override void Create()
		{
			_pass = new BloomRenderPass
			{
				renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing,
			};
		}

		public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
		{
			_pass.SetUp(_settings);
			renderer.EnqueuePass(_pass);
		}

		protected override void Dispose(bool disposing)
		{
			_pass?.Dispose();
		}
	}
}