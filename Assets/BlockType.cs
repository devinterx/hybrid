using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class BlockAbility : PoolBehaviour {
    public HashSet<Block> blocks;
    public string key;

    public KeyCode keyCode {
        get {
            return (KeyCode)System.Enum.Parse(typeof(KeyCode), key);
        }
    }

    public virtual bool WorksWith(Block block) {
        return false;
    }
}

public class BlockComponent : PoolBehaviour {
    [NonSerialized]
    public Block block;
   

    public Blockform form;

    public virtual void OnNewBlock(Block block) { }
    public virtual void OnRealize() { }
}

public class BlockType : MonoBehaviour {
    [Tooltip("The mass value of each block is added to the mass of a ship rigidBody.")]
    public float mass;

    public float value = 1f;

    static List<BlockType> all = new List<BlockType>();
    static SensibleDictionary<string, BlockType> byId = new SensibleDictionary<string, BlockType>();

    public static void LoadTypes() {
        foreach (var type in Game.LoadPrefabs<BlockType>("Blocks")) {
            type.tileable = Tile.tileablesByName[type.GetComponent<SpriteRenderer>().sprite.texture.name];
            type.tileable.tileWidth = Mathf.RoundToInt(type.transform.localScale.x);
            type.tileable.tileHeight = Mathf.RoundToInt(type.transform.localScale.y);
            BlockType.byId[type.id] = type;
            BlockType.all.Add(type);
        }
    }

    public static BlockType FromId(string id) {
        if (BlockType.byId.Keys.Count == 0)
            LoadTypes();        

        return BlockType.byId[id];
    }

    public static List<BlockType> All {
        get {
            if (BlockType.all.Count == 0)
                LoadTypes();

            return BlockType.all.ToList();
        }
    }

    public string id { 
        get { return name; }
    }

    public string savePath {
        get { return "foo"; }
    }

    public int scrapRequired = 30;
    public float maxHealth = 1;
    public float damageBuffer = 0;

    [Header("Description")]
    [TextArea]
    public string descriptionHeader;
    [TextArea]
    public string descriptionBody;

    public Tileable tileable;

    public BlockLayer blockLayer;

    public Sprite sprite {
        get {
            return GetComponent<SpriteRenderer>().sprite;
        }
    }

    public bool canRotate = false;
    public bool canFitInsideWall = false;
	public bool canBlockSight = false;
    public bool canBlockFront = false;

    public BlockAbility[] abilities;

    /* Complex block specific functionality */
    public bool isComplexBlock = false;
    public bool showInMenu = false;
    public bool isWeapon = false;

    [Tooltip("Whether a block requires an attached console with an active crew member to function.")]
    public bool needsCrew = false;

    [HideInInspector]
    public SpriteRenderer spriteRenderer;

    [NonSerialized]
    private BlockComponent[] _blockComponents;
    public BlockComponent[] blockComponents {
        get {
            if (_blockComponents == null) {
                _blockComponents = GetComponents<BlockComponent>();
            }
            return _blockComponents;
        }
    }

    public void Awake() {   
        Destroy(this);
    }
}