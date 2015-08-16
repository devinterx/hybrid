﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Ship : PoolBehaviour {
	public static GameObject prefab;
	public static List<Ship> allActive = new List<Ship>();

	public static IEnumerable<Ship> ClosestTo(Vector2 worldPos) {
		return Ship.allActive.OrderBy((ship) => Vector2.Distance(ship.transform.position, worldPos));
	}
	
	public static Ship AtWorldPos(Vector2 worldPos) {
		foreach (var ship in Ship.allActive) {
			var blockPos = ship.WorldToBlockPos(worldPos);
			if (ship.blocks[blockPos] != null || ship.blueprint.blocks[blockPos] != null) {
				return ship;
			}
		}
		
		return null;
	}

	public BlockMap blocks;
	public Blueprint blueprint;
	
	public Rigidbody rigidBody;

	public bool hasCollision = true;
	public bool hasGravity = false;
	
	public Dictionary<IntVector2, GameObject> colliders = new Dictionary<IntVector2, GameObject>();
	public Shields shields = null;
	
	public Vector3 localCenter;

	public Dictionary<Block, GameObject> blockComponents = new Dictionary<Block, GameObject>();

	public List<Crew> maglockedCrew = new List<Crew>();

	public float scrapAvailable = 1000;

	public GameObject collidersObj;

	public TileLayer tileLayer;

	public IEnumerable<T> GetBlockComponents<T>() {
		return GetComponentsInChildren<T>();
	}

	public bool HasBlockComponent<T>() {
		return GetBlockComponents<T>().ToList().Count > 0;
	}

    public override void OnCreate() {
		rigidBody = GetComponent<Rigidbody>();
		blocks = GetComponent<BlockMap>();
		tileLayer = GetComponent<TileLayer>();
		blocks.OnBlockChanged += OnBlockChanged;

		var obj = Pool.For("Blueprint").TakeObject();
		obj.transform.parent = transform;
		obj.transform.position = transform.position;
		obj.SetActive(true);
		blueprint = obj.GetComponent<Blueprint>();
		blueprint.ship = this;

		obj = Pool.For("Holder").TakeObject();
		obj.name = "Colliders";
		obj.transform.parent = transform;
		obj.transform.position = transform.position;
		obj.SetActive(true);
		collidersObj = obj;
	}
	
	void OnEnable() {		
		if (hasCollision) {
			foreach (var block in blocks.AllBlocks) {
				if (blocks.IsEdge(block.pos)) {
					AddCollider(block);
				}
			}
		}
						
		UpdateMass();	
		UpdateShields();	
		UpdateGravity();

		foreach (var block in blocks.AllBlocks) {
			if (block.type.isComplexBlock)
				AddBlockComponent(block);
		}

		Ship.allActive.Add(this);
	}
		
	public override void OnRecycle() {
		Ship.allActive.Remove(this);
	}

	public void SetBlock(int x, int y, BlockType type) {

		var block = new Block(type);
		blocks[x, y] = block;
		var block2 = new BlueprintBlock(type);
		blueprint.blocks[x, y] = block2;
	}

	public void SetBlock(int x, int y, string typeName) {
		SetBlock(x, y, Block.types[typeName]);
	}

	public void SetBlock(int x, int y, BlockType type, Orientation orientation) {
		var block = new Block(type);
		block.orientation = orientation;
		blocks[x, y] = block;

		var block2 = new BlueprintBlock(type);
		block2.orientation = orientation;
		blueprint.blocks[x, y] = block2;
	}

	public void SetBlock(int x, int y, string typeName, Orientation orientation) {
		SetBlock(x, y, Block.types[typeName], orientation);	
	}

	public void ReceiveImpact(Rigidbody fromRigid, Block block) {
		var impactVelocity = rigidBody.velocity - fromRigid.velocity;
		var impactForce = impactVelocity.magnitude * fromRigid.mass;
		//if (impactForce < 5) return;

		// break it off into a separate fragment
		//BreakBlock(block);
	}

	public GameObject BreakBlock(Block block) {
		blocks[block.pos] = null;
		
		/*var newShipObj = Pool.For("Ship").TakeObject();
		newShipObj.transform.position = BlockToWorldPos(block.pos);
		var newShip = newShipObj.GetComponent<Ship>();
		newShip.blocks[0, 0] = block;
		newShipObj.SetActive(true);
		newShip.rigidBody.velocity = rigidBody.velocity;
		newShip.rigidBody.angularVelocity = rigidBody.angularVelocity;*/
		//newShip.hasCollision = false;

		var obj = Pool.For("Item").TakeObject();
		obj.transform.position = BlockToWorldPos(block.pos);
		obj.SetActive(true);
		var rigid = obj.GetComponent<Rigidbody>();
		rigid.velocity = rigidBody.velocity;
		rigid.angularVelocity = rigidBody.angularVelocity;

		if (blocks.Count == 0) Pool.Recycle(gameObject);

		return obj;
	}

	public void AddCollider(Block block) {
		Profiler.BeginSample("AddCollider");

		GameObject colliderObj;
		if (block.CollisionLayer == Block.wallLayer)
			colliderObj = Pool.For("WallCollider").TakeObject();
		else
			colliderObj = Pool.For("FloorCollider").TakeObject();
		colliderObj.transform.SetParent(collidersObj.transform);
		colliderObj.transform.localPosition = BlockToLocalPos(block.pos);
		colliders[block.pos] = colliderObj;
		colliderObj.SetActive(true);

		Profiler.EndSample();
	}

	public void UpdateCollider(IntVector2 pos) {
		Profiler.BeginSample("UpdateCollider");

		var block = blocks[pos];
		var hasCollider = colliders.ContainsKey(pos);
		var isEdge = blocks.IsEdge(pos);

		if (hasCollider && (!isEdge || colliders[pos].layer != block.CollisionLayer)) {
			colliders[pos].SetActive(false);
			colliders.Remove(pos);
			hasCollider = false;
		}

		if (!hasCollider && isEdge) {
			AddCollider(block);
		}

		Profiler.EndSample();
	}


	public void OnBlockChanged(Block newBlock, Block oldBlock) {
		Profiler.BeginSample("OnBlockChanged");
		if (newBlock != null) newBlock.ship = this;

		tileLayer[newBlock.pos] = newBlock.Tile;

		// Inactive ships do not automatically update on block change, to allow
		// for performant pre-runtime mass construction. kinda like turning the power
		// off so you can stick your hand in there
		// - mispy
		if (!gameObject.activeInHierarchy) {
			Profiler.EndSample();
			return;
		}
		var pos = newBlock == null ? oldBlock.pos : newBlock.pos;
		UpdateCollision(pos);

		var oldMass = oldBlock == null ? 0 : oldBlock.mass;
		var newMass = newBlock == null ? 0 : newBlock.mass;
		if (oldMass != newMass)
			UpdateMass();

		if (Block.IsType(newBlock, "shieldgen") || Block.IsType(oldBlock, "shieldgen"))
			UpdateShields();

		if (Block.IsType(newBlock, "gravgen") || Block.IsType(oldBlock, "gravgen"))
			UpdateGravity();

		if (oldBlock != null && oldBlock.type.isComplexBlock)
			Pool.Recycle(blockComponents[oldBlock]);

		if (newBlock != null && newBlock.type.isComplexBlock) {
			AddBlockComponent(newBlock);
		}


		particleCache.Remove(pos);

		Profiler.EndSample();
	}

	public void AddBlockComponent(Block block) {
		Vector2 worldOrient = transform.TransformVector(Util.orientToCardinal[block.orientation]);

		var obj = Pool.For(block.type.gameObject).TakeObject();		
		obj.transform.parent = transform;
		obj.transform.position = BlockToWorldPos(block.pos);
		obj.transform.up = worldOrient;
		blockComponents[block] = obj;
		obj.SetActive(true);
	}

	public void UpdateCollision(IntVector2 pos) {
		if (!hasCollision) return;
		
		foreach (var other in blocks.Neighbors(pos)) {
			UpdateCollider(other);
		}
		
		UpdateCollider(pos);
	}

	public void UpdateMass() {		
		var totalMass = 0.0f;
		var avgPos = new IntVector2(0, 0);
		
		foreach (var block in blocks.AllBlocks) {
			totalMass += block.mass;
			avgPos.x += block.pos.x;
			avgPos.y += block.pos.y;
		}
		
		rigidBody.mass = totalMass;

		if (blocks.Count > 0) {
			avgPos.x /= blocks.Count;
			avgPos.y /= blocks.Count;
		}
		localCenter = BlockToLocalPos(avgPos);
		rigidBody.centerOfMass = localCenter;
	}

	public void UpdateShields() {
		if (blocks.HasType("shieldgen") && shields == null) {
			var shieldObj = Pool.For("Shields").TakeObject();
			shields = shieldObj.GetComponent<Shields>();
			shieldObj.transform.parent = transform;
			shieldObj.transform.localPosition = localCenter;
			shieldObj.SetActive(true);
		} else if (!blocks.HasType("shieldgen") && shields != null) {
			shields.gameObject.SetActive(false);
			shields = null;
		}
	}

	public void UpdateGravity() {
		if (blocks.HasType("gravgen") && hasGravity == false) {
			//hasGravity = true;
			rigidBody.drag = 5;
			rigidBody.angularDrag = 5;
		} else if (!blocks.HasType("gravgen") && hasGravity == true) {
			//hasGravity = false;
			rigidBody.drag = 0;
			rigidBody.angularDrag = 0;
		}
		hasGravity = true;
	}

	public void RotateTowards(Vector2 worldPos) {
		var dir = (worldPos - (Vector2)transform.position).normalized;
		float angle = Mathf.Atan2(dir.y,dir.x)*Mathf.Rad2Deg - 90;
		var currentAngle = transform.localEulerAngles.z;

		if (Math.Abs(360+angle - currentAngle) < Math.Abs(angle - currentAngle)) {
			angle = 360+angle;
		}

		if (angle > currentAngle + 10) {
			FireAttitudeThrusters(Orientation.right);
		} else if (angle < currentAngle - 10) {
			FireAttitudeThrusters(Orientation.left);
		}

	}

	public void MoveTowards(Vector3 worldPos) {
		var dist = (worldPos - transform.position).magnitude;
		if ((worldPos - (transform.position + transform.up)).magnitude < dist) {
			FireThrusters(Orientation.down);
		}
		/*var localDir = transform.InverseTransformDirection((worldPos - (Vector2)transform.position).normalized);
		var orient = Util.cardinalToOrient[Util.Cardinalize(localDir)];
		FireThrusters((Orientation)(-(int)orient));*/
	}

	public IntVector2 WorldToBlockPos(Vector2 worldPos) {
		return LocalToBlockPos(transform.InverseTransformPoint(worldPos));
	}

	public IntVector2 LocalToBlockPos(Vector3 localPos) {
		// remember that blocks go around the center point of the center block at [0,0]		
		return new IntVector2(Mathf.FloorToInt((localPos.x + Tile.worldSize/2.0f) / Tile.worldSize),
		                      Mathf.FloorToInt((localPos.y + Tile.worldSize/2.0f) / Tile.worldSize));
	}


	public Vector2 BlockToLocalPos(IntVector2 blockPos) {
		return new Vector2(blockPos.x*Tile.worldSize, blockPos.y*Tile.worldSize);
	}

	public Vector2 BlockToWorldPos(IntVector2 blockPos) {
		return transform.TransformPoint(BlockToLocalPos(blockPos));
	}

	public Block BlockAtLocalPos(Vector3 localPos) {
		return blocks[LocalToBlockPos(localPos)];
	}

	public Block BlockAtWorldPos(Vector2 worldPos) {
		Profiler.BeginSample("BlockAtWorldPos");
		var block = blocks[WorldToBlockPos(worldPos)];
		Profiler.EndSample();
		return block;
	}

	public Dictionary<IntVector2, ParticleSystem> particleCache = new Dictionary<IntVector2, ParticleSystem>();

	public void FireThrusters(Orientation orientation) {
		foreach (var thruster in GetBlockComponents<Thruster>()) {
			if (thruster.block.orientation == orientation)
				thruster.Fire();
		}
	}

	public void FireAttitudeThrusters(Orientation orientation) {
		foreach (var thruster in GetBlockComponents<Thruster>()) {
			if (thruster.block.orientation == orientation)
				thruster.FireAttitude();
		}
	}

	
	public void FireLasers() {
		foreach (var beam in GetBlockComponents<BeamCannon>()) {
			beam.Fire();
		}
	}


	private ParticleCollisionEvent[] collisionEvents = new ParticleCollisionEvent[16];
	public void OnParticleCollision(GameObject psObj) {
		var ps = psObj.GetComponent<ParticleSystem>();
		var safeLength = ps.GetSafeCollisionEventSize();

		if (collisionEvents.Length < safeLength) {
			collisionEvents = new ParticleCollisionEvent[safeLength];
		}

		// get collision events for the gameObject that the script is attached to
		var numCollisionEvents = ps.GetCollisionEvents(gameObject, collisionEvents);

		for (var i = 0; i < numCollisionEvents; i++) {
			//Debug.Log(collisionEvents[i].intersection);
			var pos = collisionEvents[i].intersection;
			var block = BlockAtWorldPos(pos);
			if (block != null) {
				BreakBlock(block);
			}
		}
	}

	void OnCollisionEnter(Collision collision) {
		var obj = collision.rigidbody.gameObject;

		if (collision.contacts.Length == 0) return;

		if (shields != null && collision.contacts[0].thisCollider.gameObject == shields.gameObject) {
			shields.OnCollisionEnter(collision);
			return;
		}

		if (obj.tag == "Item") {
			scrapAvailable += 10;
			Pool.Recycle(obj);
			//foreach (var beam in GetBlockComponents<TractorBeam>()) {
				//if (beam.captured.Contains(obj.GetComponent<Collider>())) {
				//}
			//}
		}

		var otherShip = obj.GetComponent<Ship>();
		if (otherShip != null) {
			var block = otherShip.BlockAtWorldPos(collision.collider.transform.position);
			if (block != null)
				otherShip.ReceiveImpact(rigidBody, block);
		}
	}

	public Crew FindPilot() {
		foreach (var crew in maglockedCrew) {
			if (crew.controlConsole != null)
				return crew;
		}

		return null;
	}

	void OnCollisionStay(Collision collision) {
		if (shields != null) {
			shields.OnCollisionStay(collision);
			return;
		}
	}

	void OnCollisionExit(Collision collision) {
		if (shields != null) {
			shields.OnCollisionExit(collision);
			return;
		}
	}

	public void StartTractorBeam(Vector2 pz) {
		foreach (var tractorBeam in GetBlockComponents<TractorBeam>()) {
			tractorBeam.Fire(pz);
		}
	}

	public void StopTractorBeam() {
		foreach (var tractorBeam in GetBlockComponents<TractorBeam>()) {
			tractorBeam.Stop();
		}
	}

	void UpdateMesh() {
		Profiler.BeginSample("UpdateMesh");

		/*if (shields != null) {
			var hypo = Mathf.Sqrt(mesh.bounds.size.x*mesh.bounds.size.x + mesh.bounds.size.y*mesh.bounds.size.y);
			var scale = new Vector3(mesh.bounds.size.x, mesh.bounds.size.y, 1);
			scale.x += hypo * mesh.bounds.size.x / (mesh.bounds.size.x+mesh.bounds.size.y);
			scale.y += hypo * mesh.bounds.size.y / (mesh.bounds.size.x+mesh.bounds.size.y);
			scale.z = Math.Max(scale.x, scale.y);

			shields.transform.localScale = scale;
		}*/

		Profiler.EndSample();
	}

}
