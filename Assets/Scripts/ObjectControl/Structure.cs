using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshObstacle))]
public class Structure : ObjectController
{
    public Unit[] produceList;
    public event System.Action<int, Vector3, ObjectController, Player, bool, Vector3> Produce;
    List<Unit> producingQueue;
    public event System.Action<Unit, int, List<Unit>> CheckResource;
    readonly int maxProducingQueueSize = 5;
    float startProduceTime;
    public bool isTopPriority;
    public bool isLastPriority;

    public Structure[] buildList;
    public event System.Action<Structure, Player> Build;

    public Vector3 rallyPoint;
    public bool hasRallyPoint = false;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        producingQueue = new List<Unit>();
    }

    public override void SetEnableCommand(bool enable)
    {
        base.SetEnableCommand(enable);

        if (enable)
        {
            int commandIndex = 0;

            while (commandList[commandIndex].info != Command.Info.None) commandIndex++;

            for (int i = 0; i < produceList.Length; i++)
            {
                commandList[commandIndex].info = Command.Info.Produce;
                commandList[commandIndex].unit = produceList[i];
                commandIndex++;
            }

            for (int i = 0; i < buildList.Length; i++)
            {
                commandList[commandIndex].info = Command.Info.Build;
                commandList[commandIndex].structure = buildList[i];
                commandIndex++;
            }
        }
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        for (int i = 0; i < produceList.Length; i++)
        {
            if (isSelected && isTopPriority && onReceiveCommand && Input.GetKeyDown(produceList[i].shortcut))
            {
                StartProduceSelectedUnit(produceList[i]);
            }
        }

        for (int i = 0; i < buildList.Length; i++)
        {
            if (isSelected && isTopPriority && onReceiveCommand && Input.GetKeyDown(buildList[i].shortcut))
            {
                BuildSelectedStructure(buildList[i]);
            }
        }

        if (producingQueue.Count > 0 && Time.time - startProduceTime >= producingQueue[0].produceTime)
        {
            Produce?.Invoke(1, transform.position, producingQueue[0], owner, hasRallyPoint, rallyPoint);
            producingQueue.RemoveAt(0);
            startProduceTime = Time.time;
        }
    }

    public void SetRallyPoint(Vector3 point)
    {
        if(produceList.Length > 0)
        {
            if (Global.Equal(point, transform.position))
            {
                hasRallyPoint = false;
            }
            else
            {
                hasRallyPoint = true;
                rallyPoint = point;
            }
        }
    }

    public Vector3 GetRallyPoint()
    {
        return rallyPoint;
    }

    public void StartProduceSelectedUnit(Unit unit)
    {
        if (producingQueue.Count < maxProducingQueueSize)
        {
            if (producingQueue.Count == 0) startProduceTime = Time.time;
            producingQueue.Add(unit);
            CheckResource(unit, -unit.resource, producingQueue);
        }
    }

    public void CancelProducing(int index)
    {
        if (index >= 0)
        {
            CheckResource(producingQueue[index], producingQueue[index].resource, producingQueue);
            producingQueue.RemoveAt(index);
            if (index == 0) startProduceTime = Time.time;
        }
    }

    public void CancelLastProducing()
    {
        if(producingQueue.Count > 0)
        {
            int index = producingQueue.Count - 1;
            CheckResource(producingQueue[index], producingQueue[index].resource, producingQueue);
            producingQueue.RemoveAt(index);
        }
    }

    public void BuildSelectedStructure(Structure unit)
    {
        Build?.Invoke(unit, owner);
    }

    public void GetProduceList(out float produceProgress, out List<Unit> list)
    {
        if (producingQueue.Count > 0) produceProgress = (Time.time - startProduceTime) / producingQueue[0].produceTime;
        else produceProgress = 0;
        list = producingQueue;
    }

    public Unit GetProducingUnit(int index)
    {
        if (index < producingQueue.Count) return producingQueue[index];
        else return null;
    }

    public int ProducingUnitCount()
    {
        return producingQueue.Count;
    }

    public override bool IsStructure()
    {
        return true;
    }
}
