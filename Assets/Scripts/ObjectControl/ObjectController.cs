using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(LineRenderer))]
public class ObjectController : MonoBehaviour
{
    public string unitName;         // 이름
    public Texture thumbnail;       // 초상화??
    public int hitPoint;            // 피통
    public int resource;            // 생산 소비 자원
    public float produceTime;       // 생산 시간
    public KeyCode shortcut;        // 생산 단축키
    public bool groundAttackable;   // 지상 공격 가능 여부
    public bool airAttackable;      // 공중 공격 가능 여부
    public int damage;              // 공격 피해량
    public float attackCooldown;    // 공격 간 쿨타임 (후딜 끝나고 기다리는 시간)
    public float startupTime;       // 공격 선딜
    public float recoveryTime;      // 공격 후딜
    public float sight;             // 시야
    public int armor;               // 방어력
    public float range;             // 사정거리
    public GameObject attackEffect;      // 공격 시 공격 시작 지점에서 발생하는 이펙트
    public GameObject bullet;       // 투사체(없으면 근접공격임)
    public float bulletSpeed;       // 투사체 이동속도
    public float bulletAngle;       // 투사체 각도
    public Splash splashType;       // 스플래시 종류 (없음, 직선형, 원형) 
    public float splashRange;       // 스플래시 범위
    public float effectSize;   // 이펙트 크기
    public Quaternion baseRotationAngle = Quaternion.Euler(0, 225, 0);  // 유닛의 기본 각도

    int currentHitPoint;
    float currentHitPointFloat;
    [HideInInspector]
    public bool isOnAir = false;

    public enum State { Idle, Hold, Moving, Attack, Patrol };
    public State currentState = State.Idle;

    protected Command[] commandList;

    protected bool isSelected;
    static Transform mTransform;
    public string ownerName;
    protected Player owner;
    private LineRenderer line;
    BoxCollider boxCollider;

    protected ObjectController targetForAttack = null;
    protected ObjectController targetForForcedAttack = null;
    float bulletLaunchTime;
    float endAttackTime;
    float attackStartTime;
    float nextAttackTime;
    List<Transform> muzzle;

    public event System.Action<ObjectController> Death;
    
    public bool onReceiveCommand = true;
    public float objectMakingPercentage = 0;
    float prePercentage = 0;

    protected virtual void Start()
    {
        mTransform = transform;
        transform.rotation = baseRotationAngle;

        if (currentHitPoint == 0)
        {
            currentHitPoint = hitPoint;
            currentHitPointFloat = hitPoint;
        }

        muzzle = new List<Transform>();
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Muzzle")
            {
                muzzle.Add(child);
            }
        }
        
        commandList = new Command[9];
        for (int i = 0; i < commandList.Length; i++)
        {
            commandList[i] = new Command
            {
                info = Command.Info.None
            };
        }

        if (objectMakingPercentage == 0)
        {
            objectMakingPercentage = 1;
            prePercentage = 1;
        }

        SetEnableCommand(onReceiveCommand);

        line = GetComponent<LineRenderer>();
        line.material = new Material(Shader.Find("Standard"));
        line.startWidth = 0.1f;
        line.endWidth = 0.1f;
        line.useWorldSpace = false;
        line.material.color = Color.yellow;

        boxCollider = GetComponent<BoxCollider>();

        SetSelected(false);
    }

    float nextSearchEnemyTime = 0;
    protected virtual void Update()
    {
        mTransform = transform;
        
        if (!Global.Equal(objectMakingPercentage, prePercentage) && objectMakingPercentage < prePercentage)
        {
            currentHitPoint = (int)(objectMakingPercentage * hitPoint);
            currentHitPointFloat = currentHitPoint;
        }
        else
        {
            currentHitPointFloat += (objectMakingPercentage - prePercentage) * hitPoint;
            currentHitPoint = (int)currentHitPointFloat;
        }
        prePercentage = objectMakingPercentage;

        if (currentHitPoint > hitPoint) currentHitPoint = hitPoint;

        if (onReceiveCommand && Attackable())
        {
            if (targetForAttack != null)
            {
                AttackTarget();
            }
            else
            {
                if (currentState != State.Moving)
                {
                    AfterTargetDeath();
                    if (GameManager.progressTime >= nextSearchEnemyTime)
                    {
                        SearchEnemyForAttack();
                        nextSearchEnemyTime = GameManager.progressTime + 0.3f;
                    }
                }
                SetNextAttackStartTime(Time.time);
            }
        }
    }

    public virtual void SetEnableCommand(bool enable)
    {
        onReceiveCommand = enable;
        if (enable)
        {
            int commandIndex = 0;
            while (commandList[commandIndex].info != Command.Info.None) commandIndex++;

            if (Attackable() || IsUnit())
                commandList[commandIndex++].info = Command.Info.Stop;
            if (Attackable())
                commandList[commandIndex++].info = Command.Info.Attack;
        }
        else
        {
            for (int i = 0; commandList != null && i < commandList.Length; i++)
            {
                commandList[i].info = Command.Info.None;
            }
        }
    }

    /**********************************************************
     * 명령 : 이동 (유닛만 가능)
     *********************************************************/
    public virtual void Move(Vector3 point)
    {
        // 유닛에서 오버라이드
        // 건물은 명령이 떨어져도 아무것도 안 함
        BeforeCommand();
    }

    /**********************************************************
     * 명령 : 정지
     *********************************************************/
    public virtual void Stop()
    {
        BeforeCommand();
        ChangeState(State.Idle);
    }

    /**********************************************************
     * 명령 : 위치 사수 (유닛만 가능)
     *********************************************************/
    public virtual void Hold()
    {
        // 유닛에서 오버라이드
        // 건물은 명령이 떨어져도 아무것도 안 함
    }

    /**********************************************************
     * 명령 : 강제 공격
     *********************************************************/
    public void ForcedAttack(ObjectController target)
    {
        if (Attackable(target))
        {
            BeforeCommand();
            if (currentState != State.Attack)
            {
                Stop();
            }
            targetForForcedAttack = target;
            SearchEnemyForAttack();
        }
    }

    /**********************************************************
     * 명령 : 이동 공격 (유닛만 가능)
     *********************************************************/
    public virtual void AttackMove(Vector3 point)
    {
        // 유닛에서 오버라이드
        // 건물은 명령이 떨어져도 아무것도 안 함
    }

    public virtual void BeforeCommand()
    {
        // 모든 명령 실행 전 공통으로 하는 것들
        nextSearchEnemyTime = GameManager.progressTime;
        targetForAttack = null;
        targetForForcedAttack = null;
    }

    //--------------------- 명령 이외의 것들 ---------------------

    void AttackTarget()
    {
        if (Attackable(targetForAttack))
        {
            // 사정거리 안이라면 공격!
            // 유닛이 움직일 수 없는 상태(건물이거나 홀드 상태)일 때 사거리가 같은 유닛에게 일방적으로 맞지 않도록 사거리를 약간 늘린다
            if (Global.SqrDistanceOfTwoUnit(targetForAttack, this) <= Mathf.Pow(range + (currentState == State.Hold || !IsUnit() ? 0.01f : 0), 2))
            {
                // 공격
                StopMovingBeforeAutoAttack();
                DamageToTarget();
            }
            else
            {
                if (GameManager.progressTime >= nextSearchEnemyTime)
                {
                    SearchEnemyForAttack();
                    nextSearchEnemyTime = GameManager.progressTime + 0.3f;
                }
                MoveForAttackTarget();
            }
        }
    }

    /**********************************************************
     * 목표 공격
     * 매 프레임마다 실행되며, 선딜인지 후딜인지 공격 대기 상태인지 판별하여 맞는 동작을 수행한다.
     *********************************************************/
    bool isStartupFinish;
    bool readyToFire;
    public virtual void DamageToTarget()
    {
        ChangeState(State.Attack);
        if (readyToFire)
        {
            // 공격 순간
            if (bullet != null)
            {
                // 투사체가 있을 경우 : 투사체 발사
                for (int i = 0; i < muzzle.Count; i++)
                {
                    GameObject newBullet = Instantiate(bullet, muzzle[i].position, Quaternion.identity);
                    newBullet.GetComponent<Bullet>().SetBullet(this, targetForAttack, bulletSpeed, bulletAngle, muzzle[i].position, damage, splashType, splashRange);
                }
            }
            else
            {
                // 투사체가 없을 경우 : 목표물에 타격 효과 즉시 발생
                if (true)
                {
                    // 폭발 효과가 있을 경우 : 타격 순간 폭발 효과 발생
                }
            }
            nextAttackTime = endAttackTime + attackCooldown;
            readyToFire = false;

            if (attackEffect != null)
            {
                for (int i = 0; i < muzzle.Count; i++)
                {
                    GameObject newEffect = Instantiate(attackEffect, muzzle[i].position, transform.rotation);
                }
            }
        }
        else if (Time.time < bulletLaunchTime)
        {
            // 선딜
            isStartupFinish = true;
        }
        else
        {
            // 선딜 끝났으니 공격준비
            if (isStartupFinish)
            {
                readyToFire = true;
            }
            // 공격은 한 번만 함
            isStartupFinish = false;
        }

        // 후딜
        // 선딜 종료 & 발사 준비 상태 아님 = 공격 완료 -> 후딜 체크 (후딜이 매우 짧을 경우 가끔 공격을 패스하는 현상 수정용)
        if (!isStartupFinish && !readyToFire && Time.time > endAttackTime)
        {
            SetNextAttackStartTime(Time.time + attackCooldown);
        }
    }

    /**********************************************************
     * 자동 공격
     * 주변 적군을 탐지한 다음, 가장 가까운 적군을 공격 대상으로 정한다.
     * 공격 중인 상태를 제외하고 항상 실행한다. 즉 공격 중에는 목표를 바꾸지 않는다.
     *********************************************************/
    void SearchEnemyForAttack()
    {
        if (targetForForcedAttack != null)
        {
            targetForAttack = targetForForcedAttack;
        }
        else
        {
            // 시야 내 적군 감지
            Vector3 capsuleheight = new Vector3(0, Global.AirUnitHeight + sight, 0);
            Collider[] inSightUnits = Physics.OverlapCapsule(transform.position + capsuleheight, transform.position - capsuleheight, sight);
            List<ObjectController> inSightEnemys = new List<ObjectController>();

            // 감지된 유닛들의 소유자를 검색
            for (int i = 0; i < inSightUnits.Length; i++)
            {
                ObjectController target = inSightUnits[i].GetComponent<ObjectController>();
                if (target != null)
                {
                    Player targetOwner = target.owner;

                    // 타겟이 적군인지 확인 (중립이거나 같은 팀의 유닛이라면 적군이 아니다)

                    // 적이면 공격
                    if (Global.Relation(owner, targetOwner) == Team.Enemy && Attackable(target))
                    {
                        inSightEnemys.Add(target);
                    }
                }
            }

            // 감지된 적군 중 가장 가까운 적군을 타겟으로 지정
            if (inSightEnemys.Count > 0)
            {
                targetForAttack = GetClosestEnemy(inSightEnemys);
            }
            else
            {
                targetForAttack = null;
            }
        }
        
    }

    /**********************************************************
     * 공격 전, 경로 탐색을 중지하고 공격에 임한다.
     * 건물에는 해당사항 없음.
     *********************************************************/
    public virtual void StopMovingBeforeAutoAttack()
    {
        // 유닛에서만 오버라이드
    }

    /**********************************************************
     * 자동 공격에 의해 탐지된 적이 사정거리 밖일 경우 해당 적에게 이동한다.
     * 건물에는 해당사항 없음.
     *********************************************************/
    public virtual void MoveForAttackTarget()
    {
        // 공격 대상인 적이 사정거리 밖에 있을 경우
        SetNextAttackStartTime(Time.time);
    }

    /**********************************************************
     * 공격 대상이 죽으면 원 위치로 돌아간다.
     * 이동 중이라면 이동중이던 경로로 계속 이동한다.
     * 건물에는 해당사항 없음.
     *********************************************************/
    public virtual void AfterTargetDeath()
    {
        // 공격 대상이 죽었을 때
        // 유닛에서만 오버라이드
    }

    /**********************************************************
     * 공격 초기화. 다음 공격 관련 정보를 설정한다.
     * 파라미터 time : 다음 공격 준비 시간
     * 다음 공격 준비 시간이 지나지 않았을 경우 = 공격 쿨타임 안 됨 = 쿨타임 끝난 이후로 공격시간 설정
     *********************************************************/
    void SetNextAttackStartTime(float time)
    {
        isStartupFinish = false;
        readyToFire = false;
        if (time >= nextAttackTime)
        {
            attackStartTime = time;
        }
        else
        {
            attackStartTime = nextAttackTime;
        }
        bulletLaunchTime = attackStartTime + startupTime;
        endAttackTime = bulletLaunchTime + recoveryTime;
    }

    /**********************************************************
     * 리스트에서 가장 가까운 오브젝트를 가져온다.
     *********************************************************/
    ObjectController GetClosestEnemy(List<ObjectController> list)
    {
        ObjectController result = list[0];

        for (int i = 1; i < list.Count; i++)
        {
            if (Global.SqrDistanceOfTwoUnit(list[i], this) < Global.SqrDistanceOfTwoUnit(result, this))
            {
                result = list[i];
            }
        }
        return result;
    }

    //--------------------- 상태 설정 ---------------------
    protected virtual void ChangeState(State state)
    {
        currentState = state;
    }

    // 유닛의 선택 상태 여부 설정
    public void SetSelected(bool isSelected)
    {
        this.isSelected = isSelected;
        float xRadius = boxCollider.size.x;
        float zRadius = boxCollider.size.z;

        if (isSelected)
        {
            float x, z;
            float angle = 0;

            line.material.color = Global.GetRelationColor(Global.Relation(Global.gamePlayer, owner));
            line.positionCount = 21;
            for (int i = 0; i < 20 + 1; i++)
            {
                x = Mathf.Cos(Mathf.Deg2Rad * angle) * xRadius;
                z = Mathf.Sin(Mathf.Deg2Rad * angle) * zRadius;

                line.SetPosition(i, new Vector3(x, boxCollider.center.y - boxCollider.size.y / 2 + 0.1f, z));
                angle += 360f / 20f;
            }
        } else
        {
            line.positionCount = 0;
        }
    }

    // 선택 여부 반환
    public bool IsSelected()
    {
        return isSelected;
    }

    // 데미지 입음. HP 0 이하면 죽음.
    public void TakeDamage(int receivedDamage, ObjectController attacker = null)
    {
        if (currentHitPoint > 0)
        {
            int realDamage = receivedDamage - armor;
            if (realDamage < 0) realDamage = 0;

            SetHitPoint(currentHitPoint - realDamage);
            if (currentHitPoint <= 0)
            {
                ExplodeObject();
            } else if(targetForAttack == null && (currentState == State.Patrol || currentState == State.Idle) && attacker != null && Global.Relation(owner, attacker.owner) == Team.Enemy && Attackable(attacker))
            {
                // 자신을 공격한 유닛을 쫓아가는 조건
                // 현재 공격 중인 대상이 없을 것, 기본 또는 정찰 중일 것, 공격 대상이 존재할 것, 공격 대상이 적일 것, 공격 대상을 공격할 수 있을 것.
                AttackMove(attacker.transform.position);
            }
        }
    }

    public void SetHitPoint(int value)
    {
        currentHitPointFloat = value + (currentHitPointFloat - (int)currentHitPointFloat);
        currentHitPoint = value;
    }

    public void ExplodeObject()
    {
        GameObject deathEffect = Resources.Load("Effect/Death Particle") as GameObject;
        if (deathEffect != null)
        {
            for (int i = 0; i < 27; i++)
            {
                GameObject newEffect = Instantiate(deathEffect, transform.position, Quaternion.identity);
                newEffect.transform.localScale *= boxCollider.size.magnitude / Mathf.Sqrt(3);
                newEffect.GetComponent<Rigidbody>().AddForce(new Vector3(Random.Range(-1f, 1f), Random.Range(0f, 1f), Random.Range(-1f, 1f)) * 500);
                newEffect.GetComponent<MeshRenderer>().material = GetComponentsInChildren<MeshRenderer>()[0].material;
            }
        }
        Death(this);
    }

    // 공격 가능 여부 반환
    public bool Attackable(ObjectController target = null)
    {
        if (target != null && target.gameObject == this.gameObject) return false;
        if (groundAttackable && (target == null || !target.isOnAir)) return true;
        if (airAttackable && (target == null || target.isOnAir)) return true;
        return false;
    }

    // UI 상태 정보창에 설정될 정보를 반환한다.
    public void GetStatusForUI(out string _name, out int _hitPoint, out int _currentHitPoint, out int _damage, out int _armor)
    {
        _name = unitName;
        _hitPoint = hitPoint;
        _currentHitPoint = currentHitPoint;
        _damage = damage * muzzle.Count;
        _armor = armor;
    }

    // UI 명령창에 입력될 명령 반환
    public Command[] GetCommandList()
    {
        return commandList;
    }

    // 이거 유닛임 건물임? - 클래스 Unit에선 true로 재정의
    public virtual bool IsUnit()
    {
        return false;
    }

    // 위에것만 있어도 되는데 헷갈려서 같이 만듦 - 클래스 Structure에선 true로 재정의
    public virtual bool IsStructure()
    {
        return false;
    }

    public void SetOwner(Player owner)
    {
        this.owner = owner;
        Transform underBuild = transform.Find("UnderBuildGraphic");
        if(underBuild != null) underBuild.gameObject.SetActive(true);
        foreach (MeshRenderer color in GetComponentsInChildren<MeshRenderer>())
        {
            color.material.color = owner.color;
        }
        if (underBuild != null) underBuild.gameObject.SetActive(false);
    }

    public Player GetOwner()
    {
        return owner;
    }

    public Team team;
}