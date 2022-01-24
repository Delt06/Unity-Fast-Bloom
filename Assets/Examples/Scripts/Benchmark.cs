using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Examples.Scripts
{
	public class Benchmark : MonoBehaviour
	{
		[Min(0), SerializeField]
		private int _sleepTimeMs = 30;
		[SerializeField] private Camera _camera;
		[SerializeField] private ScriptableRendererFeature _fastBloom;

		private GUILayoutOption[] _guiLayoutOptions;

		private void Awake()
		{
			var minWidth = GUILayout.MinWidth(200);
			var minHeight = GUILayout.MinHeight(100);
			_guiLayoutOptions = new[]
			{
				minWidth,
				minHeight,
			};
		}

		private void Update()
		{
			Thread.Sleep(_sleepTimeMs);
		}

		private void OnGUI()
		{
			if (GUILayout.Button("Fast Bloom", _guiLayoutOptions))
				ToggleFastBloom();
			if (GUILayout.Button("Built-in Bloom", _guiLayoutOptions))
				ToggleBuiltInBloom();
			if (GUILayout.Button("HDR", _guiLayoutOptions))
				ToggleHdr();
		}

		private void ToggleFastBloom()
		{
			_fastBloom.SetActive(!_fastBloom.isActive);
		}

		private void ToggleBuiltInBloom()
		{
			var cameraData = _camera.GetUniversalAdditionalCameraData();
			cameraData.renderPostProcessing = !cameraData.renderPostProcessing;
		}

		private static void ToggleHdr()
		{
			var renderPipeline = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
			renderPipeline.supportsHDR = !renderPipeline.supportsHDR;
		}
	}
}