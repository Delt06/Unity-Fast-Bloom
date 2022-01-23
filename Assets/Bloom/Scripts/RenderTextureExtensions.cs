using UnityEngine;

namespace PostEffects
{
	internal static class Ext
	{
		public static RenderTextureFormat
			argbHalf = RenderTextureUtils.GetSupportedFormat(RenderTextureFormat.ARGBHalf);
	}
}