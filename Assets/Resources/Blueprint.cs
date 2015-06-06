﻿using UnityEngine;
using System.Collections;

public class Blueprint : MonoBehaviour {
	public static GameObject prefab;
	public BlockMap blocks;
	public Ship ship;

	public MeshRenderer renderer;	
	private Mesh mesh;


	void Awake() {
		blocks = new BlockMap();
		blocks.OnBlockChanged = OnBlockChanged;
		renderer = GetComponent<MeshRenderer>();
		mesh = GetComponent<MeshFilter>().mesh;	
	}

	void OnBlockChanged(Block newBlock, Block oldBlock) {
		UpdateMesh();
	}

	// Use this for initialization
	void Start () {
		//ship = gameObject.transform.parent.GetComponent<Ship>();
	}
	
	void UpdateMesh() {
		Profiler.BeginSample("UpdateMesh");
		
		mesh.Clear();
		mesh.vertices = blocks.meshVertices;
		mesh.triangles = blocks.meshTriangles;
		mesh.uv = blocks.meshUV;
		mesh.Optimize();
		mesh.RecalculateNormals();	

		Profiler.EndSample();
	}
}
