using Newtonsoft.Json;
using UnityEngine;
using SFB;
using System;

public static class Serialize {

	public static bool SaveToFileDialog (string obj_name, object obj) {
		try {
			var extensions = new [] {
				new ExtensionFilter("JSON", "json"),
				new ExtensionFilter("All Files", "*"),
			};

			string path = StandaloneFileBrowser.SaveFilePanel("Save "+ obj_name, "", obj_name, extensions);

			if (path.Length != 0)
				return SaveToFile(obj_name, path, obj);

		} catch (Exception ex) {
			Debug.LogError("Could not save "+ obj_name +" with SaveToFileDialog()!\n"+ ex);
		}

		return false;
	}
	
	public static bool LoadFromFileDialog<T> (string obj_name, out T obj) {
		try {
			var extensions = new [] {
				new ExtensionFilter("JSON", "json"),
				new ExtensionFilter("All Files", "*"),
			};

			string[] paths = StandaloneFileBrowser.OpenFilePanel("Load "+ obj_name, "", extensions, false);

			if (paths.Length != 0)
				return LoadFromFile(obj_name, paths[0], out obj);

		} catch (Exception ex) {
			Debug.LogError("Could not load "+ obj_name +" with LoadFromFileDialog()!\n"+ ex);
		}

		obj = default(T);

		return false;
	}
	
	public static bool SaveToFile (string obj_name, string path, object obj) {
		try {
			string json_str = JsonConvert.SerializeObject(obj, Formatting.Indented);

			System.IO.File.WriteAllText(path, json_str, System.Text.Encoding.UTF8);
			
			return true;

		} catch (Exception ex) {
			Debug.LogError("Could not save "+ obj_name +" with SaveToFile()!\n"+ ex);
		}

		return false;
	}
	public static bool LoadFromFile<T> (string obj_name, string path, out T obj) {
		try {
			string json_str = System.IO.File.ReadAllText(path, System.Text.Encoding.UTF8);
			
			obj = JsonConvert.DeserializeObject<T>(json_str);

			return true;

		} catch (Exception ex) {
			Debug.LogError("Could not load "+ obj_name +" with LoadFromFile()!\n"+ ex);
		}

		obj = default(T);

		return false;
	}
}
