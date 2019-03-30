using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Global
{
    public static readonly Vector3 zeroVector3 = Vector3.zero;
    public static readonly int mapBaseLossyScale = 10;
    public static Color myColor = Color.green;
    public static Color allyColor = Color.yellow;
    public static Color enemyColor = Color.red;
    public static List<string> players;
    public static List<List<string>> team;
    public static string playerName;
    public static readonly float uiWidthHeightRadio = Screen.width * 150f / 800f / Screen.height;
    public static readonly float AirUnitHeight = 6;

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
    public static Team Relation(string a, string b)
    {
        if (a == "" || b == "") return Team.Ally;
        if (a == b) return Team.Mine;
        for (int j = 0; j < team.Count; j++)
        {
            if (team[j].Contains(a) && team[j].Contains(b)) return Team.Ally;
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
}

public enum Team { Mine, Enemy, Ally };

public class Command
{
    public enum Info { None, Stop, Attack, Hold, Produce, Build, Spell }
    public Info info;
    public Unit unit;
    public Structure structure;
    // Spell spell;

    public bool Equal(Command others)
    {
        if (info != others.info) return false;
        if (info == Info.Produce && (unit.unitName == others.unit.unitName)) return false;
        if (info == Info.Build && (structure.unitName == others.structure.unitName)) return false;
        return true;
    }
}

public enum Splash { None, Straight, Circle };