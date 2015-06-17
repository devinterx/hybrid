﻿using UnityEngine;
using System.Collections;



public class ShipMind : MonoBehaviour {
	public Ship ship;

	// Use this for initialization
	void Start () {
		ship = GetComponent<Ship>();
	}
	
	// Update is called once per frame
	void Update () {


		Ship targetShip = null;

		foreach (var otherShip in Ship.allActive) {
			if (otherShip != this.ship) {
				targetShip = otherShip;
				break;
			}
		}

		if (targetShip != null) {
			ship.MoveTowards(Crew.player.transform.position);
		}
	}
}
