using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

/*
 * Command Line untuk Server
 * - berisi fungsi yang akan dipanggil sesuai perintah cmd di server
 * -- scene untuk mendapatkan scene aktif diserver
 * -- kick untuk menendang user atau membuat user disconnect
 */

public class ServerManager : NetworkBehaviour
{
    string command;
    // Start is called before the first frame update
    void Start()
    {
        if (isServer)
        {
            command = string.Empty;
            DontDestroyOnLoad(this);
        }
    }

    public void ClearLine()
    {
        Console.CursorLeft = 0;
        Console.Write(new String(' ', Console.BufferWidth));
        Console.CursorLeft = 0;
    }

    void RunCommand()
    {
        Debug.Log("Kamu mejalankan command : "+command);
        string[] perintah = command.Split(' ');
        if (perintah[0] == "scene")
        {
            Debug.Log(SceneManager.GetActiveScene().name);
        }
        if (perintah[0] == "kick")
        {
            int playerIndex=0;
            if (!int.TryParse(perintah[2], out playerIndex))
            {
                Debug.Log(perintah[2] + " is not an integer");
                // Whatever
            }
            else
            {
                if (playerIndex > 0)
                {
                    for (int i = 0; i < MatchMaker.instance.matches.Count; i++)
                    {
                        if (MatchMaker.instance.matches[i].matchId.ToUpper() == perintah[1].ToUpper())
                        {
                            for (int j = 0; j < MatchMaker.instance.matches[i].players.Count; j++)
                            {
                                if (MatchMaker.instance.matches[i].players[j].GetComponent<Player>().playerIndex == int.Parse(perintah[2]))
                                {

                                    MatchMaker.instance.matches[i].players[j].GetComponent<NetworkIdentity>().connectionToClient.Disconnect();
                                    Debug.Log("Kamu menkick player " + perintah[2] + " dari match id " + perintah[1]);

                                    break;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
        command = string.Empty;

    }

    // Update is called once per frame
    void Update()
    {
        if (isServer)
        {
            if (Console.KeyAvailable)
            {
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Backspace)
                {
                    command = command.Substring(0, command.Length - 1);
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    command = string.Empty;
                    ClearLine();
                }
                else if (key.Key == ConsoleKey.Enter)
                {
                    RunCommand();
                }
                else
                {
                    if (key.KeyChar != '\u0000')
                    {
                        command += key.KeyChar;
                    }
                }
            }

            /*Debug.Log(Console.ReadKey().Key);
            if(Console.ReadKey().Key == ConsoleKey.Backspace)
            {
                command = command.Substring(0, command.Length - 1);
            }
            if (Console.ReadKey().Key == ConsoleKey.Escape)
            {
                command = string.Empty;
                ClearLine();
            }
            if (Console.ReadKey().Key == ConsoleKey.Enter)
            {
                RunCommand();
            }
            command += Console.ReadKey().KeyChar;
            */
        }
    }
}
