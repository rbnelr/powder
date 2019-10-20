using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

public class PowderSim : MonoBehaviour {
	
	public enum Type {
		GAS,
		LIQUID,
		SOLID,
		POWDER,
	}

    [JsonConverter(typeof(StringEnumConverter))]
	public enum MaterialID {
		_NULL,

		WOOD,
		STONE,

		SAND,

		AIR,
		STEAM,
		SMOKE,
		FLAME,

		WATER,
		OIL,
	}
	public static readonly Type[] Types = new Type[] {
		Type.SOLID,

		Type.SOLID,
		Type.SOLID,

		Type.POWDER,

		Type.GAS,
		Type.GAS,
		Type.GAS,
		Type.GAS,
		
		Type.LIQUID,
		Type.LIQUID,
	};
	public static readonly float[] Density = new float[] {
		float.PositiveInfinity,

		5f,
		40f,

		20f,
		
		10f / 1000,
		6f / 1000,
		8f / 1000,
		8f / 1000,

		10f,
		9f,
	};
	
	[Serializable]
	public struct Cell {

		public MaterialID mat;
		public bool moved;

		public Type type => Types[(int)mat];
		public float density => Density[(int)mat];

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
	public bool Clear, SaveDialog, LoadDialog;

	public bool PauseSimulation = false;
	public bool StepSimulation = false;

	public GameObject Background;

	MeshRenderer MeshRenderer;
	private void Start () {
		MeshRenderer = GetComponent<MeshRenderer>();
	}
	void Update () {
		Resolution = max(Resolution, 1);

		if (cells == null || any(cells.GetResolution() != Resolution))
			Resize();

		if (Reset)
			reset();

		if (Clear)
			clear();

		if (SaveDialog || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.S)))
			save();

		if (LoadDialog || (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.L)))
			load();
		
		if (!PauseSimulation || StepSimulation)
			Simulate();
		StepSimulation = false;

		DrawCells();
	}
	
	void reset () {
		if (LoadFileOnStart.Length != 0) {
			if (Serialize.LoadFromFile("cells", LoadFileOnStart, out Cells cells)) {
				Resolution = cells.GetResolution();
				this.cells = cells;

				CreateTexture();
			}
		} else {
			Clear = true;
		}
		Reset = false;
	}
	void clear () {
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				cells.Array[y,x] = new Cell { mat = MaterialID.AIR };
			}
		}
		Clear = false;
	}
	
	void save () {
		Serialize.SaveToFileDialog("cells", cells);
		SaveDialog = false;
	}
	void load () {
		if (Serialize.LoadFromFileDialog("cells", out Cells cells)) {
			Resolution = cells.GetResolution();
			this.cells = cells;
		}
		LoadDialog = false;
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
		
		if (old_cells != null) { // copy old cells where possible
			for (int y=0; y<min(cells.GetResolution().y, old_cells.GetResolution().y); ++y) {
				for (int x=0; x<min(cells.GetResolution().x, old_cells.GetResolution().x); ++x) {
					cells.Array[y,x] = old_cells.Array[y,x];
				}
			}
		}

		CreateTexture();
	}

	public Texture2DArray MaterialTexArray;
	public Texture2DArray ShadingTexArray;
	public float[] MaterialTextureIndecies;
	public float[] MaterialShadingModes;

	int2 TexSize => int2(MaterialTexArray.width, MaterialTexArray.height);

	void DrawCells () {

		MeshRenderer.sharedMaterial.SetTexture("_CellsTex", texture);
		MeshRenderer.sharedMaterial.SetVector("_TexScale",				float4((float2)Resolution * TexScale / TexSize, 0,0));
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
				var c = cells.Array[y,x];
				Pixels[y * Resolution.x + x] = (byte)c.mat;

				c.moved = false;
				cells.Array[y,x] = c;
			}
		}
		
		texture.SetPixelData(Pixels, 0);
		texture.Apply();
	}
	
	public float WaterFluidity = 0.9f;

	Unity.Mathematics.Random rand = new Unity.Mathematics.Random(12345);

	void Simulate () {
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Down(int2(x,y));
			}
		}

		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Up(int2(x,y));
			}
		}

		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				DiagonalLeft(int2(x,y));
			}
		}
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				DiagonalRight(int2(x,y));
			}
		}
		
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Left(int2(x,y));
			}
			for (int x=Resolution.x-1; x>=0; --x) {
				Right(int2(x,y));
			}
		}
		
		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Down(int2(x,y));
			}
		}

		for (int y=0; y<Resolution.y; ++y) {
			for (int x=0; x<Resolution.x; ++x) {
				Up(int2(x,y));
			}
		}
	}

	void Down (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		if (c.mat == MaterialID.AIR) return;

		if ((c.type == Type.LIQUID || c.type == Type.POWDER) && !c.moved && (b.type != Type.SOLID && c.density > b.density) && !b.moved) {
			Swap(ref c, ref b);
		}
		
		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}
	void Left (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		if (c.mat == MaterialID.AIR) return;

		float MoveLeftChance = WaterFluidity / 2;

		if ((c.type == Type.LIQUID || c.type == Type.GAS) && !c.moved && (l.type == Type.LIQUID || l.type == Type.GAS) && c.mat != l.mat && !l.moved && rand.NextFloat() < MoveLeftChance) {
			Swap(ref c, ref l);
		}

		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}
	void Right (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		if (c.mat == MaterialID.AIR) return;
		
		float MoveRightChance = WaterFluidity / 2;
		float DontMoveChance = 1f - WaterFluidity;
		float MoveChance = MoveRightChance / (MoveRightChance + DontMoveChance);

		if ((c.type == Type.LIQUID || c.type == Type.GAS) && !c.moved && (r.type == Type.LIQUID || r.type == Type.GAS) && c.mat != r.mat && !r.moved && rand.NextFloat() < MoveChance) {
			Swap(ref c, ref r);
		}
		
		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}
	void Up (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		if (c.mat == MaterialID.AIR) return;

		if (c.type == Type.GAS && !c.moved && (t.type != Type.SOLID && c.density < t.density) && !t.moved) {
			Swap(ref c, ref t);
		}
		
		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}
	void DiagonalLeft (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		
		float MoveLeftChance = WaterFluidity / 2;

		if (c.type == Type.POWDER && !c.moved && (lb.type != Type.SOLID && c.density > lb.density) && !lb.moved && rand.NextFloat() < MoveLeftChance) {
			Swap(ref c, ref lb);
		}
		
		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}
	void DiagonalRight (int2 pos) {
		GetNeighborhood(pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb);
		
		float MoveRightChance = WaterFluidity / 2;
		float DontMoveChance = 1f - WaterFluidity;
		float MoveChance = MoveRightChance / (MoveRightChance + DontMoveChance);

		if (c.type == Type.POWDER && !c.moved && (rb.type != Type.SOLID && c.density > rb.density) && !rb.moved && rand.NextFloat() < MoveChance) {
			Swap(ref c, ref rb);
		}
		
		SetNeighborhood(pos, c, l, r, b, t, lb, rb);
	}

	void GetNeighborhood (int2 pos, out Cell c, out Cell l, out Cell r, out Cell b, out Cell t, out Cell lb, out Cell rb) {
		c = cells.Array[pos.y, pos.x];

		l = pos.x > 0              ? cells.Array[pos.y, pos.x -1] : new Cell { mat = MaterialID._NULL };
		r = pos.x < Resolution.x-1 ? cells.Array[pos.y, pos.x +1] : new Cell { mat = MaterialID._NULL };
		b = pos.y > 0              ? cells.Array[pos.y -1, pos.x] : new Cell { mat = MaterialID._NULL };
		t = pos.y < Resolution.y-1 ? cells.Array[pos.y +1, pos.x] : new Cell { mat = MaterialID._NULL };
		
		lb = pos.y > 0 && pos.x > 0              ? cells.Array[pos.y -1, pos.x -1] : new Cell { mat = MaterialID._NULL };
		rb = pos.y > 0 && pos.x < Resolution.x-1 ? cells.Array[pos.y -1, pos.x +1] : new Cell { mat = MaterialID._NULL };
	}
	void SetNeighborhood (int2 pos, Cell c, Cell l, Cell r, Cell b, Cell t, Cell lb, Cell rb) {
		cells.Array[pos.y, pos.x] = c;

		if (pos.x > 0             ) cells.Array[pos.y, pos.x -1] = l;
		if (pos.x < Resolution.x-1) cells.Array[pos.y, pos.x +1] = r;
		if (pos.y > 0             ) cells.Array[pos.y -1, pos.x] = b;
		if (pos.y < Resolution.y-1) cells.Array[pos.y +1, pos.x] = t;

		if (pos.y > 0 && pos.x > 0             ) cells.Array[pos.y -1, pos.x -1] = lb;
		if (pos.y > 0 && pos.x < Resolution.x-1) cells.Array[pos.y -1, pos.x +1] = rb;
	}

	void Swap (ref Cell a, ref Cell b) {
		var tmp = a;
		a = b;
		b = tmp;

		a.moved = true && a.mat != MaterialID.AIR; // air can move freely to improve falling fluids
		b.moved = true && b.mat != MaterialID.AIR;
	}
}
