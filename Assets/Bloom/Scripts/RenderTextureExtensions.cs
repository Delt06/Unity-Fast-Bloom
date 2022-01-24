using UnityEngine;

namespace PostEffects
{
	internal static class Ext
	{
		public static RenderTextureFormat
			argbHalf = RenderTextureUtils.GetSupportedFormat(RenderTextureFormat.ARGBHalf);

		public static bool MainDescParametersMatch(in RenderTextureDescriptor d1, in RenderTextureDescriptor d2) =>
			d1.width == d2.width && 
			d1.height == d2.height && 
			d1.colorFormat == d2.colorFormat;
	}
}