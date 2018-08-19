using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


/**
 * 1.create a unity 2D project
 * 2.copy scripts to you project folder
 * 3.create a gameobject,and attach example.cs to it
 * 4.play
 */
public class Example : MonoBehaviour
{
    enum TileType
    {
        none,
        wall,
    }

    public int width = 15;      //tile map width
    public int height = 12;     //tile map height
    public int obstacleFillPercent = 30;    //tile map obstacle fill percent
    public float scale = 32f;

    Sprite tilePrefab;
    string message = "";

    List<int> passableValues;

    GameObject allMapTiles;     //the map and tiles
    GameObject player;          //the player
    GameObject goal;            //the goal



    void Start()
    {
        Camera.main.orthographicSize = scale * 10;
        Camera.main.gameObject.transform.position = new Vector3(width * scale / 2, height * scale / 2, -10);
        tilePrefab = Sprite.Create(new Texture2D((int)scale, (int)scale), new Rect(0, 0, scale, scale), new Vector2(0.5f, 0.5f), 1f);

        goal = new GameObject("goal");
        goal.AddComponent<SpriteRenderer>();
        goal.GetComponent<SpriteRenderer>().sprite = tilePrefab;
        goal.GetComponent<SpriteRenderer>().color = Color.yellow;
        goal.GetComponent<SpriteRenderer>().sortingOrder = 1;

        player = new GameObject("player");
        player.AddComponent<SpriteRenderer>();
        player.GetComponent<SpriteRenderer>().sprite = tilePrefab;
        player.GetComponent<SpriteRenderer>().color = Color.red;
        player.GetComponent<SpriteRenderer>().sortingOrder = 2;


        passableValues = new List<int>();
        passableValues.Add((int)TileType.none);
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(10, 10, 150, 30), "pathfinding 4"))
        {
            message = "finding...";
            simPathFinding4();
        }
        if (GUI.Button(new Rect(10, 50, 150, 30), "pathfinding 6X"))
        {
            message = "finding...";
            simPathFinding6();
        }
        if (GUI.Button(new Rect(10, 90, 150, 30), "pathfinding 6Y"))
        {
            message = "finding...";
            simPathFinding6(false);
        }

        GUI.Label(new Rect(180, 20, 300, 30), message);
    }

    /**
     * simulate path finding in grid tilemaps
     */
    public void simPathFinding4()
    {
        StopAllCoroutines();

        //init map
        var map = mapToDict4(generateMapArray(width, height));
        float xScale = scale;
        float yScale = scale;
        renderMap(map, xScale, yScale);

        //init player and goal
        var playerPos = new Vector2Int(0, 0);
        map[playerPos] = (int)TileType.none;
        setTransformPosition(player.transform, playerPos, xScale, yScale);
        var goalPos = new Vector2Int(width - 1, height - 1);
        map[goalPos] = (int)TileType.none;
        setTransformPosition(goal.transform, goalPos, xScale, yScale);

        //finding
        var path = PathFinding2D.find4(playerPos, goalPos, map, passableValues);
        if (path.Count == 0)
        {
            message = "oops! cant find goal";
        }
        else
        {
            StartCoroutine(movePlayer(path, xScale, yScale, .2f));
        }
    }

    /**
     * simulate path finding in hexagonal grid tilemaps
     */
    public void simPathFinding6(bool staggerByRow = true)
    {
        StopAllCoroutines();

        //init map
        var map = mapToDict6(generateMapArray(width, height), staggerByRow);
        var hexScale = scale + 4f; //addtional 4f makes tiles seperated
        float xScale = staggerByRow ? hexScale / 2 : hexScale;
        float yScale = staggerByRow ? hexScale : hexScale/2;
        renderMap(map, xScale, yScale);

        //init player and goal
        var mapPoses = map.Keys.ToList();
        mapPoses.Sort((a, b) => a.x + a.y - b.x - b.y);
        var playerPos = mapPoses.First();
        map[playerPos] = (int)TileType.none;
        setTransformPosition(player.transform, playerPos, xScale, yScale);
        var goalPos = mapPoses.Last();
        map[goalPos] = (int)TileType.none;
        setTransformPosition(goal.transform, goalPos, xScale, yScale);

        //find
        List<Vector2Int> path;
        if (staggerByRow) {
            path = PathFinding2D.find6X(playerPos, goalPos, map, passableValues);
        } else {
            path = PathFinding2D.find6Y(playerPos, goalPos, map, passableValues);
        }
        if (path.Count == 0)
        {
            message = "oops! cant find goal";
        }
        else
        {
            StartCoroutine(movePlayer(path, xScale, yScale, .2f));
        }
    }


    void setTransformPosition(Transform trans, Vector2Int pos, float xScale, float yScale)
    {
        trans.position = new Vector3(pos.x * xScale, pos.y * yScale, 0);
    }

    void renderMap(Dictionary<Vector2Int, int> map, float xScale, float yScale)
    {
        Destroy(allMapTiles);
        allMapTiles = new GameObject("allMapTiles");
        foreach (var item in map)
        {
            GameObject temp = new GameObject();
            temp.transform.position = new Vector3(item.Key.x * xScale, item.Key.y * yScale, 0);
            SpriteRenderer spr = temp.AddComponent<SpriteRenderer>();
            spr.sprite = tilePrefab;
            switch (item.Value)
            {
                case (int)TileType.none:
                    spr.color = Color.white;
                    break;
                case (int)TileType.wall:
                    spr.color = Color.black;
                    break;
            }
            temp.transform.parent = allMapTiles.transform;
        }
    }

    IEnumerator movePlayer(List<Vector2Int> path, float xScale, float yScale, float interval = 0.1f)
    {
        foreach(var item in path) {
            setTransformPosition(player.transform, item, xScale, yScale);
            yield return new WaitForSeconds(interval);
        }
   
        message = "reach goal !";
    }

    int[,] generateMapArray(int pwidth, int pheight)
    {
        var mapArray = new int[pwidth, pheight];
        for (int x = 0; x < pwidth; x++)
        {
            for (int y = 0; y < pheight; y++)
            {
                mapArray[x, y] = Random.Range(0, 100) < obstacleFillPercent ? (int)TileType.wall : (int)TileType.none;
            }
        }
        return mapArray;
    }

    Dictionary<Vector2Int, int> mapToDict4(int[,] mapArray)
    {
        Dictionary<Vector2Int, int> mapDict = new Dictionary<Vector2Int, int>();
        for (int x = 0; x < mapArray.GetLength(0); x++)
        {
            for (int y = 0; y < mapArray.GetLength(1); y++)
            {
                mapDict.Add(new Vector2Int(x, y), mapArray[x, y]);
            }
        }
        return mapDict;
    }

    Dictionary<Vector2Int, int> mapToDict6(int[,] mapArray, bool stretchRow)
    {
        Dictionary<Vector2Int, int> mapDict = new Dictionary<Vector2Int, int>();
        for (int x = 0; x < mapArray.GetLength(0); x++)
        {
            for (int y = 0; y < mapArray.GetLength(1); y++)
            {
                if (stretchRow)
                {
                    if (y % 2 == 0)
                    {
                        mapDict.Add(new Vector2Int(2 * x, y), mapArray[x, y]);
                    }
                    else
                    {
                        mapDict.Add(new Vector2Int(2 * x + 1, y), mapArray[x, y]);
                    }
                }
                else
                {
                    if (x % 2 == 0)
                    {
                        mapDict.Add(new Vector2Int(x, 2 * y), mapArray[x, y]);
                    }
                    else
                    {
                        mapDict.Add(new Vector2Int(x, 2 * y + 1), mapArray[x, y]);
                    }
                }

            }
        }
        return mapDict;
    }
}