using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEditor;

[ExecuteInEditMode]
public class MaterialsCreator : MonoBehaviour {
	
	public bool Regenerate = false;
	public bool RematchArray = false;

	void Update () {
		if (Regenerate) {
			Generate();
			Regenerate = false;
		}
		if (RematchArray) {
			MatchArray();
			RematchArray = false;
		}
	}

	[Serializable]
	public class MaterialInfo {
		public string Name;
		public bool Invisible = false;
		public Texture2D Texture = null;
		public int ShadingMode = 0;
		public int TextureIndex;
	}

	public PowderSim Sim;
	
	public Texture2D DefaultTexture;
	
	public Texture2D[] ShadingTextures;
	public MaterialInfo[] Materials;

	void MatchArray () {
		var types = typeof(PowderSim.MaterialID).GetEnumNames();

		if (types.Length != Materials.Length) {
			var old = Materials;
			Materials = new MaterialInfo[types.Length];

			for (int i=0; i<types.Length; ++i) {
				Materials[i] = i < min(types.Length, old.Length) ? old[i] : new MaterialInfo();
			}
		}
		
		for (int i=0; i<types.Length; ++i) {
			Materials[i].Name = types[i];
			if (Materials[i].Texture == null)
				Materials[i].Texture = DefaultTexture;
		}
	}
	void Generate () {
		var Textures = new List<Texture2D>();

		foreach (var mat in Materials) {
			mat.TextureIndex = -1;

			if (!mat.Invisible) {
				mat.TextureIndex = Textures.IndexOf(mat.Texture);
				if (mat.TextureIndex < 0) {
					mat.TextureIndex = Textures.Count;
					Textures.Add(mat.Texture);
				}
			}
		}

		Sim.MaterialTexArray = TextureArrayCreator.CreateTextureArray(Textures.ToArray(), "MaterialsTexArray");
		Sim.ShadingTexArray = TextureArrayCreator.CreateTextureArray(ShadingTextures.ToArray(), "ShadingTexArray");
		
		Sim.MaterialTextureIndecies = Materials.Select(x => (float)x.TextureIndex).ToArray();
		Sim.MaterialShadingModes = Materials.Select(x => (float)x.ShadingMode).ToArray();
		
		AssetDatabase.CreateAsset(Sim.MaterialTexArray, "Assets/MaterialsTexArray.asset");
		AssetDatabase.CreateAsset(Sim.ShadingTexArray, "Assets/ShadingTexArray.asset");
	}
}
