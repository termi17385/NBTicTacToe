using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Color nonActive, normal;
    [SerializeField] private Image x, o;

    public void SetColor(Turn _turn)
    {
        switch (_turn)
        {
            case Turn.CROSS:
                x.color = normal;
                o.color = nonActive;
                break;
            case Turn.NAUGHT:
                x.color = nonActive;
                o.color = normal;
                break;
            case Turn.none:
                x.color = nonActive;
                o.color = nonActive;
                break;
        }
    }
}