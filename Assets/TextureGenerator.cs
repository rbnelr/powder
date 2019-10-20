using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Jobs;

[ExecuteInEditMode]
public class TextureGenerator : MonoBehaviour {
	
	public bool Regen = true;
	
	public bool Animate = true;

	public string Name;
	public Type Select;

	public int2 Resolution = 64; 

	Texture2D Texture;
	Color32[] Pixels;

	public float AnimateTime = 0f;
	public float AnimateDuration = 2f;

	void Update () {
		if (Texture == null || any(int2(Texture.width, Texture.height) != Resolution)) {
			Texture = new Texture2D(Resolution.x, Resolution.y, TextureFormat.ARGB32, false, false);
			Texture.name = Name;
			Texture.wrapMode = TextureWrapMode.Repeat;
			Texture.filterMode = FilterMode.Point;

			Pixels = new Color32[Resolution.x * Resolution.y];

			Regen = true;
		}

		if (Animate) {
			AnimateTime += Time.deltaTime;
			AnimateTime = AnimateTime % AnimateDuration;

			Regen = true;
		}

		if (Regen) {
			switch (Select) {
				case Type.WATER: Water(); break;
			}

			Texture.SetPixels32(Pixels, 0);
			Texture.Apply();

			GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", Texture);
		}

		Regen = false;
	}

	public Color color;
	public Gradient Gradient;

	public float CutoffLow;
	public float CutoffHigh;

	public enum Type {
		WATER,
	};

	static float fpnoise (int octaves, float3 pos, float3 offset, float3 freq, float3 period) {
		pos += offset;

		float val = noise.pnoise(pos * freq, period * freq);

		freq *= 2;

		float total_strength = 1;
		float strength = 0.5f;

		for (int i=1; i<octaves; ++i) {
			total_strength += strength;
			
			val += strength * noise.pnoise(pos * freq, period * freq);

			freq *= 2;
			strength *= 0.5f;
		}

		return val / total_strength;
	}
	
	void Water () {
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				float2 uv = (float2)int2(x,y) / (float2)(Resolution - 1);

				float val = fpnoise(5, float3(uv, AnimateTime), 4, float3(4,4,1), float3(4f,4f, AnimateDuration));
				val = val * 0.5f + 0.5f;
				
				float alpha = val;

				//if (alpha >= CutoffLow && alpha < CutoffHigh)
				//	alpha = 0;
				//var alpha = Gradient.Evaluate(val).r;
				Pixels[y * Resolution.x + x] = new Color(color.r, color.g, color.b, alpha);
			}
		}
	}
}