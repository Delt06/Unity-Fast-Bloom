using System;
using UnityEngine;

namespace PostEffects
{
	[Serializable]
	public class BloomSettings
	{
		public Shader Shader;
		public Texture2D Noise;

		[Header("Settings"), Range(64, 1024)]
		 public int Resolution = 512;
		[Range(2, 8)] public int Iterations = 8;
		[Range(0, 10)] public float Intensity = 0.8f;
		[Range(0, 10)] public float Threshold = 0.6f;
		[Range(0, 1)] public float SoftKnee = 0.7f;
	}
}