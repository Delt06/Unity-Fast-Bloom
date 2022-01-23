using UnityEngine;

namespace PostEffects
{
	public class RenderTextureUtils
	{
		public static bool SupportsRenderToFloatTexture() =>
			SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBFloat) ||
			SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);

		public static RenderTextureFormat GetSupportedFormat(RenderTextureFormat targetFormat)
		{
			if (IsHalfFormat(targetFormat))
			{
				var supportsHalf = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf);
				if (!supportsHalf)
					targetFormat = ToFloatFormat(targetFormat);
			}

			if (!SystemInfo.SupportsRenderTextureFormat(targetFormat))
				switch (targetFormat)
				{
					case RenderTextureFormat.RHalf:
						return GetSupportedFormat(RenderTextureFormat.RGHalf);

					case RenderTextureFormat.RGHalf:
						return GetSupportedFormat(RenderTextureFormat.ARGBHalf);

					case RenderTextureFormat.RFloat:
						return GetSupportedFormat(RenderTextureFormat.RGFloat);

					case RenderTextureFormat.RGFloat:
						return GetSupportedFormat(RenderTextureFormat.ARGBFloat);
				}

			return targetFormat;
		}

		public static bool IsHalfFormat(RenderTextureFormat format)
		{
			switch (format)
			{
				case RenderTextureFormat.RHalf:
				case RenderTextureFormat.RGHalf:
				case RenderTextureFormat.ARGBHalf:
					return true;
			}

			return false;
		}

		public static RenderTextureFormat ToFloatFormat(RenderTextureFormat format)
		{
			switch (format)
			{
				case RenderTextureFormat.RHalf:
					return RenderTextureFormat.RFloat;

				case RenderTextureFormat.RGHalf:
					return RenderTextureFormat.RGFloat;

				case RenderTextureFormat.ARGBHalf:
					return RenderTextureFormat.ARGBFloat;
			}

			return format;
		}

		public static Vector2Int GetScreenResolution(int resolution)
		{
			var aspectRatio = Screen.width / (float)Screen.height;
			if (aspectRatio < 1)
				aspectRatio = 1f / aspectRatio;

			var min = resolution;
			var max = (int)(resolution * aspectRatio);

			if (Screen.width > Screen.height)
				return new Vector2Int(max, min);
			return new Vector2Int(min, max);
		}

		public static Vector2 GetTextureScreenScale(Texture2D texture)
		{
			if (texture == null) return Vector2.one;

			Vector2 scale;
			scale.x = Screen.width / (float)texture.width;
			scale.y = Screen.height / (float)texture.height;
			return scale;
		}
	}
}