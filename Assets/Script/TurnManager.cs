using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

/*
 * Player Manager
 * - berisi player-player yang akan dimulai dibegin game sesuai match id
 */

public class TurnManager : NetworkBehaviour
{
    List<Player> players = new List<Player>();

    public void AddPlayer(Player player)
    {
        players.Add(player);
    }
}
