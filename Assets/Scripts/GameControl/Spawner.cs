using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public SpawnList[] spawnList;
    public int spawnIndex = 0;
    public float repeatLastEnemyTime;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    // 게임 진행 시간이 다음 스폰 시간에 이르렀는가를 반환
    public bool IsAboutTime(float progressTime)
    {
        if (spawnList[spawnIndex].spawnTime < progressTime) return true;
        else return false;
    }

    // 다음 스폰될 유닛들 정보와 수량 반환
    public void GetSpawnUnit(out Unit[] unit, out int[] number)
    {
        unit = new Unit[spawnList[spawnIndex].units.Length];
        number = new int[spawnList[spawnIndex].units.Length];
        for (int i = 0; i < spawnList[spawnIndex].units.Length; i++)
        {
            unit[i] = spawnList[spawnIndex].units[i].unit;
            number[i] = spawnList[spawnIndex].units[i].number;
        }
    }

    // 다음 스폰 대기
    public void SetNextSpawn()
    {
        if (spawnIndex < spawnList.Length-1) spawnIndex++;
        else spawnList[spawnIndex].spawnTime += repeatLastEnemyTime;
    }
}

[System.Serializable]
public struct SpawnUnit
{
    public Unit unit;
    public int number;
};

[System.Serializable]
public struct SpawnList
{
    public SpawnUnit[] units;
    public float spawnTime;
};