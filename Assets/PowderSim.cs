using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class PowderSim : MonoBehaviour {
	public struct Cell {
		public enum MaterialID {
			AIR,
			WOOD,
			STONE,
			TEST
		}

		public MaterialID mat;

		public override string ToString () => mat.ToString();
	}
	
	public int2 Resolution = int2(300, 200);
	public int TexScale = 2;
	public float ShadingScale = 2;

	public Cell[,] Cells;
	byte[] Pixels;

	Texture2D texture;

	public bool Reset = true;

	public GameObject Background;

	MeshRenderer MeshRenderer;
	private void Start () {
		MeshRenderer = GetComponent<MeshRenderer>();
	}
	void Update () {
		Resolution = max(Resolution, 1);

		if (Cells == null || Cells.GetLength(1) != Resolution.x || Cells.GetLength(0) != Resolution.y) {
			Cells = new Cell[Resolution.y, Resolution.x];
			Pixels = new byte[Resolution.y * Resolution.x];

			texture = new Texture2D(Resolution.x, Resolution.y, TextureFormat.R8, false, false);
			texture.filterMode = FilterMode.Point;
			texture.wrapMode = TextureWrapMode.Clamp;
			
		}
		MeshRenderer.material.SetTexture("_CellsTex", texture);
		MeshRenderer.material.SetVector("_Resolution", float4(Resolution, 0, 0));
		MeshRenderer.material.SetFloat("_TexScale", 64f / (float)TexScale);
		MeshRenderer.material.SetFloat("_ShadingScale", 1f / (float)ShadingScale);

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
					Cells[y,x] = new Cell { mat = Cell.MaterialID.AIR };
				}
			}

			Reset = false;
		}
		
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Pixels[y * Resolution.x + x] = (byte)Cells[y,x].mat;
			//	Pixels[y * Resolution.x + x] = ((x % 2) ^ (y % 2)) == 0 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 0);
			}
		}
		
		texture.SetPixelData(Pixels, 0);
		texture.Apply();
	}
}
