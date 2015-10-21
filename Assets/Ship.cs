using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using System.IO;
using System.Xml;
using System.Xml.Serialization;

public static class ShipManager {
    public static Dictionary<string, ShipData> templates = new Dictionary<string, ShipData>();
    public static List<Ship> all = new List<Ship>();
    public static Dictionary<string, Ship> byId = new Dictionary<string, Ship>();

    public static ShipData RandomTemplate() {
        return templates[Util.GetRandom(templates.Keys.ToList())];
    }
    
    public static void LoadTemplates() {
        foreach (var path in Directory.GetFiles(Application.dataPath + "/Ships/", "*.xml")) {
            var data = Save.Load<ShipData>(path);
            var id = Util.GetIdFromPath(path);
            data.name = id;
            templates[id] = data;
        }
    }

    public static void LoadAll() {
        foreach (var path in Save.GetFiles("Ship")) {
            var data = Save.Load<ShipData>(path);
            data.name = Util.GetIdFromPath(path);
            var ship = ShipManager.Unpack(data);
            var id = Util.GetIdFromPath(path);
            ShipManager.all.Add(ship);
            ShipManager.byId[id] = ship;
        }
    }
    
    public static void SaveAll() {
        foreach (var sector in SectorManager.all) {
            Save.Dump(sector, Save.GetPath("Sector", sector.Id));
        }
    }

    public static void Add(Ship ship) {
        ShipManager.all.Add(ship);
    }

    public static Ship Create(string template = null, Faction faction = null, Sector sector = null, Vector2? sectorPos = null) {
		if (template == null) template = "Little Frigate";
        if (faction == null) faction = Util.GetRandom(FactionManager.all);
        //if (sector == null) sector = Util.GetRandom(SectorManager.all);
		if (sector != null && sectorPos == null) sectorPos = sector.RandomEdge();

        var ship = ShipManager.Unpack(ShipManager.templates[template]);
        ship.faction = faction;
        //ship.name = ship.faction.name + " " + ship.name;
        for (var i = 0; i < 6; i++ ) {
            CrewManager.Create(ship: ship, faction: ship.faction);
        }
        if (sector != null)
            sector.PlaceShip(ship, (Vector2)sectorPos);
        else
            ship.galaxyPos = Game.galaxy.RandomPosition();
        ShipManager.Add(ship);
        return ship;
    }

    public static Ship Unpack(ShipData data) {
        var ship = new Ship();
        ship.name = data.name;

        foreach (var blockData in data.blocks) {
            var block = BlockManager.Deserialize(blockData);
            ship.blocks[blockData.x, blockData.y, block.layer] = block;
        }
        
        foreach (var blockData in data.blueprintBlocks) {
            var block = new BlueprintBlock(BlockManager.Deserialize(blockData));
            ship.blueprintBlocks[blockData.x, blockData.y, block.layer] = block;
        }
        return ship;
    }

    public static ShipData Pack(Ship ship) {
        var data = new ShipData();
        data.name = ship.name;

        data.blocks = new BlockData[ship.blocks.allBlocks.Count];
        data.blueprintBlocks = new BlockData[ship.blueprintBlocks.allBlocks.Count];
        
		var i = 0;
		foreach (var block in ship.blocks.allBlocks) {
			data.blocks[i] = BlockManager.Serialize(block);
			i += 1;
		}

		i = 0;
		foreach (var block in ship.blueprintBlocks.allBlocks) {
			data.blueprintBlocks[i] = BlockManager.Serialize(block);
			i += 1;
		}

        return data;
    }
}

[Serializable]
public class ShipData {
    public string name;
    /*public Vector2 position;
    public Quaternion rotation;
    public Vector2 velocity;
    public Vector3 angularVelocity;*/
    public BlockData[] blocks;
    public BlockData[] blueprintBlocks;
    public string sectorId;
}


public class Ship : IOpinionable {
    public string name;
    public string nameWithColor {
        get { return name; }
    }

    public BlockMap blocks;
    public BlockMap blueprintBlocks;
    public List<Crew> crew = new List<Crew>();
    public float scrapAvailable = 0f;
    public float jumpSpeed = 10f;
    public GalaxyPos galaxyPos;
    public Vector2 sectorPos;
    public Sector sector;
    public Sector destSector;
    public Faction faction = null;
    public Crew captain {
        get {
            return crew.First();
        }
    }

    public ShipStrategy strategy;
    public Blockform form = null;
    public JumpShip jumpShip = null;
    public Dictionary<Ship, Disposition> localDisposition = new Dictionary<Ship, Disposition>();

    public bool isStationary {
        get { return !blocks.Find<Thruster>().Any(); }
    }

    public bool inTransit {
        get { return destSector != null; }
    }

    public Ship() {
        crew = new List<Crew>();
        strategy = new ShipStrategy(this);
        blocks = new BlockMap(this);
        blueprintBlocks = new BlockMap(this);
        blocks.OnBlockAdded += OnBlockAdded;
    }

    public Blockform LoadBlockform() {
        var blockform = Pool.For("Blockform").Attach<Blockform>(Game.activeSector.contents);
        blockform.Initialize(this);
        this.form = blockform;
        return blockform;
    }

    public void OnBlockAdded(Block newBlock) {
        newBlock.ship = this;
    }

    public void FoldJump(Sector destSector) {
        Debug.LogFormat("Jumping to: {0}", destSector);
        this.destSector = destSector;

        if (sector != null)
            sector.ships.Remove(this);
        sector = null;
    }

    public void Simulate(float deltaTime) {
        // don't simulate realized ships
        if (form != null) return;
        strategy.Simulate();

        if (destSector != null) {
            var targetDir = (destSector.galaxyPos.vec - galaxyPos.vec).normalized;
            var dist = targetDir * jumpSpeed * deltaTime;
            
            if (Vector2.Distance(destSector.galaxyPos, galaxyPos) < dist.magnitude) {
                destSector.JumpEnterShip(this, destSector.galaxyPos.vec - galaxyPos.vec);
            } else {
                galaxyPos = new GalaxyPos(null, galaxyPos.vec + dist);
            }
        }

        if (jumpShip != null) jumpShip.SyncShip();
    }
    
    public void SetBlock(IntVector2 pos, BlockType type) {
        var block = new Block(type);
        blocks[pos, block.layer] = block;
        var block2 = new BlueprintBlock(type);
        blueprintBlocks[pos, block2.layer] = block2;
    }
    
    public void SetBlock(int x, int y, BlockType type) {
        var block = new Block(type);
        blocks[x, y, block.layer] = block;
        var block2 = new BlueprintBlock(type);
        blueprintBlocks[x, y, block2.layer] = block2;
    }

    public Disposition DispositionTowards(Ship other) {
        if (localDisposition.ContainsKey(other))
            return localDisposition[other];

        if (captain == null) return Disposition.neutral;

        return Disposition.FromOpinion(captain.opinion[other]);
    }

}

