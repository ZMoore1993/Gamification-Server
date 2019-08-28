using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;


#region structs
public struct coin
{
    public int A_upgradeLv;
    public int P_upgradeLv;
    public int active;
    public int passive;
    public int numBooster;
    public int boosterRemain;
    public double boosterRate;
}
public struct exp
{
    public int A_upgradeLv;
    public int P_upgradeLv;
    public double cur;
    public int numBooster;
    public int boosterRemain;
    public double boosterRate;
}

public struct prog
{
    public int A_upgradeLv;
    public int P_upgradeLv;
    public double cur;
    public double max;
    public int numBooster;
    public int boosterRemain;
    public double boosterRate;
}

public struct stam
{
    public static double _cur = 15;
    public static double _max = 15;
    public double cur
    {
        get
        {
            return _cur;
        }
        set
        {
            if (value >= _max)
            {
                _cur = _max;
            }
            else
                _cur = value;
        }
    }
    public double max
    {
        get
        {
            return _max;
        }
    }
}

public struct timeLeft
{
    public static double _cur = 60 * 60;
    public static double _max = 60 * 60;

    public double cur
    {
        get
        {
            return _cur;
        }
        set
        {
            if (value >= _max)
            {
                _cur = _max;
            }
            else
                _cur = value;
        }
    }
    public double max
    {
        get
        {
            return _max;
        }
    }
}

#endregion
public class IncrementalData
{
    public bool startNew = true;
    public bool needTutorial = true;
    public coin coin = new coin { active = 0, A_upgradeLv = 1, boosterRate = 1, boosterRemain = 0, numBooster = 0, passive = 0, P_upgradeLv = 1 };
    public exp exp = new exp { A_upgradeLv = 1, boosterRate = 1, boosterRemain = 0, numBooster = 0, P_upgradeLv = 1 };
    public prog progress = new prog { A_upgradeLv = 1, boosterRate = 1, boosterRemain = 0, numBooster = 0, P_upgradeLv = 1 };
    public stam stamina;
    public timeLeft timeleft;
    public int lv = 1;
    public string username = "";
    public int permanentPerksLV = 1;
    string _DateTimeStr = "";
    public bool startMessageDisplayed = false;
    public DateTime lastLogOut = DateTime.Now;
    public bool passive = false;
    public bool gameON = false;
    public minigame currentGame;
    public minigame nextGame;
    public bool debugging = false;
    public int activePlayingTimeLeft = 16; //16 hours
    public int passivePlayingTimeLeft = 16 * 7; //16weeks
    public string titleString = "";
    public string titleCollectionStr = "";
    public bool[] titleCollection = new bool[50];
    public int LogOutpassiveProgressGained = 0;
    public List<string> usedCode = new List<string>(); 
}

public enum game
{
    incremental, seeker, mastermind, conquer
}

public enum minigame
{
    none,
    seeker,
    conquer,
    mastermind
}

