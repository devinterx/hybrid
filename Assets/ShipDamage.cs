using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ShipDamage : PoolBehaviour {
    Blockform form;
    BlockMap blocks;    
    HashSet<IntVector2> breaksToCheck = new HashSet<IntVector2>();

    void Awake() {
        form = GetComponent<Blockform>();
    }

    void Start() {
        blocks = form.blocks;
        blocks.OnBlockRemoved += OnBlockRemoved;
    }

    public HashSet<Block> blocksToUpdate = new HashSet<Block>();

    public override void OnSerialize(ExtendedBinaryWriter writer, bool initial) {
        if (initial) {
            var damaged = new List<Block>();
            foreach (var block in form.blocks.allBlocks)               
                if (block.health < block.type.maxHealth)
                    damaged.Add(block);

            writer.Write(damaged.Count);
            foreach (var block in damaged) {
                writer.Write(block.pos);
                writer.Write(block.health);
            }
        } else {
            writer.Write(blocksToUpdate.Count);
            foreach (var block in blocksToUpdate) {
                writer.Write(block.pos);
                writer.Write(block.health);
            }

            blocksToUpdate.Clear();
        }
    }

    public override void OnDeserialize(ExtendedBinaryReader reader, bool initial) {
        var count = reader.ReadInt32();
        while (count > 0) {
            var pos = reader.ReadIntVector2();
            var health = reader.ReadSingle();
            var block = form.blocks.Topmost(pos);
            if (block != null) {
                block.health = health;
                UpdateHealth(block);
            }
            count--;
        }
    }

    void OnEnable() {
        InvokeRepeating("UpdateBreaks", 0f, 0.1f);
    }

    void OnBlockRemoved(Block oldBlock) {        
        if (oldBlock.layer == BlockLayer.Base)
            breaksToCheck.Remove(oldBlock.pos);
    }

    public void UpdateHealth(Block block) {        
        if (block._gameObject == null && block.health == block.type.maxHealth)
            return;
        
        var healthBar = block.gameObject.GetComponent<BlockHealthBar>();
        if (healthBar == null) {
            healthBar = block.gameObject.AddComponent<BlockHealthBar>();
            healthBar.block = block;
        }
        
        healthBar.OnHealthUpdate();
        
        if (block.health <= 0) {
            BreakBlock(block);
        }
    }

    public void DamageBlock(Block block, float amount) {
        if (!SpaceNetwork.isServer) return;
        if (block.IsDestroyed) return;
        block.health = Mathf.Max(block.health - amount, 0);

        blocksToUpdate.Add(block);

        SpaceNetwork.Sync(this);
    }  
    
    public void BreakBlock(Block block, bool checkBreaks = true) {
        if (block == null) return;
        blocks[block.pos, block.layer] = null;
        
        if (block.layer == BlockLayer.Base) {
            var top = blocks[block.pos, BlockLayer.Top];
            if (top != null)
                BreakBlock(top);
            
            if (checkBreaks) {
                foreach (var neighbor in IntVector2.Neighbors(block.pos)) {
                    if (blocks[neighbor, BlockLayer.Base] != null)
                        breaksToCheck.Add(neighbor);
                }
            }           
        }
        
        
        /*var newShipObj = Pool.For("Ship").TakeObject();
        newShipObj.transform.position = BlockToWorldPos(block.pos);
        var newShip = newShipObj.GetComponent<Ship>();
        newShip.blocks[0, 0] = block;
        newShipObj.SetActive(true);
        newShip.rigidBody.velocity = rigidBody.velocity;
        newShip.rigidBody.angularVelocity = rigidBody.angularVelocity;*/
        //newShip.hasCollision = false;

        var item = Pool.For("Item").Attach<Rigidbody>(Game.activeSector.transients);
        item.position = form.BlockToWorldPos(block.pos);
        item.velocity = form.rigidBody.velocity;
        item.angularVelocity = form.rigidBody.angularVelocity;
        
        if (blocks.baseSize == 0) Pool.Recycle(gameObject);
    }
    
    public void BreakDetected(HashSet<Block> frag1, HashSet<Block> frag2) {
        var bigger = frag1.Count > frag2.Count ? frag1 : frag2;
        var smaller = frag1.Count > frag2.Count ? frag2 : frag1;
        
        foreach (var block in smaller) {
            foreach (var layerBlock in blocks.BlocksAtPos(block.pos)) {
                BreakBlock(layerBlock);
            }
        }
    }
    
    public void UpdateBreaks() {
        Profiler.BeginSample("UpdateBreaks");
        
        while (breaksToCheck.Count > 0) {
            var breakPos = breaksToCheck.First();
            breaksToCheck.Remove(breakPos);
            
            BlockBitmap fill = BlockPather.Floodfill(blocks, breakPos);
            
            if (fill.size == blocks.baseSize) {
                // There are no breaks here
                breaksToCheck.Clear();
            } else {
                if (fill.size < blocks.baseSize/2f) {
                    foreach (var pos in fill)
                        BreakBlock(blocks[pos, BlockLayer.Base], checkBreaks: false);
                } else {
                    foreach (var pos in fill)
                        breaksToCheck.Remove(pos);
                }
            }
        }
        
        Profiler.EndSample();
    }

}
