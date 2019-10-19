using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : MonoBehaviour {
	
	public string Path = "Assets/";
	public TextureWrapMode WrapMode = TextureWrapMode.Repeat;
	public FilterMode FilterMode = FilterMode.Point;
	
	public bool Regenerate = false;

	public Texture2D[] Textures;

	void Start () {
		Generate();
	}

	void Update () {
		if (Regenerate) {
			Generate();
			Regenerate = false;
		}
	}

	void Generate () {
		var arr = new Texture2DArray(Textures[0].width, Textures[0].height, Textures.Length, Textures[0].format, false, false);
		arr.wrapMode = WrapMode;
		arr.filterMode = FilterMode;
	
		for (int i=0; i<Textures.Length; ++i)
			Graphics.CopyTexture(Textures[i], 0, 0, arr, i, 0);
	
		AssetDatabase.CreateAsset(arr, Path + ".asset");
	}
}
