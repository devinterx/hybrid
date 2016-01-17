using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class ActiveSector : MonoBehaviour {
    private Transform _contents;
    public Transform contents {
        get {
            if (_contents == null) {
                _contents = Pool.For("Holder").Attach<Transform>(transform);
                _contents.name = "Contents";
            }

            return _contents;
        }
    }

    private Transform _transients;
    public Transform transients {
        get {
            if (_transients == null) {
                _transients = Pool.For("Holder").Attach<Transform>(transform);
                _transients.name = "Transients";
            }
            
            return _transients;
        }
    }

    public float radius {
        get {
            return 100f;
        }
    }

    public HashSet<Blockform> blockforms = new HashSet<Blockform>();

    public bool IsOutsideBounds(Vector3 pos) {
        return pos.magnitude > radius;
    }

    public IObjective[] objectives = new IObjective[] { };

    public void Load() {
        Game.playerShip.transform.SetParent(contents);
        Game.playerShip.gameObject.SetActive(true);
        objectives = Game.state.GetComponentsInChildren<IObjective>();

        for (var i = 0; i < 20; i++) {
            var pos = new Vector2(Random.Range(-radius, radius), Random.Range(-radius, radius));
            var asteroidRadius = Random.Range(10, 15);
            if (Physics.OverlapSphere(pos, asteroidRadius*Tile.worldSize).Length == 0) {
                Generate.Asteroid(pos, asteroidRadius);
            }
        }

        var ship = Blockform.FromTemplate(ShipTemplate2.FromId("Little Frigate"));
        ship.transform.position = new Vector2(radius, 0);
        ((BoardObjective)objectives[0]).target = ship;
 //       var reactor = ship.blocks.Find<Reactor>().First();
 //       reactor.health = reactor.type.maxHealth/2.0f;
    }

    public void Unload() {
        Game.playerShip.transform.SetParent(Game.state.transform);
        Game.playerShip.gameObject.SetActive(false);
        blockforms.Clear();
        Pool.Recycle(transients.gameObject);
        Pool.Recycle(contents.gameObject);
    }

}
