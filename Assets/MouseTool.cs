using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Mathematics;

public class MouseTool : MonoBehaviour {
	public PowderSim Sim;

	MeshRenderer MeshRenderer;
	private void Start () {
		MeshRenderer = GetComponent<MeshRenderer>();
	}

	public float MouseScrollDeltaSens = 0.5f;


	public PowderSim.MaterialID DrawMaterial = PowderSim.MaterialID.WOOD;
	public float DrawSize = 5f;

	PowderSim.MaterialID? NumberKeyDown () {
		     if (Input.GetKeyDown(KeyCode.Alpha1))	return PowderSim.MaterialID.WOOD;
		else if (Input.GetKeyDown(KeyCode.Alpha2))	return PowderSim.MaterialID.STONE;
		else if (Input.GetKeyDown(KeyCode.Alpha3))	return PowderSim.MaterialID.WATER;
		else if (Input.GetKeyDown(KeyCode.Alpha4))	return PowderSim.MaterialID.OIL;
		else if (Input.GetKeyDown(KeyCode.Alpha5))	return PowderSim.MaterialID.STEAM;
		else if (Input.GetKeyDown(KeyCode.Alpha6))	return PowderSim.MaterialID.SMOKE;
		else if (Input.GetKeyDown(KeyCode.Alpha7))	return PowderSim.MaterialID.FLAME;
		else if (Input.GetKeyDown(KeyCode.Alpha8))	return PowderSim.MaterialID.SAND;
		//else if (Input.GetKeyDown(KeyCode.Alpha9))	return PowderSim.MaterialID.SAND;
		//else if (Input.GetKeyDown(KeyCode.Alpha0))	return PowderSim.MaterialID.SAND;
		else return null;
	}

	void Update () {
		var plane = new Plane(new Vector3(0, 0, -1), 0);
		var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
		bool mouseHit = plane.Raycast(ray, out float dist);

		MeshRenderer.enabled = mouseHit;
		if (mouseHit) {
			float3 pos = ray.origin + ray.direction * dist;
			transform.position = float3(pos.xy, transform.position.z);
		}

		{ // Update Tool Size
			float size_log = log2(DrawSize);
			size_log += Input.mouseScrollDelta.y * MouseScrollDeltaSens;
			DrawSize = clamp(pow(2f, size_log), 1, 1024); 

			float Diameter = DrawSize * Sim.transform.localScale.y / Sim.Resolution.y; // size in world space of mouse cursor

			transform.localScale = float3(Diameter);
			MeshRenderer.material.SetFloat("_Thickness", 0.33f*2 / DrawSize);
		}

		DrawMaterial = NumberKeyDown() ?? DrawMaterial;

		if (Input.GetMouseButton(0)) { // Draw
			Draw(DrawMaterial);
		} else if (Input.GetMouseButton(1)) { // Erase
			Draw(PowderSim.MaterialID.AIR);
		} else {
			draw_prev_frame = false;
		}
	}
	
	bool draw_prev_frame = false;
	float2 prev_center;
	float  prev_radius;

	void Draw (PowderSim.MaterialID Type) {
		float2 cur_center = ((float3)Sim.transform.InverseTransformPoint(transform.position)).xy + 0.5f;
		float  cur_radius = ((float3)Sim.transform.InverseTransformVector(transform.localScale)).y / 2;

		cur_center *= Sim.Resolution;
		cur_radius *= Sim.Resolution.y;
		
		// since moving the mouse fast results in in skipping pixels between frames
		// we store the prev frames mouse position and step through all pixels on the line from prev frame and cur frame pos to never skip pixels
		float dist = draw_prev_frame ? distance(cur_center, prev_center) : 0f;
		float cur_dist = 0;

		do {
			float t = dist != 0f ? cur_dist / dist : cur_dist;

			float2 center = lerp(cur_center, prev_center, t);
			float  radius = lerp(cur_radius, prev_radius, t);

			int2 a = clamp( (int2)floor(center - radius), 0, Sim.Resolution);
			int2 b = clamp( (int2)ceil (center + radius), 0, Sim.Resolution);

			for (int y=a.y; y<b.y; ++y) {
				for (int x=a.x; x<b.x; ++x) {
					if (distance(float2(x,y) + 0.5f, center) <= radius + 0.2f)
						Sim.cells.Array[y,x] = new PowderSim.Cell { mat = Type };
				}
			}

			cur_dist += 1;
		} while (cur_dist < dist);

		prev_center = cur_center;
		prev_radius = cur_radius;
		
		draw_prev_frame = true;
	}
}
