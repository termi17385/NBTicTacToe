using NBTicTacToe.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using UnityEngine;

public class TileType
{
    const string PATH = "T_Sprite_Sheet";
    static Sprite s_cross, s_naught;

    public static void Initialize()
    {
        Sprite[] spriteSheet = Resources.LoadAll<Sprite>(PATH);

        s_naught = spriteSheet[0];
        s_cross = spriteSheet[1];
    }

    public static Sprite GetCross() => s_cross;   
    public static Sprite GetNaught() => s_naught;   
}

[Serializable] public struct GAMETILE<T> where T : MonoBehaviour
{
    public T tile;
    public bool blocked;
    public Turn turnPlayed;

    public GAMETILE(T tile, bool blocked, Turn turn)
    {
        this.tile = tile;
        this.blocked = blocked;
        this.turnPlayed = turn;
    }
}

public class Board : MonoBehaviour
{
    [SerializeField] private Tile tile;
    [SerializeField] private SpriteRenderer winTile;
    [SerializeField] private GAMETILE<Tile>[,] board = new GAMETILE<Tile>[3,3];

    public GAMETILE<Tile>[,] GetBoard => board;
    public bool BoardWon { get; private set; }

    private Vector2 BOARDSIZE = new(2.8f, 2.8f);
    private Vector2 TILESIZE = new(.9f, .9f);
    private const int TILECOUNT = 3;
    public bool Selected { get; private set; }
    public Vector2Int GetID { get; private set; }

    #region Board Setup
    public void GenerateBoard(Vector2Int _position, int _x, int _y)
    {
        BoardWon = false; 
        this.transform.localScale = Vector3.one * 3;
        for(int x = 0; x < TILECOUNT; x++) 
        {
            for(int y = 0; y < TILECOUNT; y++)
            {
                // Calculate the position to center the tile on the board
                Tile newTile = SpawnTile(new Vector2Int(x,y));
                newTile.transform.localScale = TILESIZE;
                
                if(x == 1 && y == 1) transform.position = newTile.transform.position;
                board[x,y] = new GAMETILE<Tile>(newTile, false, Turn.none);
                newTile.SetID(new Vector2Int(x,y));
                newTile.board = this;
            }
        }

        SetParent();
        transform.position = (Vector2)_position;
        transform.localScale = BOARDSIZE;
        GetID = new Vector2Int(_x, _y);
    }

    private void SetParent()
    {
        foreach(GAMETILE<Tile> tile in board)
            tile.tile.transform.SetParent(transform);
    }
    private Tile SpawnTile(Vector2Int _position) => Instantiate(tile, (Vector2)_position, Quaternion.identity);
    #endregion

    #region Gameplay
    /// <summary>
    /// Informs the board which tile was played,
    /// and sets the sprite.
    /// </summary>
    /// <returns></returns>
    public bool SetTile(Sprite _sprite, Vector2Int _id, Turn _turn)
    {
        bool valid = false;
        GAMETILE<Tile> temp_tile = board[_id.x, _id.y];
        if(!temp_tile.blocked)
        {
            // handle tile placement logic
            temp_tile.tile.Renderer.sprite = _sprite;
            temp_tile.blocked = true;
            temp_tile.turnPlayed = _turn;
            valid = true;
        }

        board[_id.x, _id.y] = temp_tile;
        return valid;
    }
    public void SetWin(Sprite _sprite, Turn _winningTurn)
    {
        winTile.sprite = _sprite;
        BoardWon = true;
    }
    public void BoardSelected()
    {
        winTile.gameObject.SetActive(false);
        Selected = true;
    }

    public void BoardDeselected()
    {
        winTile.gameObject.SetActive(true);
        Selected = false;
    }

    public void ResetBoard()
    {
        winTile.sprite = null;
        BoardWon = false;
        Selected = false;

        for (int x = 0; x < TILECOUNT; x++)
        {
            for (int y = 0; y < TILECOUNT; y++)
            {
                GAMETILE<Tile> tile = board[x, y];
                tile.tile.Renderer.sprite = null;
                tile.blocked = false;
                tile.turnPlayed = Turn.none;
                board[x, y] = tile;
            }
        }
    }
    #endregion
}
