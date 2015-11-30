using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class Explosive : PoolBehaviour
{
    public float explosionForce = 0.001f;
    public float explosionRadius = 2f;
    new Rigidbody rigidbody;
	public GameObject explosionPrefab;
    public Blockform originShip;
    bool hasStarted = false;

    void Awake() {
        rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnSerialize(ExtendedBinaryWriter writer, bool initial) {
        writer.Write(originShip);
    }

    public override void OnDeserialize(ExtendedBinaryReader reader, bool initial) {
        originShip = reader.ReadComponent<Blockform>();
    }

    void Start() {
        var form = originShip;
        var mcol = GetComponent<Collider>();
        if (form.shields && form.shields.isActive) {
            Physics.IgnoreCollision(form.shields.GetComponent<Collider>(), mcol, true);
            Physics.IgnoreCollision(mcol, form.shields.GetComponent<Collider>(), true);
        }
        hasStarted = true;
    }
                 
    void OnCollisionEnter(Collision col) {
        if (!hasStarted) return;

        if (enabled && col.contacts.Length > 0) {
            // compare relative velocity to collision normal - so we don't explode from a fast but gentle glancing collision
            //float velocityAlongCollisionNormal =
            //    Vector3.Project(col.relativeVelocity, col.contacts[0].normal).magnitude;
            
            //if (velocityAlongCollisionNormal > detonationImpactVelocity)
            //{

                Explode();
            //}
        }
    }

    /*void FixedUpdate() {
        foreach (var form in Game.activeSector.blockforms) {
            if (form.BlocksAtWorldPos(rigidbody.position).Any())
                Explode();
        }
    }*/

	public void Explode() {
        var explosion = Pool.For(explosionPrefab).Attach<Transform>(Game.activeSector.transients);
        explosion.transform.position = rigidbody.position;

		var multiplier = explosionRadius;
		var systems = GetComponentsInChildren<ParticleSystem>();
		foreach (ParticleSystem system in systems)
		{
			system.startSize *= multiplier;
			system.startSpeed *= multiplier;
			system.startLifetime *= Mathf.Lerp(multiplier, 1, 0.5f);
			system.Clear();
			system.Play();
		}		

        var cols = Physics.OverlapSphere(rigidbody.position, explosionRadius, LayerMask.GetMask(new string[] { "Wall", "Floor", "Shields" }));

		HashSet<Block> blocksToBreak = new HashSet<Block>();
		HashSet<Rigidbody> rigidBodies = new HashSet<Rigidbody>();
        HashSet<Rigidbody> shielded = new HashSet<Rigidbody>();

        foreach (var col in cols) {
            var shields = col.gameObject.GetComponent<Shields>();
            if (shields != null) {
                shielded.Add(col.attachedRigidbody);
                shields.TakeDamage(1f);
            }
        }

		foreach (var col in cols)
		{
			rigidBodies.Add(col.attachedRigidbody);

            if (shielded.Contains(col.attachedRigidbody)) continue;

			var form = col.attachedRigidbody.GetComponent<Blockform>();
			if (form == null || form == originShip) continue;

            foreach (var block in form.BlocksAtWorldPos(col.transform.position))
                blocksToBreak.Add(block);

		}

		foreach (var block in blocksToBreak) {
            if (block.IsDestroyed) continue;

            var startPos = block.ship.WorldToBlockPos(rigidbody.position);
            var damage = 10f;
            foreach (var pos in Util.LineBetween(startPos, block.pos)) {
                foreach (var between in block.ship.blocks.BlocksAtPos(pos)) {
                    if (between != block)
                        damage -= between.type.damageBuffer;
                }
            }

            damage = Mathf.Max(0, damage);
            block.ship.damage.DamageBlock(block, damage);
		}
		
		foreach (var rb in rigidBodies) {
            rb.AddExplosionForce(Math.Min(explosionForce, rb.mass*20), rigidbody.position, explosionRadius, 1, ForceMode.Impulse);
		}

        Pool.Recycle(gameObject);
	}
}
