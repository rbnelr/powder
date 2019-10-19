using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class PowderSim : MonoBehaviour {
	public struct Cell {
		public enum Type {
			AIR,
			WOOD,
		}

		public Type type;

		public override string ToString () => type.ToString();
	}
	
	public int2 Resolution = int2(300, 200);

	public Cell[,] Cells;
	Color32[] Pixels;

	Texture2D texture;

	public Color32[] CellTypeColors;

	public bool Reset = true;

	public GameObject Background;

	void Update () {
		Resolution = max(Resolution, 1);

		if (Cells == null || Cells.GetLength(1) != Resolution.x || Cells.GetLength(0) != Resolution.y) {
			Cells = new Cell[Resolution.y, Resolution.x];
			Pixels = new Color32[Resolution.y * Resolution.x];

			texture = new Texture2D(Resolution.x, Resolution.y, TextureFormat.RGBA32, false, false);
			texture.filterMode = FilterMode.Point;
			
			GetComponent<MeshRenderer>().material.SetTexture("_MainTex", texture);
		}

		// scale texture plane to make pixels non-stretched
		float aspect = (float)Resolution.x / (float)Resolution.y;

		var scale = this.transform.localScale;
		scale.x = scale.y * aspect;
		this.transform.localScale = scale;

		// fix backround texture stretching
		float background_aspect = 16f / 9f;
		Background.GetComponent<MeshRenderer>().material.SetVector("_BaseMap_ST", float4(aspect / background_aspect, 1, 0.5f - aspect / background_aspect / 2, 0));

		if (Reset) {
			for (int y=0; y<Resolution.y; ++y) {
				for (int x=0; x<Resolution.x; ++x) {
					Cells[y,x] = new Cell { type = Cell.Type.AIR };
				}
			}

			Reset = false;
		}
		
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Pixels[y * Resolution.x + x] = CellTypeColors[ clamp((int)Cells[y,x].type, 0, CellTypeColors.Length -1) ];
			//	Pixels[y * Resolution.x + x] = ((x % 2) ^ (y % 2)) == 0 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 0);
			}
		}
		
		texture.SetPixels32(Pixels, 0);
		texture.Apply();
	}
}
