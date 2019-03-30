using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : ObjectController
{
    public float speed;             // 이동속도
    public float angularSpeed;      // 회전속도 (최대 360)
    protected float stoppingDistance = 0.3f;

    Vector3 finalPosition;          // 최종 목표 위치 저장 (현재 동작 완료 후 이동할 목표 지점)

    private void Awake()
    {
        finalPosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();

    }

    public override void SetEnableCommand(bool enable)
    {
        base.SetEnableCommand(enable);

        if (enable && Attackable())
            commandList[2].info = Command.Info.Hold;
    }


    protected override void Update()
    {
        base.Update();

        switch (currentState)
        {
            case State.Idle:
                break;
            case State.Hold:
                break;
            case State.Attack:
                // 정지
                break;
            case State.Moving:
            case State.Patrol:
                // 이동
                break;
        }

        // 목적지 도달하면 멈추기
        if (currentState == State.Moving || currentState == State.Patrol)
        {
            StopInStoppingDistance();
        }
    }

    //--------------------- 명령들 ---------------------
    public override void Move(Vector3 point)
    {
        Stop();
        finalPosition = point;
        ChangeState(State.Moving);
        MoveToPosition(point);
    }

    public override void Stop()
    {
        base.Stop();
        finalPosition = transform.position;
    }

    public override void Hold()
    {
        BeforeCommand();
        Stop();
        ChangeState(State.Hold);
    }

    public override void AttackMove(Vector3 point)
    {
        BeforeCommand();
        Stop();
        targetForForcedAttack = null;
        finalPosition = point;
        ChangeState(State.Patrol);
        MoveToPosition(point);
    }

    //--------------------- 명령 이외의 것들 ---------------------
    protected virtual void MoveToPosition(Vector3 point)
    {
        if (currentState != State.Moving) ChangeState(State.Patrol);
    }

    protected virtual void StopInStoppingDistance()
    {

    }

    public override void DamageToTarget()
    {
        if (targetForAttack.gameObject != this.gameObject)
        {
            Vector3 targetPosition = targetForAttack.transform.position;
            targetPosition.y = transform.position.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), angularSpeed * Mathf.Deg2Rad * Time.deltaTime);
            base.DamageToTarget();
        }
    }

    public override void StopMovingBeforeAutoAttack()
    {
        // 공격 전 경로 진행을 초기화하는 함수
        // 부모 클래스의 AutoAttack에서 호출
        //if (pathFinder.isActiveAndEnabled) pathFinder.ResetPath();
    }

    public override void MoveForAttackTarget()
    {
        // 공격 대상인 적이 사정거리 밖에 있을 경우
        // 부모 클래스의 AutoAttack에서 호출
        base.MoveForAttackTarget();
        if (currentState != State.Hold && targetForAttack != null)
        {
            MoveToPosition(targetForAttack.transform.position);
        }
    }

    public override void AfterTargetDeath()
    {
        // 공격 대상이 없을 때
        // 부모 클래스의 AutoAttack에서 호출
        if (currentState != State.Idle && currentState != State.Hold) MoveToPosition(finalPosition);
    }

    protected override void ChangeState(State state)
    {
        base.ChangeState(state);

        if (state == State.Moving)
            targetForAttack = null;
    }

    protected bool StateIsMoving(State state)
    {
        if (currentState == State.Moving || currentState == State.Patrol) return true;
        else return false;
    }

    public override bool IsUnit()
    {
        return true;
    }
}
