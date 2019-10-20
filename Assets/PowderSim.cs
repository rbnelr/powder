using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class PowderSim : MonoBehaviour {
	
	public enum MaterialID {
		AIR,
		WOOD,
		STONE,
		WATER,
		OIL,
		STEAM,
		SMOKE,
		TEST
	}
	
	[Serializable]
	public struct Cell {

		public MaterialID mat;

		public override string ToString () => mat.ToString();
	}
	
	[Serializable]
	public class Cells {
		public int2 GetResolution () => int2(Array.GetLength(1), Array.GetLength(0));
		public Cell[,] Array;
		
		public Cells (int2 resolution) {
			Array = new Cell[resolution.y, resolution.x];
		}
	}
	
	public int2 Resolution = int2(300, 200);
	public int TexScale = 2;
	public float ShadingScale = 2;

	[NonSerialized]
	public Cells cells;
	byte[] Pixels;

	Texture2D texture;

	public string LoadFileOnStart = @"D:\coding\powder\save_00.json";

	public bool Reset = true;
	public bool Save = false;
	public bool Load = false;

	public GameObject Background;

	MeshRenderer MeshRenderer;
	private void Start () {
		MeshRenderer = GetComponent<MeshRenderer>();

		if (LoadFileOnStart.Length != 0) {
			if (Serialize.LoadFromFile("cells", LoadFileOnStart, out Cells cells)) {
				Resolution = cells.GetResolution();
				this.cells = cells;

				CreateTexture();
			}
			Reset = false;
		}
	}
	void Update () {
		Resolution = max(Resolution, 1);

		if (cells == null || any(cells.GetResolution() != Resolution))
			Resize();

		if (Reset)
			clear();
		if (Save || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)))
			Serialize.SaveToFileDialog("cells", cells);
		if (Load || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L))) {
			if (Serialize.LoadFromFileDialog("cells", out Cells cells)) {
				Resolution = cells.GetResolution();
				this.cells = cells;
			}
		}
		
		Reset = false;
		Save = false;
		Load = false;
		
		UpdateTexture();
	}
	
	void clear () {
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				cells.Array[y,x] = new Cell { mat = MaterialID.AIR };
			}
		}
	}

	void CreateTexture () {
		Pixels = new byte[Resolution.y * Resolution.x];

		texture = new Texture2D(Resolution.x, Resolution.y, TextureFormat.R8, false, false);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
	}
	void Resize () {
		var old_cells = cells;

		cells = new Cells(Resolution);
		Resolution = cells.GetResolution();

		clear();
		
		// copy old cells where possible
		for (int y=0; y<min(cells.GetResolution().y, old_cells.GetResolution().y); ++y) {
			for (int x=0; x<min(cells.GetResolution().x, old_cells.GetResolution().x); ++x) {
				cells.Array[y,x] = old_cells.Array[y,x];
			}
		}

		CreateTexture();
	}

	public Texture2D DisplacementTex;
	public Texture2DArray MaterialTexArray;
	public Texture2DArray ShadingTexArray;
	public float[] MaterialTextureIndecies;
	public float[] MaterialShadingModes;

	int2 TexSize => int2(MaterialTexArray.width, MaterialTexArray.height);

	public float DisplacementStrength = 1;
	public float DisplacementAnimTime = 0;
	public float DisplacementAnimSpeed = 1;

	void UpdateTexture () {
		
		DisplacementAnimTime += DisplacementAnimSpeed * Time.deltaTime;
		DisplacementAnimTime = DisplacementAnimTime % 1f;

		MeshRenderer.sharedMaterial.SetTexture("_CellsTex", texture);
		MeshRenderer.sharedMaterial.SetTexture("_DisplacementTex", DisplacementTex);
		MeshRenderer.sharedMaterial.SetVector("_TexScale",				float4((float2)Resolution * TexScale / TexSize, 0,0));
		MeshRenderer.sharedMaterial.SetVector("_DisplacementScale",		float4((float2)Resolution * TexScale / TexSize, 0,0));
		MeshRenderer.sharedMaterial.SetVector("_DisplacementTexOffset", float4(float2(0,1) * DisplacementAnimTime, 0,0));
		MeshRenderer.sharedMaterial.SetFloat("_DisplacementStrength", DisplacementStrength);
		MeshRenderer.sharedMaterial.SetVector("_ShadingScale",			float4(1f / ((float2)Resolution * ShadingScale), 0,0));
		
		MeshRenderer.sharedMaterial.SetTexture("_MaterialTexArray", MaterialTexArray);
		MeshRenderer.sharedMaterial.SetTexture("_ShadingTexArray", ShadingTexArray);
		MeshRenderer.sharedMaterial.SetFloatArray("_MaterialTextureIndecies", MaterialTextureIndecies);
		MeshRenderer.sharedMaterial.SetFloatArray("_MaterialShadingModes", MaterialShadingModes);

		// scale texture quad to make pixels non-stretched
		float aspect = (float)Resolution.x / (float)Resolution.y;

		var scale = this.transform.localScale;
		scale.x = scale.y * aspect;
		this.transform.localScale = scale;

		// fix backround texture stretching
		float background_aspect = 16f / 9f;
		Background.GetComponent<MeshRenderer>().material.SetVector("_BaseMap_ST", float4(aspect / background_aspect, 1, 0.5f - aspect / background_aspect / 2, 0));
		
		//
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Pixels[y * Resolution.x + x] = (byte)cells.Array[y,x].mat;
			//	Pixels[y * Resolution.x + x] = ((x % 2) ^ (y % 2)) == 0 ? new Color32(255, 0, 0, 255) : new Color32(0, 0, 255, 0);
			}
		}
		
		texture.SetPixelData(Pixels, 0);
		texture.Apply();
	}
}
