using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// renders a BlockMap as a collection of tiles
// draws large static structures very efficiently
public class TileRenderer : PoolBehaviour {
    BlockMap blocks;

    [HideInInspector]
    public TileLayer baseTiles;
    [HideInInspector]    
    public TileLayer topTiles;
    
    public IEnumerable<MeshRenderer> MeshRenderers {
        get {
            foreach (var chunk in baseTiles.AllChunks)
                yield return chunk.renderer;
            
            foreach (var chunk in topTiles.AllChunks)
                yield return chunk.renderer;
        }
    }
    
    public Bounds Bounds {
        get {
            var bounds = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var chunk in baseTiles.AllChunks) {
                bounds.Encapsulate(chunk.renderer.bounds);
            }
            return bounds;
        }
    }
    
    public void Awake() {
        baseTiles = Pool.For("TileLayer").Attach<TileLayer>(transform);
        baseTiles.name = "TileLayer (Base)";

        topTiles = Pool.For("TileLayer").Attach<TileLayer>(transform);
        topTiles.name = "TileLayer (Top)";
    } 

    public void Start() {
        var form = GetComponent<Blockform>();
        if (form != null) SetBlocks(form.blocks);
    }

    void OnBlockAdded(Block block) {
        if (block.type.isComplexBlock && block.ship != null)
            return;

        if (block.isBlueprint) return;

        var tileLayer = baseTiles;
        if (block.layer == BlockLayer.Top)
            tileLayer = topTiles;

        tileLayer[block.pos.x, block.pos.y] = block.type.tileable.GetRotatedTile(block.facing);
    }

    void OnBlockRemoved(Block block) {
        var tileLayer = baseTiles;
        if (block.layer == BlockLayer.Top)
            tileLayer = topTiles;
        
        for (var i = 0; i < block.Width; i++) {
            for (var j = 0; j < block.Height; j++) {
                tileLayer[block.pos.x + i, block.pos.y + j] = null;
            }
        }
    }

    public void SetBlocks(BlockMap blocks) {
        this.blocks = blocks;
        blocks.OnBlockAdded += OnBlockAdded;
        blocks.OnBlockRemoved += OnBlockRemoved;

        foreach (var block in blocks.allBlocks)            
            OnBlockAdded(block);
    }

    public void EnableRendering() {
        baseTiles.EnableRendering();
        topTiles.EnableRendering();
    }
    
    public void DisableRendering() {
        baseTiles.DisableRendering();
        topTiles.DisableRendering();
    }

    // Force a mesh update
    public void UpdateMesh() {
        foreach (var chunk in baseTiles.AllChunks)
            chunk.UpdateMesh();

        foreach (var chunk in topTiles.AllChunks)
            chunk.UpdateMesh();        
    }
}
