using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public class UIPlayer : MonoBehaviour
{
    [SerializeField] Text text;

    Player player;

    public void setPlayer(Player player)
    {
        this.player = player;
        Debug.Log(player.playerIndex);
        text.text = "Player " + player.playerIndex.ToString();
    }
}
