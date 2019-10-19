using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class TextureArrayCreator {
	
	// No mipmap handling for now
	public static Texture2DArray CreateTextureArray (Texture2D[] Textures, string name="", TextureWrapMode WrapMode = TextureWrapMode.Repeat, FilterMode FilterMode = FilterMode.Point) {
		var arr = new Texture2DArray(Textures[0].width, Textures[0].height, Textures.Length, Textures[0].format, false, false);
		if (name.Length != 0) arr.name = name;

		arr.wrapMode = WrapMode;
		arr.filterMode = FilterMode;
	
		for (int i=0; i<Textures.Length; ++i)
			Graphics.CopyTexture(Textures[i], 0, 0, arr, i, 0);

		return arr;
	}
}
