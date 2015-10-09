﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ShipDesigner : MonoBehaviour {
    public Ship designShip;
    public Blueprint cursor;
    public bool isDragging = false;
    public bool isMirroring = true;
    public IntVector2 cursorOrigin;
    public IntVector2 mousePos;
    BlueprintBlock cursorBlock;
    bool isCursorValid = false;
    bool isMirrorValid = false;

    public void OnEnable() {        
        cursor = Pool.For("Blueprint").Take<Blueprint>();
        cursor.name = "Cursor";
        cursor.blocks = new BlockMap(null);
        cursor.gameObject.SetActive(true);

		cursor.tiles.EnableRendering();
        Game.shipControl.gameObject.SetActive(false);

        //Game.main.debugText.text = "Designing Ship";
        //Game.main.debugText.color = Color.green;        
        designShip = Game.playerShip;
        cursor.transform.position = designShip.form.transform.position;
        cursor.transform.rotation = designShip.form.transform.rotation;
        designShip.form.tiles.DisableRendering();
        designShip.form.blueprint.tiles.EnableRendering();
        
        foreach (var blockObj in designShip.form.GetComponentsInChildren<BlockType>()) {
            blockObj.renderer.enabled = false;
        }

        Game.Pause();
    }
    
    public void OnDisable() {
        Pool.Recycle(cursor.gameObject);

        if (designShip != null) {
			designShip.form.blueprint.tiles.DisableRendering();
			designShip.form.tiles.EnableRendering();
            foreach (var blockObj in designShip.form.GetComponentsInChildren<BlockType>()) {
                blockObj.renderer.enabled = true;
            }
        }
        Game.shipControl.gameObject.SetActive(true);
        //Game.main.debugText.text = "";
        //Game.main.debugText.color = Color.white;
        Game.Unpause();
    }

    Block FindAdjoiningBlock(Vector2 worldPos, IntVector2 blockPos) {
        var neighborBlocks = new List<Block>();
        foreach (var pos in IntVector2.Neighbors(blockPos)) {
            var block = designShip.form.blueprint.blocks[pos, BlockLayer.Base];
            if (block != null) {
                neighborBlocks.Add(block);
                continue;
            }

            block = cursor.blocks[pos, BlockLayer.Base];
            if (block != null) neighborBlocks.Add(block);
        }    

        if (neighborBlocks.Count == 0)
            return null;

        return neighborBlocks.OrderBy((block) => Vector2.Distance(worldPos, designShip.form.BlockToWorldPos(block.pos))).First();
    }

    Facing FacingFromAdjoining(IntVector2 pos, Block adjoiningBlock) {
        if (adjoiningBlock == null) return Facing.up;
        return (Facing)((pos - adjoiningBlock.pos).normalized);
    }

    bool CanFitInto(Block cursorBlock, Block existingBlock) {
        // Base layer blocks only go in empty space adjacent to an existing block
        if (cursorBlock.layer == BlockLayer.Base && existingBlock == null)
            return true;
        
        // Top layer blocks go on top of floor, or sometimes inside walls
        if (cursorBlock.layer == BlockLayer.Top && existingBlock != null) {
            if (existingBlock.Is("Floor")) 
                return true;
            if (existingBlock.Is("Wall") && cursorBlock.type.canFitInsideWall)
                return true;
        }

        return false;
    }

    Block AdjoiningBlock(Block block, IntVector2 pos) {
        var adjoining = Util.AdjoiningBlock(block, pos, designShip.blueprintBlocks);
        if (adjoining != null) return adjoining;

        return Util.AdjoiningBlock(block, pos, cursor.blocks);
    }

    bool IsValidPlacement(IntVector2 desiredPos, BlueprintBlock block) {
        var adjoiningBlock = AdjoiningBlock(block, desiredPos);
        if (adjoiningBlock == null)
            return false;

        for (var i = 0; i < block.Width; i++) {
            for (var j = 0; j < block.Height; j++) {
                var pos = new IntVector2(desiredPos.x+i, desiredPos.y+j);

                foreach (var currentBlock in designShip.blueprintBlocks.BlocksAtPos(pos)) {
                    if (!CanFitInto(block, currentBlock))
                        return false;
                }

                if (PositionBlocked(pos))
                    return false;

                if (block.type.canBlockFront && !HasLineOfFire(pos, block.facing))
                    return false;
            }
        }
        
        return true;
    }

    bool PositionBlocked(IntVector2 pos) {
        foreach (var blocker in designShip.blueprintBlocks.frontBlockers) {
            if (pos.x == blocker.x && blocker.facing == Facing.up && pos.y > blocker.y)
                return true;
            if (pos.x == blocker.x && blocker.facing == Facing.down && pos.y < blocker.y)
                return true;
            if (pos.y == blocker.y && blocker.facing == Facing.right && pos.x > blocker.x)
                return true;
            if (pos.y == blocker.y && blocker.facing == Facing.left && pos.x < blocker.x)
                return true;
        }

        return false;
    }

    bool HasLineOfFire(IntVector2 blockPos, Facing dir) {
        var pos = blockPos;

        while (!designShip.blueprintBlocks.IsOutsideBounds(pos)) {
            pos = pos + (IntVector2)dir;

            if (designShip.blueprintBlocks[pos, BlockLayer.Base] != null)
                return false;
        }

        return true;
    }

    bool IsValidPlacement(BlueprintBlock cursorBlock) {
        return IsValidPlacement(cursorBlock.pos, cursorBlock);
    }

    void FinishDrag() {
        isDragging = false;

        foreach (var block in cursor.blocks.allBlocks) {
            designShip.blueprintBlocks[block.pos,  block.layer] = new BlueprintBlock(block);
        }
       
        foreach (var block in cursor.blocks.allBlocks.ToList()) {
            if (block.pos != IntVector2.zero)
                cursor.blocks[block.pos, block.layer] = null;
        }
    }

    IntVector2 MirrorPosition(IntVector2 pos) {
        var cx = Mathf.RoundToInt(designShip.form.centerOfMass.x / Tile.worldSize);
        return new IntVector2(cx + (cx - pos.x), pos.y);
    }

    void UpdateDrag() {        
        if (!Input.GetMouseButton(0)) {
            FinishDrag();
            return;
        }

        var rect = new IntRect(cursorOrigin, mousePos);
        var mirrorOrigin = MirrorPosition(cursorOrigin);
        var mirrorRect = new IntRect(mirrorOrigin, MirrorPosition(mousePos));

        foreach (var block in cursor.blocks.allBlocks.ToList()) {
            if (!rect.Contains(block.pos) && !mirrorRect.Contains(block.pos))
                cursor.blocks[block.pos, block.layer] = null;
        }

        if (isCursorValid) {
            for (var i = rect.minX; i <= rect.maxX; i++) {
                for (var j = rect.minY; j <= rect.maxY; j++) {
                    var pos = new IntVector2(i, j);
                    var newBlock = new BlueprintBlock(cursorBlock);

                    if (AdjoiningBlock(newBlock, pos) == null) {
                        var adjoiningBlock = FindAdjoiningBlock(Game.mousePos, pos);                    
                        cursorBlock.facing = FacingFromAdjoining(pos, adjoiningBlock);
                    }


                    if (cursor.blocks[pos, newBlock.layer] == null && IsValidPlacement(pos, newBlock))
                        cursor.blocks[pos, newBlock.layer] = newBlock;
                }
            }
        }

        if (isMirrorValid) {
            for (var i = mirrorRect.minX; i <= mirrorRect.maxX; i++) {
                for (var j = mirrorRect.minY; j <= mirrorRect.maxY; j++) {
                    var pos = new IntVector2(i, j);
                    var newBlock = new BlueprintBlock(cursorBlock);

                    if (newBlock.facing == Facing.right)
                        newBlock.facing = Facing.left;
                    else if (newBlock.facing == Facing.left)
                        newBlock.facing = Facing.right;

                    if (AdjoiningBlock(newBlock, pos) == null) {
                        var adjoiningBlock = FindAdjoiningBlock(Game.mousePos, pos);                    
                        cursorBlock.facing = FacingFromAdjoining(pos, adjoiningBlock);
                    }

                    if (cursor.blocks[pos, newBlock.layer] == null && IsValidPlacement(pos, newBlock))
                        cursor.blocks[pos, newBlock.layer] = newBlock;
                }
            }
        }
    }

    void Update() {
        mousePos = designShip.form.WorldToBlockPos(Game.mousePos);

        if (isDragging) {
            UpdateDrag();
            return;
        }

        foreach (var block in designShip.blueprintBlocks.frontBlockers) {
            var worldPos = designShip.form.BlockToWorldPos(block.pos);
            var dir = designShip.form.transform.TransformDirection((Vector2)block.facing);
            Annotation.DrawLine(worldPos, worldPos + (Vector2)dir*10, Color.red, 0.2f);
        }

        foreach (var block in cursor.blocks.allBlocks.ToList())
            cursor.blocks[block.pos, block.layer] = null;

        var selectedType = MainUI.blockSelector.selectedType;
        cursorBlock = new BlueprintBlock(selectedType);
        var adjoiningBlock = FindAdjoiningBlock(Game.mousePos, mousePos);                    
        cursorBlock.facing = FacingFromAdjoining(mousePos, adjoiningBlock);

        cursor.blocks[mousePos, selectedType.blockLayer] = cursorBlock;
        var mirrorPos = MirrorPosition(mousePos);
        var mirrorBlock = new BlueprintBlock(selectedType);
        if (cursorBlock.facing == Facing.left)
            mirrorBlock.facing = Facing.right;
        else if (cursorBlock.facing == Facing.right)
            mirrorBlock.facing = Facing.left;
        cursor.blocks[mirrorPos, selectedType.blockLayer] = mirrorBlock;

        isCursorValid = IsValidPlacement(mousePos, cursorBlock);
        isMirrorValid = IsValidPlacement(mirrorPos, cursorBlock);

        if (isCursorValid) {
            foreach (var renderer in cursor.tiles.MeshRenderers)
                renderer.material.color = Color.green;
        } else {
            foreach (var renderer in cursor.tiles.MeshRenderers)
                renderer.material.color = Color.red;
        }


        if (Input.GetKeyDown(KeyCode.F1)) {
            gameObject.SetActive(false);
        }

        if (EventSystem.current.IsPointerOverGameObject())
            return;

        if (Input.GetMouseButton(0)) {
            cursorOrigin = mousePos;
            isDragging = true;
            cursor.blocks[cursorOrigin, selectedType.blockLayer] = null;
            cursor.blocks[mirrorPos, selectedType.blockLayer] = null;
            UpdateDrag();
        }

        /*if (Input.GetMouseButton(0) && isValid) {            
            //designShip.blueprintBlocks[targetBlockPos, cursorBlock.layer] = new BlueprintBlock(cursorBlock);
            //designShip.blocks[targetBlockPos, cursorBlock.layer] = new Block(cursorBlock);
        } else if (Input.GetMouseButton(1)) {
            designShip.blueprintBlocks.RemoveSurface(targetBlockPos);
            designShip.blocks.RemoveSurface(targetBlockPos);
        }*/
    }
}
