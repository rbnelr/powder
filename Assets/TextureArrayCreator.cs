using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class TextureArrayCreator : MonoBehaviour {
	
	public Texture2D[] Textures;

	void Start () {
		var arr = new Texture2DArray(Textures[0].width, Textures[0].height, Textures.Length, Textures[0].format, false, false);
		arr.wrapMode = TextureWrapMode.Repeat;
		arr.filterMode = FilterMode.Point;

		for (int i=0; i<Textures.Length; ++i)
			Graphics.CopyTexture(Textures[i], 0, 0, arr, i, 0);

		AssetDatabase.CreateAsset(arr, "Assets/TextureArray.asset");
	}
}
