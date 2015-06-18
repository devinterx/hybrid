﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Random = UnityEngine.Random;

public class Generate : MonoBehaviour {
	public static Ship Asteroid(Vector2 pos, int radius) {
		var shipObj = Pool.For("Ship").TakeObject();
		var ship = shipObj.GetComponent<Ship>();
		ship.blocks.ExpandBlockSequence(8192);

		ship.name = "Asteroid";
		for (var x = -radius; x < radius; x++) {
			for (var y = -radius; y < radius; y++) {
				if (Vector2.Distance(new Vector2(x, y), new Vector2(0, 0)) <= radius) {

					//var ori = new Vector2[] { Vector2.up, Vector2.right, -Vector2.up, -Vector2.right };
					ship.SetBlock(x, y, Block.types["wall"]);
					//ship.SetBlock(x, y, Block.types["wall"], ori[Random.Range(0, 3)]);
				}
			}
		}
		shipObj.transform.position = pos;
		shipObj.SetActive(true);
		return ship;
	}

	public static Ship TestShip(Vector2 pos) {
		var shipObj = Pool.For("Ship").TakeObject();
		var ship = shipObj.GetComponent<Ship>();

		ship.name = "Test Ship";
		ship.SetBlock(0, 0, Block.types["console"], Orientation.up);
		ship.SetBlock(0, 1, Block.types["wall"]);
		ship.SetBlock(-1, 1, Block.types["wall"], Orientation.left);
		ship.SetBlock(1, 1, Block.types["wall"], Orientation.right);
		ship.SetBlock(0, -1, Block.types["thruster"], Orientation.up);
		ship.SetBlock(0, 2, Block.types["wall"]);
		ship.SetBlock(1, 2,  Block.types["thruster"], Orientation.down);
		ship.SetBlock(-1, 2,  Block.types["thruster"], Orientation.down);
		ship.SetBlock(2, 2,  Block.types["wall"], Orientation.down);
		ship.SetBlock(-2, 2,  Block.types["wall"], Orientation.down);
		ship.SetBlock(2, 1,  Block.types["thruster"], Orientation.right);
		ship.SetBlock(-2, 1,  Block.types["thruster"], Orientation.left);
		ship.SetBlock(0, 3, Block.types["tractorBeam"], Orientation.down);

		shipObj.transform.Rotate(new Vector3(0, 0, 90));
		shipObj.transform.position = pos;
		shipObj.SetActive(true);

		foreach (var block in ship.blocks.All.ToArray()) {
			//ship.blocks[block.pos] = null;
		}

		return ship;
	}

	public static void Room(Ship ship, int x, int y, int width, int height) {
		for (var i = x; i < x+width; i++) {
			for (var j = y; j < y+height; j++) {
				ship.SetBlock(i, j, Block.types["floor"]);
			}
		}
	}

	public static void HorizontalTunnel(Ship ship, int y, int x1, int x2) {
		for (var x = Math.Min(x1, x2); x < Math.Max(x1, x2); x++) {
			ship.SetBlock(x, y, Block.types["floor"]);
		}
	}

	public static void VerticalTunnel(Ship ship, int x, int y1, int y2) {
		for (var y = Math.Min(y1, y2); y < Math.Max(y1, y2); y++) {
			ship.SetBlock(x, y, Block.types["floor"]);
		}
	}

	public static void ConnectRoom(Ship ship, Rect r1, Rect r2) {
		if (r1.yMin <= r2.yMin && r1.yMax >= r2.yMin) {
			Generate.HorizontalTunnel(ship, (int)(r2.yMin+r1.yMax)/2, (int)r1.center.x, (int)r2.center.x);
		} else if (r2.yMin <= r1.yMin && r2.yMax >= r1.yMin) {
			Generate.HorizontalTunnel(ship, (int)(r1.yMin+r2.yMax)/2, (int)r1.center.x, (int)r2.center.x);
		} else if (r1.xMin <= r2.xMin && r1.xMax >= r2.xMin) {
			Generate.VerticalTunnel(ship, (int)(r2.xMin+r1.xMax)/2, (int)r1.center.y, (int)r2.center.y);
		} else if (r2.xMin <= r1.xMin && r2.xMax >= r1.xMin) {
			Generate.VerticalTunnel(ship, (int)(r1.xMin+r2.xMax)/2, (int)r1.center.y, (int)r2.center.y);
		}



		///if (r2.yMin < r1.yMin && r2.yMax > r1.yMax)

		/*if ((r1.yMin < r2.yMin && r1.yMax > r2.yMin) || (r2.yMin < r1.yMin && r2.yMax > r1.yMin)) {
			var minX = (int)Math.Min(r1.center.x, r2.center.x);
			var maxX = (int)Math.Max(r1.center.x, r2.center.x);
			
			for (var x = minX; x < maxX; x++) {
				ship.SetBlock(x, (int)r1.center.y, Block.types["floor"]);
			}
		} else if ((r1.xMin < r2.xMin && r1.xMax > r2.xMin) || (r2.xMin < r1.xMin && r2.xMax > r1.xMin)) {
			var minY = (int)Math.Min(r1.center.y, r2.center.y);
			var maxY = (int)Math.Max(r1.center.y, r2.center.y);
			
			for (var y = minY; y < maxY; y++) {
				ship.SetBlock((int)r1.center.x, y, Block.types["floor"]);
			}
		}*/
	}

	public static bool Adjacent(Rect r1, Rect r2) {
		if ((r1.yMin <= r2.yMin && r1.yMax >= r2.yMin) || (r2.yMin <= r1.yMin && r2.yMax >= r1.yMin)) {
			if ((r1.xMax < r2.xMin && r1.xMax + 1 >= r2.xMin) || (r2.xMax < r1.xMin && r2.xMax + 1 >= r1.xMin)) {
				return true;
			}
		}

		if ((r1.xMin <= r2.xMin && r1.xMax >= r2.xMin) || (r2.xMin <= r1.xMin && r2.xMax >= r1.xMin)) {
			if ((r1.yMax < r2.yMin && r1.yMax + 1 >= r2.yMin) || (r2.yMax < r1.yMin && r2.yMax + 1 >= r1.yMin)) {
				return true;
			}
		}

		return false;
	}

	public static void AssignThrusters(Ship ship) {
		foreach (var block in ship.blocks.All.ToArray()) {
			if (ship.blocks[block.x-1, block.y] == null) {
				ship.SetBlock(block.x-1, block.y, Block.types["thruster"], Orientation.left);
			}

			if (ship.blocks[block.x, block.y-1] == null) {
				ship.SetBlock(block.x, block.y-1, Block.types["thruster"], Orientation.down);
			}

			if (ship.blocks[block.x+1, block.y] == null) {
				ship.SetBlock(block.x+1, block.y, Block.types["thruster"], Orientation.right);
			}

			if (ship.blocks[block.x, block.y+1] == null) {
				ship.SetBlock(block.x, block.y+1, Block.types["thruster"], Orientation.up);
			}
		}
	}

	public static Ship EllipsoidShip(Vector2 pos, int width, int height) {
		var shipObj = Pool.For("Ship").TakeObject();
		var ship = shipObj.GetComponent<Ship>();
		ship.blocks.ExpandBlockSequence(width*height);
		
		for (var x = -width/2; x < width/2; x++) {
			for (var y = -height/2; y < height/2; y++) {
				var dist = (new Vector2(x, y) - new Vector2(0,0));
				if (dist.magnitude < Mathf.Max(width, height) && dist.x <= width && dist.y <= height) {					
					//var ori = new Vector2[] { Vector2.up, Vector2.right, -Vector2.up, -Vector2.right };
					ship.SetBlock(x, y, Block.types["wall"]);
					//ship.SetBlock(x, y, Block.types["wall"], ori[Random.Range(0, 3)]);
				}
			}
		}

		var rects = new List<Rect>();
		rects.Add(new Rect(-width/2 + 1, -height/2 + 1, width - 2, height - 2));
		var leaves = new List<Rect>();
		var minSize = 5;
		do {
			var newRects = new List<Rect>();
			foreach (var rect in rects) {
				if (rect.width < minSize || rect.height < minSize) {
					leaves.Add(rect);
					continue;
				}

				var division = Random.Range(0.3f, 0.6f);			
				if (rect.width > rect.height) {
					newRects.Add(new Rect(rect.left, rect.top, Mathf.Ceil(rect.width * division) - 1, rect.height - 1));
					newRects.Add(new Rect(rect.left + Mathf.Ceil(rect.width * division), rect.top, Mathf.Floor(rect.width * (1-division)), rect.height));
				} else {
					newRects.Add(new Rect(rect.left, rect.top, rect.width - 1, Mathf.Ceil(rect.height * division) - 1));
					newRects.Add(new Rect(rect.left, rect.top + Mathf.Ceil(rect.height * division), rect.width, Mathf.Floor(rect.height * (1-division))));
				}
			}
			if (newRects.Count == 0)
				break;
			else
				rects = newRects;
		} while (true);

		foreach (var rect in leaves) {
			Generate.Room(ship, (int)rect.left, (int)rect.top, (int)rect.width, (int)rect.height);
		}

		for (var i = 0; i < leaves.Count; i++) {
			for (var j = i+1; j < leaves.Count; j++) {
				var r1 = leaves[i];
				var r2 = leaves[j];
				if (Adjacent(r1, r2)) {
					ConnectRoom(ship, r1, r2);
				}
			}
		}

		for (var y = height/2; y > -height/2; y--) {
			Block block = null;
			for (var x = 0; x < width; x++) {
				if (ship.blocks[x,y] != null && ship.blocks[x,y].type == Block.types["floor"]) {
					block = ship.blocks[x,y];
					break;
				}
			}

			if (block != null) {
				ship.SetBlock(block.pos.x, block.pos.y, Block.types["console"], Orientation.up);
				break;
			}
		}


		/*for (var i = 0; i < leaves.Count; i++) {
			for (var j = 0; j < leaves.Count; j++) {
				if (i == j) continue;
				if (leaves[i].center.y == leaves[j].center.y) {
					var minX = (int)Math.Min(leaves[i].center.x, leaves[j].center.x);
					var maxX = (int)Math.Max(leaves[i].center.x, leaves[j].center.x);
					
					for (var x = minX; x < maxX; x++) {
						ship.SetBlock(x, (int)leaves[i].center.y, Block.types["floor"]);
					}
				} else if (leaves[i].center.x == leaves[j].center.x) {
					var minY = (int)Math.Min(leaves[i].center.y, leaves[j].center.y);
					var maxY = (int)Math.Max(leaves[i].center.y, leaves[j].center.y);
					
					for (var y = minY; y < maxY; y++) {
						ship.SetBlock((int)leaves[i].center.x, y, Block.types["floor"]);
					}
				}
			}
		}*/
		AssignThrusters(ship);

		int y2 = 0;
		for (var x = -width/2-1; x < -width/2+2; x++) {
			ship.SetBlock(x, y2, Block.types["floor"]);
		}

		var b = ship.blocks.FindType("floor");
		ship.SetBlock(b.pos.x, b.pos.y, Block.types["gravgen"]);

		b = ship.blocks.FindType("floor");
		ship.SetBlock(b.pos.x, b.pos.y, Block.types["shieldgen"]);

		shipObj.transform.position = pos;
		shipObj.SetActive(true);
		return ship;
	}

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}