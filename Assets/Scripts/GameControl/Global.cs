using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Global
{
    public static readonly Vector3 zeroVector3 = Vector3.zero;
    public static readonly int mapBaseLossyScale = 10;
    public static readonly Color myColor = Color.green;
    public static readonly Color allyColor = Color.yellow;
    public static readonly Color enemyColor = Color.red;
    public static readonly float uiWidthHeightRadio = Screen.width * 150f / 800f / Screen.height;
    public static readonly float AirUnitHeight = 6;
    public static List<Player> playerList;
    public static List<List<Player>> teamList;
    public static Player gamePlayer;
    public static Player enemyPlayer;

    public static bool Equal(float a, float b)
    {
        if (Mathf.Abs(a - b) > Mathf.Epsilon) return false;
        else return true;
    }

    public static bool Equal(Vector3 a, Vector3 b)
    {
        if (Mathf.Abs(a.x - b.x) > Mathf.Epsilon) return false;
        else if (Mathf.Abs(a.y - b.y) > Mathf.Epsilon) return false;
        else if (Mathf.Abs(a.z - b.z) > Mathf.Epsilon) return false;
        else return true;
    }

    /**********************************************************
     * 두 플레이어의 관계 확인
     * 파라미터 a, b : 비교할 플레이어명
     *********************************************************/
    public static Team Relation(Player a, Player b)
    {
        if (a == null || b == null) return Team.Ally;
        if (a == b) return Team.Mine;
        for (int j = 0; j < teamList.Count; j++)
        {
            if (teamList[j].Contains(a) && teamList[j].Contains(b)) return Team.Ally;
        }
        return Team.Enemy;
    }

    public static Color GetRelationColor(Team team)
    {
        if (team == Team.Mine) return Color.green;
        else if (team == Team.Enemy) return Color.red;
        else return Color.yellow;
    }

    public static float SqrDistanceOfTwoUnit(ObjectController a, ObjectController b)
    {
        Vector3 newA = new Vector3(a.transform.position.x, 0, a.transform.position.z);
        Vector3 newB = new Vector3(b.transform.position.x, 0, b.transform.position.z);

        return (newA - newB).sqrMagnitude;
    }

    public static float SqrDistanceOfTwoUnit(Collider a, Collider b)
    {
        Vector3 newA = new Vector3(a.transform.position.x, 0, a.transform.position.z);
        Vector3 newB = new Vector3(b.transform.position.x, 0, b.transform.position.z);

        return (newA - newB).sqrMagnitude;
    }

    public static Player FindPlayerWithName(string name)
    {
        for (int i = 0; i < playerList.Count; i++)
        {
            if (playerList[i].name == name) return playerList[i];
        }
        return null;
    }
}

public enum Team { Mine, Enemy, Ally };

public class Command
{
    public enum Info { None, Stop, Attack, Hold, Produce, Build, Spell }
    public Info info;
    public Unit unit;
    public Structure structure;

    public bool Equal(Command others)
    {
        if (info != others.info) return false;
        if (info == Info.Produce && (unit.unitName == others.unit.unitName)) return false;
        if (info == Info.Build && (structure.unitName == others.structure.unitName)) return false;
        return true;
    }
}

public class Player
{
    public string name;
    public Color color;
}

public enum Splash { None, Straight, Circle };