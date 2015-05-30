﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Pool {
	public static Pool ParticleThrust;
	
	public static void CreatePools() {
		Pool.ParticleThrust = new Pool(Game.main.thrustPrefab, 10);
	}

	public GameObject pooledObject;
	public List<GameObject> pooledObjects;

	public Pool(GameObject prefab, int startingAmount) {
		pooledObject = prefab;
		pooledObjects = new List<GameObject>();
		for(int i = 0; i < startingAmount; i++)
		{
			GameObject obj = Object.Instantiate(pooledObject) as GameObject;
			obj.SetActive(false);
			pooledObjects.Add(obj);
		}
	}
	
	public GameObject TakeObject() {
		for (int i = 0; i < pooledObjects.Count; i++) {
			if (pooledObjects[i] == null) {
				GameObject obj = Object.Instantiate(pooledObject) as GameObject;
				obj.SetActive(false);
				pooledObjects[i] = obj;
				return pooledObjects[i];
			}

			if (!pooledObjects[i].activeInHierarchy) {
				return pooledObjects[i];
			}
		}

		GameObject obj2 = Object.Instantiate(pooledObject) as GameObject;
		obj2.SetActive(false);
		pooledObjects.Add(obj2);
		return obj2;
	}
}
