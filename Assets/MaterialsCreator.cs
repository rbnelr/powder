using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using UnityEditor;
using System.ComponentModel;

[ExecuteInEditMode]
public class MaterialsCreator : MonoBehaviour {
	
	public bool Regenerate = false;

	void Update () {
		if (Regenerate) {
			Generate();
			Regenerate = false;
		}
	}

	[Serializable]
	public class MaterialInfo {
		public string EnumName;

		public Texture2D Texture = null;
		public int ShadingMode = 0;
		public int TextureIndex;
	}

	public PowderSim Sim;
	
	public Texture2D DefaultTexture;
	
	public Texture2D[] ShadingTextures;
	public MaterialInfo[] Materials;

	void Generate () {
		var mats = typeof(PowderSim.MaterialID).GetEnumNames().Select(enum_name =>
			Materials.FirstOrDefault(m => m.EnumName == enum_name) ?? new MaterialInfo { EnumName = enum_name, Texture = DefaultTexture }
		).ToArray();

		var Textures = new List<Texture2D>();

		foreach (var mat in mats) {
			mat.TextureIndex = -1;

			if (mat.Texture != null) {
				mat.TextureIndex = Textures.IndexOf(mat.Texture);
				if (mat.TextureIndex < 0) {
					mat.TextureIndex = Textures.Count;
					Textures.Add(mat.Texture);
				}
			}
		}

		Sim.MaterialTexArray = TextureArrayCreator.CreateTextureArray(Textures.ToArray(), "MaterialsTexArray");
		Sim.ShadingTexArray = TextureArrayCreator.CreateTextureArray(ShadingTextures.ToArray(), "ShadingTexArray");
		
		Sim.MaterialTextureIndecies = mats.Select(x => (float)x.TextureIndex).ToArray();
		Sim.MaterialShadingModes = mats.Select(x => (float)x.ShadingMode).ToArray();
		
		AssetDatabase.CreateAsset(Sim.MaterialTexArray, "Assets/MaterialsTexArray.asset");
		AssetDatabase.CreateAsset(Sim.ShadingTexArray, "Assets/ShadingTexArray.asset");
	}
}
