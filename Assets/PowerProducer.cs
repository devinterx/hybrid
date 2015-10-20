﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PowerProducer : BlockComponent {
    public int supplyRadius;
    public bool isProducing {
        get { return isSwitchedOn && hasAvailablePower; }
    }
    public bool isSwitchedOn = true;
    public bool hasAvailablePower = true;
    PowerCircle circle;

    GameObject noPowerIndicator;

    
    public override void OnCreate() {        
        circle = Pool.For("PowerCircle").Take<PowerCircle>();
        circle.transform.SetParent(transform);
        circle.transform.position = transform.position;
        circle.transform.rotation = transform.rotation;
        circle.spriteRenderer.enabled = false;
        circle.gameObject.SetActive(true);
    }
        
    public void OnBlockSelected() {
        circle.spriteRenderer.enabled = true;
    }
    
    public void OnBlockDeselected() {
        circle.spriteRenderer.enabled = false;
    }

    public void Update() {
        if (!isProducing && noPowerIndicator == null) {
            noPowerIndicator = Pool.For("NoPower").TakeObject();
            noPowerIndicator.transform.SetParent(transform);
            noPowerIndicator.transform.rotation = block.ship.form.transform.rotation;
            noPowerIndicator.transform.position = transform.position;
            noPowerIndicator.SetActive(true);

            block.gameObject.SendMessage("OnDepowered", SendMessageOptions.DontRequireReceiver);
        } else if (isProducing && noPowerIndicator != null) {
            Pool.Recycle(noPowerIndicator);

            block.gameObject.SendMessage("OnPowered", SendMessageOptions.DontRequireReceiver);
        }
    }
}
