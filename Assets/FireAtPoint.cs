﻿using UnityEngine;
using System.Collections;
using System.Linq;

public class FireAtPoint : BlockAbility {
    GameObject targetCircle;
    RotatingTurret[] turrets;
    
    public override bool WorksWith(Block block) {
        return block.type.GetComponent<RotatingTurret>() != null;
    }
    
    void Awake() {
        targetCircle = Pool.For("SetTargetCircle").TakeObject();
    }

    void OnEnable() {
        //targetCircle.SetActive(true);
        turrets = blocks.Select((b) => b.gameObject.GetComponent<RotatingTurret>()).ToArray();
        InputEvent.OnLeftClick.AddListener(this);
    }

    void OnDisable() {
        targetCircle.SetActive(false);
    }

    void OnLeftClick() {

    }

    void Update() {
        if (Input.GetMouseButton(0)) {
            foreach (var turret in turrets) {
                turret.gameObject.SendMessage("OnFire");
            }
        }

        targetCircle.transform.position = Game.mousePos;
        
        foreach (var turret in turrets) {
            turret.AimTowards(Game.mousePos);   
        }
    }
}
