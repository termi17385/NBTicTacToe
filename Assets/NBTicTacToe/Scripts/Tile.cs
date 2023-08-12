using NBTicTacToe.Game.Manager;
using System;
using System.Collections;
using UnityEngine;

namespace NBTicTacToe.Game
{
    public class Tile : MonoBehaviour
    {
        [Header("Set up")]
        [SerializeField] private Vector2Int tileId;

        [Header("Selection Colours")]
        [SerializeField] private Color onMouseOver;
        [SerializeField] private Color onClick, onSelectedBoard, normalColor;
        [SerializeField] private SpriteRenderer tileRenderer;

        public Board board;

        public Vector2Int GetTileID => tileId;
        public void SetID(Vector2Int _id) => tileId = _id;
        public SpriteRenderer Renderer;

        private bool ColorChange => GameManager.Instance.ChoosingBoard || board.Selected;
        private bool Blocked => board.GetBoard[tileId.x, tileId.y].blocked;
        private bool mouseOver, clicked;

        private void Awake()
        {
            onMouseOver.a = 255;
            onClick.a = 255;
            onSelectedBoard.a = 255;

            normalColor = tileRenderer.color;
        }

        private void LateUpdate()
        {
            if (Blocked) tileRenderer.color = normalColor;
            else
            {
                if (ColorChange && !mouseOver)
                {
                    tileRenderer.color = onSelectedBoard;
                }
                else if (!mouseOver)
                {
                    tileRenderer.color = normalColor;
                }
            }
        }

        private void OnMouseDown()
        {
            if (Blocked) return;

            if(GameManager.Instance.ChoosingBoard) GameManager.Instance.SetBoard(board.GetID);
            GameManager.Instance.TileInteraction(tileId);
            if(ColorChange)
            {
                clicked = true;
                tileRenderer.color = onClick;
                StartCoroutine(ResetColor());
            }
        }

        private IEnumerator ResetColor()
        {
            yield return new WaitForSeconds(.05f);
            tileRenderer.color = normalColor;
            clicked = false;
        }

        private void OnMouseOver()
        {
            if (Blocked) return;

            if(ColorChange && !clicked) tileRenderer.color = onMouseOver;
            mouseOver = true;
        }

        private void OnMouseExit()
        {
            tileRenderer.color = normalColor;
            mouseOver = false;
        }
    }
}