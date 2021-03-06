﻿using UnityEngine;
using System.Collections;
using System.Linq;

[RequireComponent(typeof(Shields))]
[RequireComponent(typeof(MeshFilter))]
public class ShieldCollider : PoolBehaviour {
	public Mesh mesh;
	public MeshCollider meshCollider;
	public Shields shields;
	
	void Awake() {
		shields = GetComponent<Shields>();
		mesh = GetComponent<MeshFilter>().mesh;
        meshCollider = GetComponent<MeshCollider>();
	}

	public void OnShieldsEnable() {
        if (meshCollider == null)
    		meshCollider = gameObject.AddComponent<MeshCollider>();
		meshCollider.convex = true;
		UpdateMesh(shields.ellipse);
	}

	public void OnShieldsDisable() {
		Destroy(meshCollider);
	}
	
	public void OnShieldsResize() {
		UpdateMesh(shields.ellipse);
	}

    public void OnShieldsMove() {
        UpdateMesh(shields.ellipse);
    }

	public void UpdateMesh(Ellipse ellipse) {		
		if (meshCollider == null)
			return;

        var positions = shields.arcPositions;

		var triangles = new int[positions.Length*3*2];
		var vertices = new Vector3[positions.Length*2];
		for (var i = 0; i < positions.Length; i += 2) {
			vertices[i] = (Vector3)positions[i] + Vector3.forward*2;
			vertices[i+1] = (Vector3)positions[i] + Vector3.back*2;
		}
		
		for (var i = 0; i < vertices.Length-3; i++) {
			triangles[i*3] = i;
			triangles[i*3+1] = i+1;
			triangles[i*3+2] = i+2;
		}
		
		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.Optimize();
		mesh.RecalculateBounds();
		
		// this null set here seems to be necessary to force the mesh collider
		// to update. not sure why! - mispy
		meshCollider.sharedMesh = null;
		meshCollider.sharedMesh = mesh;
	}
}