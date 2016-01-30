﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Console : BlockComponent {
    LineRenderer radiusLine = null;

    private CrewBody _crew;
    public CrewBody crew {
        get { return _crew; }
        set {
            if (value == _crew) return;
            _crew = value;

            foreach (var otherBlock in linkedBlocks) {
                otherBlock.crew = _crew;
            }
        }
    }

    float controlRadius = 5f;
    public HashSet<Block> linkedBlocks = new HashSet<Block>();

    public bool CanLink(Block otherBlock) {
        return IntVector2.Distance(block.pos, otherBlock.pos) <= controlRadius;
    }

    void Start() {
        radiusLine = Pool.For("Line").Attach<LineRenderer>(transform);
        radiusLine.sortingLayerName = "UI";
        DrawRadius();

        foreach (var otherBlock in form.blocks.allBlocks) {
            if (CanLink(otherBlock))
                linkedBlocks.Add(otherBlock);
        }
    }

    void DrawRadius() {
        var lineWidth = 0.05f;
        var ellipse = new Ellipse(0, 0, controlRadius, controlRadius, 0);

        radiusLine.SetWidth(lineWidth, lineWidth);
        radiusLine.SetVertexCount(ellipse.positions.Length);
        for (int i = 0; i < ellipse.positions.Length; i++) {
            radiusLine.SetPosition(i, ellipse.positions[i]);
        }

        var color = new Color(Color.cyan.r, Color.cyan.g, Color.cyan.b, Color.cyan.a/2);
        radiusLine.SetColors(color, color);
    }

    void Update() {
        if (crew != null && crew.currentBlock != block)
            crew = null;
    }
}
