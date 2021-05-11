using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Karakter
{
    public Brave brave;
    public Indigo indigo;
    public Mechanic mechanic;
    public Escaper escaper;
    public int type;

    public Karakter(int i)
    {
        setCharacter(i);
    }
    public dynamic getCharacter()
    {
        if (type == 1)
        {
            return brave;
        }
        else if (type == 2)
        {
            return indigo;
        }
        else if (type == 3)
        {
            return mechanic;
        }
        else if (type == 4)
        {
            return escaper;
        }
        return indigo;
    }
    public void setCharacter(int i)
    {
        if (i == 1)
        {
            brave = new Brave();
        }
        else if (i == 2)
        {
            indigo = new Indigo();
        }
        else if (i == 3)
        {
            mechanic = new Mechanic();
        }
        else if (i == 4)
        {
            escaper = new Escaper();
        }
        type = i;
    }
}


