using NBTicTacToe.Game.Manager.BoardGeneration;
using NBTicTacToe.Game.Rules;
using TMPro;
using UnityEngine;

public enum Turn
{
    CROSS,
    NAUGHT,
    none
}

namespace NBTicTacToe.Game.Manager
{
    public class GameManager : MonoBehaviour
    {
        const string WINTEXTPARTONE = "Game Over\r\n'";
        const string WINTEXTPARTTWO = "'\r\nIs The Winner";

        #region Variables
        [Header("For Win Condition")]
        [SerializeField] private GameObject winScreenOBJ;
        [SerializeField] private TextMeshProUGUI winScreen;
        private Turn winningTurn;

        [Header("For Gameplay")]
        [SerializeField] private Transform board_prefab;
        [SerializeField] private BoardGenerator boardGenerator = new BoardGenerator();
        [SerializeField] private Turn currentTurn = Turn.CROSS;
        [SerializeField] private bool debugging;
        const int TILECOUNT = 3;
        #endregion
        #region Board Variables
        private GAMETILE<Board>[,] boards;
        private GAMETILE<Board> currentBoard;
        #endregion
        #region Properties
        private static GameManager instance;

        public bool GameOver { get; private set; }
        public static GameManager Instance => instance;
        public bool ChoosingBoard { get; private set; }
        #endregion
        #region Setup
        private void Awake()
        {
            TileType.Initialize();
            instance = this;

            ChoosingBoard = true;
            BoardSetup();
        }

        private void BoardSetup()
        {
            boards = boardGenerator.GenerateBoards(board_prefab, TILECOUNT);
            transform.position = boards[1, 1].tile.transform.position;
            boardGenerator.SetBoardParents(boards, this.transform);
            transform.position = Vector3.zero;
        }
        #endregion

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R)) ResetBoards();
        }

        private void HandleWinCondition()
        {
            winScreen.text = WINTEXTPARTONE + winningTurn.ToString() + WINTEXTPARTTWO;
            winScreenOBJ.SetActive(true);
        }

        private void ResetBoards()
        {
            winScreenOBJ.SetActive(false);
            GameOver = false;
            ChoosingBoard = true;
            boardGenerator.ResetBoards(boards);
        }

        #region Gameplay
        public void TileInteraction(Vector2Int _id)
        {
            Turn turn = currentTurn;
            if (!GameOver) HandleBoard(_id);

            if (GameOver = Ruleset.WinCondition(boards))
            {
                Debug.Log($"Game Won By: {turn}");
                currentBoard.tile.BoardDeselected();
                winningTurn = turn;
                HandleWinCondition();
            }
        }                

        private void HandleBoard(Vector2Int _id)
        {
            if (currentBoard.tile == null) currentBoard = boards[_id.x, _id.y];
            else
            {
                if (currentBoard.tile.GetBoard[_id.x, _id.y].blocked) return;
                else currentBoard.tile.BoardDeselected();
            }

            ChoosingBoard = false;
            PlaceTile(_id);
            currentBoard = boards[_id.x, _id.y];
            currentBoard.tile.BoardSelected();

            if (Ruleset.BoardFull(currentBoard.tile.GetBoard))
            {
                currentBoard.tile.BoardDeselected();
                ChoosingBoard = true;
            }
        }

        private void PlaceTile(Vector2Int _id)
        {
            if (currentBoard.tile.GetBoard[_id.x, _id.y].blocked) return;

            Turn turn = currentTurn;
            Sprite sprite = null;

            switch (currentTurn)
            {
                case Turn.CROSS:
                    sprite = TileType.GetCross();
                    break;
                case Turn.NAUGHT:
                    sprite = TileType.GetNaught();
                    break;
            }

            currentBoard.tile.SetTile(sprite, _id, turn);
            currentTurn = NextTurn(currentTurn);

            // Sets the Win Graphic
            if (Ruleset.WinCondition(currentBoard.tile.GetBoard) && !currentBoard.tile.BoardWon)
            {
                currentBoard.tile.SetWin(sprite, turn);
                currentBoard.turnPlayed = turn;
                boards[currentBoard.tile.GetID.x, currentBoard.tile.GetID.y] = currentBoard;
                Debug.Log($"Board[{currentBoard.tile.GetID.x},{currentBoard.tile.GetID.y}] won by: " + turn);
            }
        }

        private Turn NextTurn(Turn _t)
        {
            if (debugging) return Turn.CROSS;
            return _t == Turn.CROSS ? Turn.NAUGHT : Turn.CROSS;
        }

        internal void SetBoard(Vector2Int getID) => currentBoard = boards[getID.x, getID.y];
        #endregion
    }
}

namespace NBTicTacToe.Game.Manager.BoardGeneration
{
    [System.Serializable] public class BoardGenerator
    {
        const string PATH = "Tile";

        public GAMETILE<Board>[,] GenerateBoards(Transform _prefab, int _count)
        {
            GAMETILE<Board>[,] boards = new GAMETILE<Board>[_count, _count];
            for(int x = 0; x < _count; x++)
            {
                for(int y = 0; y < _count; y++)
                {
                    Vector2Int position = new(x * 3, y * 3);
                    Board newBoard = SpawnBoard(_prefab, position);

                    // store board in array
                    boards[x,y] = new GAMETILE<Board>(newBoard, false, Turn.none);
                    // generate the current board
                    newBoard.GenerateBoard(position, x, y);
                }
            }
            return boards;
        }

        internal void ResetBoards(GAMETILE<Board>[,] _boards)
        {
            int length = _boards.GetLength(0);
            for(int x = 0; x < length; x++)
            {
                for(int y = 0; y < length; y++)
                {
                    GAMETILE<Board> board = _boards[x, y];
                    board.tile.ResetBoard();
                    board.blocked = false;
                    board.turnPlayed = Turn.none;
                    _boards[x, y] = board;
                }
            }
        }

        internal void SetBoardParents(GAMETILE<Board>[,] _boards, Transform _t)
        {
            foreach (GAMETILE<Board> tile in _boards)
                tile.tile.transform.SetParent(_t);
        }

        private Board SpawnBoard(Transform _prefab, Vector2Int _position) =>
            Object.Instantiate(_prefab, (Vector2)_position * 3, Quaternion.identity).GetComponent<Board>();
    }
}

namespace NBTicTacToe.Game.Rules
{
    public class Ruleset
    {
        // 0,2|1,2|2,2
        // 0,1|1,1|2,1
        // 0,0|1,0|2,0

        // Center Positions
        private static Vector2Int TOP_CENTER = new Vector2Int(1, 2);
        private static Vector2Int CENTER = new Vector2Int(1, 1);
        private static Vector2Int BOTTOM_CENTER = new Vector2Int(1, 0);

        // Left
        private static Vector2Int TOP_LEFT = new Vector2Int(0, 2);
        private static Vector2Int LEFT = new Vector2Int(0, 1);
        private static Vector2Int BOTTOM_LEFT = new Vector2Int(0, 0);

        // Right
        private static Vector2Int TOP_RIGHT = new Vector2Int(2, 2);
        private static Vector2Int RIGHT = new Vector2Int(2, 1);
        private static Vector2Int BOTTOM_RIGHT = new Vector2Int(2, 0);

        /// <summary>
        /// Takes in a board, either the whole 9 board or a single board
        /// and checks to see if we have a win on our hands
        /// </summary>
        public static bool WinCondition<T>(GAMETILE<T>[,] _board) where T : MonoBehaviour =>
            // Rows
            _board[CENTER.x, CENTER.y].turnPlayed == _board[LEFT.x, LEFT.y].turnPlayed
                && _board[LEFT.x, LEFT.y].turnPlayed == _board[RIGHT.x, RIGHT.y].turnPlayed
                && _board[CENTER.x, CENTER.y].turnPlayed != Turn.none

                // Bottom Row
                || _board[BOTTOM_LEFT.x, BOTTOM_LEFT.y].turnPlayed == _board[BOTTOM_CENTER.x, BOTTOM_CENTER.y].turnPlayed
                && _board[BOTTOM_CENTER.x, BOTTOM_CENTER.y].turnPlayed == _board[BOTTOM_RIGHT.x, BOTTOM_RIGHT.y].turnPlayed
                && _board[BOTTOM_LEFT.x, BOTTOM_LEFT.y].turnPlayed != Turn.none

                // Top Row
                || _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed == _board[TOP_CENTER.x, TOP_CENTER.y].turnPlayed
                && _board[TOP_CENTER.x, TOP_CENTER.y].turnPlayed == _board[TOP_LEFT.x, TOP_LEFT.y].turnPlayed
                && _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed != Turn.none

                // Left Column
                || _board[TOP_LEFT.x, TOP_LEFT.y].turnPlayed == _board[LEFT.x, LEFT.y].turnPlayed
                && _board[LEFT.x, LEFT.y].turnPlayed == _board[BOTTOM_LEFT.x, BOTTOM_LEFT.y].turnPlayed
                && _board[TOP_LEFT.x, TOP_LEFT.y].turnPlayed != Turn.none

                // Middle Column
                || _board[TOP_CENTER.x, TOP_CENTER.y].turnPlayed == _board[CENTER.x, CENTER.y].turnPlayed
                && _board[CENTER.x, CENTER.y].turnPlayed == _board[BOTTOM_CENTER.x, BOTTOM_CENTER.y].turnPlayed
                && _board[TOP_CENTER.x, TOP_CENTER.y].turnPlayed != Turn.none

                // Right Column
                || _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed == _board[RIGHT.x, RIGHT.y].turnPlayed
                && _board[RIGHT.x, RIGHT.y].turnPlayed == _board[BOTTOM_RIGHT.x, BOTTOM_RIGHT.y].turnPlayed
                && _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed != Turn.none

                // Diagonal One
                || _board[TOP_LEFT.x, TOP_LEFT.y].turnPlayed == _board[CENTER.x, CENTER.y].turnPlayed
                && _board[CENTER.x, CENTER.y].turnPlayed == _board[BOTTOM_RIGHT.x, BOTTOM_RIGHT.y].turnPlayed
                && _board[TOP_LEFT.x, TOP_LEFT.y].turnPlayed != Turn.none

                // Diagonal Two
                || _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed == _board[CENTER.x, CENTER.y].turnPlayed
                && _board[CENTER.x, CENTER.y].turnPlayed == _board[BOTTOM_LEFT.x, BOTTOM_LEFT.y].turnPlayed
                && _board[TOP_RIGHT.x, TOP_RIGHT.y].turnPlayed != Turn.none;

        public static bool BoardFull<T>(GAMETILE<T>[,] _board) where T : MonoBehaviour
        {
            foreach (GAMETILE<T> board in _board)
                if (!board.blocked) return false;

            return true;
        }
    }
}