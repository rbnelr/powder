using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

[ExecuteInEditMode]
public class NoiseTextureCreator : MonoBehaviour {
	
	public bool Regen = true;
	bool _Regen = true;

	public float Frequency = 10;
	public int Octaves = 1;
	
	int2 _Resolution; 
	float _Frequency;
	int _Octaves;

	public Texture2D Texture;

	void Update () {
		if (Texture == null)
			return;
		
		Regen = Regen || _Regen;
		_Regen = false;

		int2 res = int2(Texture.width, Texture.height);
		
		if (any(res != _Resolution)) Regen = true;
		if (Frequency != _Frequency) Regen = true;
		if (Octaves != _Octaves) Regen = true;

		_Resolution = res;
		_Frequency = Frequency;
		_Octaves = Octaves;

		if (Regen) {
			for (int y=0; y<res.y; ++y) {
				for (int x=0; x<res.x; ++x) {
					float2 uv = (float2)int2(x,y) / (float2)(res - 1);

					float val = noise.pnoise(uv * Frequency, Frequency);

					Texture.SetPixel(x,y, new Color(val, 0, 0), 0);
				}
			}

			Texture.Apply();

			GetComponent<MeshRenderer>().material.SetTexture("_BaseMap", Texture);
		}

		Regen = false;
	}
}
