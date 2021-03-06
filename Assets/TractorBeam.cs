﻿using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(ParticleSystem))]
public class TractorBeam : BlockComponent {
    [HideInInspector]
    public ParticleSystem beam;

    [HideInInspector]
    public List<Collider> captured = new List<Collider>();

    private float range = 10f;

    public bool isActive = false;
    public Vector2 targetPos;
        
    public IEnumerable<GameObject> GetViableTargets() {
        foreach (var collider in Physics.OverlapSphere(transform.position, this.range)) {
            if (!Util.TurretBlocked(form, transform.position, collider.transform.position)) {
                yield return collider.gameObject;
            }
        }
    }

    public void Fire(Vector2 worldPos) {    
        targetPos = worldPos;
        isActive = true;
    }
    
    
    public void Stop() {
        if (!isActive) return;

        isActive = false;

        beam.Clear();
        beam.enableEmission = false;
        if (form.shields != null && form.shields.isActive) {
            var shieldCol = form.shields.shieldCollider.meshCollider;
            foreach (var col in captured) {
                if (col == null || !col.enabled) continue;
                Physics.IgnoreCollision(col, shieldCol, false);
            }
        }
    }

    void Awake() {
        beam = GetComponentInChildren<ParticleSystem>();
        beam.enableEmission = false;
    }

    void Start() {
        StartCoroutine("BeamCoroutine");
    }
        
    IEnumerator BeamCoroutine() {
        while (true) {
            UpdateBeam();
            yield return new WaitForSeconds(0.01f);
        }
    }

    void UpdateBeam() {
        if (!isActive) return;

        Collider shieldCol = null;
        if (form.shields != null)
            shieldCol = form.shields.GetComponent<SphereCollider>();
        
        foreach (var col in captured) {
            if (col != null && col.attachedRigidbody != null) {
                col.attachedRigidbody.drag -= 5;
            }
        }
        captured.Clear();
        
        if (Util.TurretBlocked(form, transform.position, targetPos)) {
            return;
        }
        
        var targetDist = (targetPos - (Vector2)transform.position);
        var targetDir = targetDist.normalized;
        
        var targetHits = Physics.RaycastAll(transform.position, targetDir, targetDist.magnitude);;

        var targetRotation = Quaternion.LookRotation((Vector3)targetPos - transform.position);
        if (targetRotation != transform.rotation) {
        }
        transform.rotation = targetRotation;
        beam.startLifetime = Vector3.Distance(transform.position, targetPos) / Math.Abs(beam.startSpeed);
        beam.enableEmission = true;
        RaycastHit[] hits = Physics.SphereCastAll(transform.position, 0.20f, targetDir, targetDist.magnitude);
        foreach (var hit in hits) {
            var rigid = hit.collider.attachedRigidbody;
            if (rigid != null) {
                if (rigid != form.rigidBody) {
                    rigid.AddForce(-targetDir * Math.Min(rigid.mass*2, BlockType.FromId("Wall").mass) * 100);
                    if (shieldCol != null)
                        Physics.IgnoreCollision(hit.collider, shieldCol);
                    hit.collider.attachedRigidbody.drag += 5;
                    captured.Add(hit.collider);
                }
            }
        }        
    }
}
