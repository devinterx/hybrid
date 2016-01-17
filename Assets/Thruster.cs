using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections;

public class Thruster : BlockComponent {
    [HideInInspector]
    public ParticleSystem ps;

    public bool isFiring = false;
    public bool isFiringAttitude = false;

    void Start() {
        ps = GetComponentInChildren<ParticleSystem>();
    }

    public override void OnSerialize(MispyNetworkWriter writer, bool initial) {
        writer.Write(isFiring);
        writer.Write(isFiringAttitude);
    }

    public override void OnDeserialize(MispyNetworkReader reader, bool initial) {
        isFiring = reader.ReadBoolean();
        isFiringAttitude = reader.ReadBoolean();
    }
    
    public void Fire() {        
        if (!block.isPowered)
            return;

        CancelInvoke("Stop");
        Invoke("Stop", 0.1f);

        if (isFiring == false) {
            isFiringAttitude = false;
            isFiring = true;          
            SpaceNetwork.Sync(this);
        }
    }

    public void FireAttitude() {
        if (!block.isPowered)
            return;

        CancelInvoke("Stop");
        Invoke("Stop", 0.1f);

        if (isFiringAttitude == false) {
            isFiring = false;
            isFiringAttitude = true;
            SpaceNetwork.Sync(this);
        }       
    }

    public void Stop() {
        if (isFiring == true || isFiringAttitude == true) {
            isFiring = false;
            isFiringAttitude = false;
            SpaceNetwork.Sync(this);
        }
    }

    void Update() {
        if (isFiring || isFiringAttitude) {
            ps.Emit(1);
        }
    }

    void UpdateForce(float deltaTime) {
        if (isFiring) {
            form.rigidBody.AddForce(-transform.up * deltaTime * 100f);
        } else if (isFiringAttitude) {            
            var dist = transform.localPosition - form.centerOfMass;
            var force = deltaTime*200f;
            
            if (dist.x > 0) {
                form.rigidBody.AddRelativeTorque(Vector3.forward * force);
            } else {
                form.rigidBody.AddRelativeTorque(Vector3.back * force);
            }
        }
    }

    void FixedUpdate() {
        if (SpaceNetwork.isServer)
            UpdateForce(Time.fixedDeltaTime);
    }
}
