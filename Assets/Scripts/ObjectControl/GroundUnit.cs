using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Diagnostics;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NavMeshObstacle))]
public class GroundUnit : Unit
{
    NavMeshAgent pathFinder;
    NavMeshObstacle pathObstacle;
    NavMeshPath path;

    public Vector3 nextPosition;
    public Vector3 preNextPosition;

    protected override void Start()
    {
        base.Start();

        pathFinder = GetComponent<NavMeshAgent>();
        pathFinder.avoidancePriority = 50;
        pathFinder.speed = speed;
        pathFinder.angularSpeed = angularSpeed;
        pathFinder.stoppingDistance = stoppingDistance * pathFinder.radius / 0.7f;

        pathObstacle = GetComponent<NavMeshObstacle>();
        pathObstacle.carving = true;
        pathObstacle.carveOnlyStationary = false;

        pathObstacle.enabled = false;
        pathFinder.enabled = true;

        path = new NavMeshPath();
    }

    protected override void Update()
    {
        base.Update();

        switch (currentState)
        {
            case State.Idle:
            case State.Hold:
                pathFinder.enabled = false;
                pathObstacle.enabled = true;
                break;
            case State.Attack:
                pathFinder.avoidancePriority = 40;
                // 정지
                break;
            case State.Moving:
            case State.Patrol:
                // 이동
                bool isStartMoveFrame = pathObstacle.enabled;
                pathObstacle.enabled = false;

                // From 'Nav Mesh Obstacle' Component Reference Document (https://docs.unity3d.com/Documentation/Manual/class-NavMeshObstacle.html)
                // 내비메시 쿼리 메서드를 사용하는 경우 내비메시 장애물을 변경한 후 이 변경 사항이 내비메시에 영향을 미칠 때까지 1프레임이 지연됨을 감안해야 합니다.
                // When using NavMesh query methods, you should take into account that there is a one-frame delay between changing a Nav Mesh Obstacle and the effect that change has on the NavMesh.

                // 따라서 이동 시작 프레임에서는 이동하지 않는다. 안 그러면 자기 자신한테 막혀서 순간이동함.
                if (!isStartMoveFrame)
                {
                    pathFinder.enabled = true;
                    pathFinder.avoidancePriority = 50;
                    if (!pathFinder.hasPath || !Global.Equal(nextPosition, preNextPosition))
                    {
                        pathFinder.CalculatePath(nextPosition, path);
                        if (path.status == NavMeshPathStatus.PathInvalid)
                        {
                            // Path가 성립되지 않을 경우, 가장 가까운 접점을 찾는다
                            NavMesh.SamplePosition(nextPosition, out NavMeshHit hit, 1000, NavMesh.AllAreas);
                            // 찾은 접점으로 다시 계산
                            pathFinder.CalculatePath(hit.position, path);
                        }
                        pathFinder.SetPath(path);
                        preNextPosition = nextPosition;
                   }
                }

                break;
        }
    }

    public override void Stop()
    {
        base.Stop();
    }

    protected override void MoveToPosition(Vector3 point)
    {
        base.MoveToPosition(point);

        nextPosition = point;
    }

    protected override void ChangeState(State state)
    {
        base.ChangeState(state);

        if(state == State.Idle && pathFinder != null && pathFinder.isActiveAndEnabled)
            pathFinder.ResetPath();
    }

    protected override void StopInStoppingDistance()
    {
        base.StopInStoppingDistance();

        if (pathFinder.isActiveAndEnabled && pathFinder.hasPath && !pathFinder.pathPending && pathFinder.remainingDistance <= pathFinder.stoppingDistance)
        {
            Stop();
        }
    }

    public override void StopMovingBeforeAutoAttack()
    {
        if(pathFinder.isActiveAndEnabled)
            pathFinder.ResetPath();
    }
}
