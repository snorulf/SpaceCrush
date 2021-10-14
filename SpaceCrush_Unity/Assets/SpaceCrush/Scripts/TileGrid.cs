using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TileGrid : MonoBehaviour
{

    private Tile[,] allTiles;
    [SerializeField] private GameObject tileBasePrefab;

    private List<Tile> CheckMatches(List<Tile> movedTiles)
    {
        var matches = new HashSet<Tile>();
        for (int i = 0; i < movedTiles.Count; i++)
        {
            var tile = movedTiles[i];
            if (tile.Popped)
            {
                continue;
            }

            if (tile.CheckRowMatches())
            {
                matches.Add(tile);

                var leftTile = tile.left;
                while (tile.MatchType(leftTile))
                {
                    matches.Add(leftTile);
                    leftTile = leftTile.left;
                }

                var rightTile = tile.right;
                while (tile.MatchType(rightTile))
                {
                    matches.Add(rightTile);
                    rightTile = rightTile.right;
                }
            }
        }

        UnityEngine.Assertions.Assert.IsFalse(matches.Any(x => x.Popped == true), "A match has already been popped so something is not right here.");
        UnityEngine.Assertions.Assert.IsTrue(matches.Count == 0 || matches.Count >= 3, "Invalid number of matches!");
        return matches.ToList();
    }

    public void ResetTiles()
    {
        for (int column = 0; column < allTiles.GetLength(0); column++)
        {
            for (int row = 0; row < allTiles.GetLength(1); row++)
            {
                var tile = allTiles[column, row];
                tile.ResetTile();
            }
        }
        LinkTiles();
    }

    public void PopulateGrid(int columns, int rows)
    {
        allTiles = new Tile[columns, rows];

        //FIXME: Seperate linking of tiles and tile type creation

        for (int row = 0; row < rows; row++)
        {
            Tile leftNeighbour = null;
            for (int column = 0; column < columns; column++)
            {
                // Set up the tile
                GameObject tileGO = Instantiate(tileBasePrefab, transform);
                Transform tileGoTransform = tileGO.transform;
                tileGoTransform.localPosition = new Vector3((float)column, (float)row, 0.0f);
                tileGO.name = "[" + column + "," + row + "]";

                //FIXME: Data should be se in a Tile constructor. Component is then added to gameobject.

                Tile tile = tileGO.GetComponent<Tile>();

                allTiles[column, row] = tile;

                tile.columnIndex = column;

                // Link up with the neigbours
                tile.left = leftNeighbour;
                if (leftNeighbour != null)
                {
                    leftNeighbour.right = tile;
                }
                leftNeighbour = tile;

                if (row != 0)
                {
                    allTiles[column, row - 1].top = tile;
                    tile.bottom = allTiles[column, row - 1];
                }

                // Randomize a tile type and make sure it is not 3-row match
                var randType = RandomizeTileTypeExcept();
                if (randType == tile?.left?.Type && randType == tile?.left?.left?.Type)
                {
                    randType = RandomizeTileTypeExcept(randType);
                }

                tile.Type = randType;

                tile.initPos = tileGoTransform.localPosition;
                tile.initRot = tileGoTransform.localRotation;
            }
        }
    }

    private void LinkTiles()
    {
        for (int row = 0; row < allTiles.GetLength(1); row++)
        {
            Tile leftNeighbour = null;
            for (int column = 0; column < allTiles.GetLength(0); column++)
            {

                Tile tile = allTiles[column, row];

                // Link up with the neigbours
                tile.left = leftNeighbour;
                if (leftNeighbour != null)
                {
                    leftNeighbour.right = tile;
                }
                leftNeighbour = tile;

                if (row != 0)
                {
                    allTiles[column, row - 1].top = tile;
                    tile.bottom = allTiles[column, row - 1];
                }
            }
        }
    }

    private Tile.TileType RandomizeTileTypeExcept(Tile.TileType except = Tile.TileType.Unknown)
    {
        var randomType = except;
        int typeCount = (int)Enum.GetValues(typeof(Tile.TileType)).Cast<Tile.TileType>().Max();
        UnityEngine.Assertions.Assert.IsTrue(typeCount > 1);
        while (randomType == except)
        {
            randomType = (Tile.TileType)UnityEngine.Random.Range(0, typeCount);
        }
        return randomType;
    }

    public void SetEmissive(List<Tile> tiles, bool enable)
    {
        for (int i = 0; tiles.Count > i; i++)
        {
            tiles[i].SetEmissive(enable);
        }
    }

    public struct TurnResult
    {
        public readonly List<Tile> matches;
        public readonly float duration;

        public TurnResult(List<Tile> matches, float duration)
        {
            this.matches = matches;
            this.duration = duration;
        }
    }

    public TurnResult PopTile(Tile tile)
    {
        return PopTiles(new List<Tile>() { tile });
    }

    public TurnResult PopTiles(List<Tile> tiles)
    {
        Debug.Log("<color=cyan>PopTiles: pop " + tiles.Count + " tiles</color>");
        var movedTiles = new List<Tile>();
        for (int i = 0; i < tiles.Count; i++)
        {
            tiles[i].Pop();
            // Get top tiles the popped tile, i.e the once that actually moved.
            movedTiles.AddRange(tiles[i].GetTopTiles());
        }

        // Get max duration from the moved tiles.
        float maxDuration = movedTiles.Count > 0 ? movedTiles.Max(x => x.moveDuration) : 0.0f;

        Debug.Log("<color=orange>Moved tiles: " + movedTiles.Count + "</color>");

        return new TurnResult(CheckMatches(movedTiles), maxDuration);
    }
}