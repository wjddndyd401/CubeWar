using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent (typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    ObjectController shooter;
    public GameObject collisionEffect;
    public float collisionScale;
    public GameObject particle;
    GameObject instParticle;

    ObjectController target;
    Vector3 startPosition;
    Vector3 destination;

    int damage;
    public Splash splashType;
    public float splashRange;

    float launchedTime;
    float xVelocity, yVelocity;

    void Start()
    {
        if (particle != null)
            instParticle = Instantiate(particle, transform.position, Quaternion.identity);
    }

    void Update()
    {
        if (target != null && target.gameObject != null)
        {
            // 타겟이 움직이면 따라가야지?
            destination = target.transform.position;
        }

        /* 곡사포 포물선 운동 처리 */
        // 평면(x, z축) 기준 남은 시간을 계산한다
        float time = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(destination.x, destination.z)) / xVelocity;
        if (Mathf.Abs(time) < 0.01f)
        {
            // 예외처리 : x, z 좌표가 목표와 일치하면 직선으로 목표로 향한다.
            transform.position = Vector3.MoveTowards(transform.position, destination, xVelocity * Time.deltaTime);            
        }
        else
        {
            // x,z축으로는 직선 전진
            Vector2 horizontalPosition = Vector2.MoveTowards(new Vector2(transform.position.x, transform.position.z), new Vector2(destination.x, destination.z), xVelocity * Time.deltaTime);

            // y축으론 남은 거리, 남은 시간, 현재 y축 기준 속도를 이용하여 수직방향 가속도를 계산한다. (가속도 기본 공식 s = vt + 1/2at^2 활용)
            float accel = 2 * (transform.position.y - destination.y + time * yVelocity) / Mathf.Pow(time, 2);

            // 계산된 가속도만큼 y축 속력을 조절하고, xz축 속력과 같이 오브젝트 포지션에 적용한다.
            yVelocity -= accel * Time.deltaTime;
            float verticalPosition = transform.position.y + yVelocity * Time.deltaTime;
            Vector3 nextPosition = new Vector3(horizontalPosition.x, verticalPosition, horizontalPosition.y);
            transform.LookAt(nextPosition);
            transform.position = nextPosition;
        }
        if (instParticle != null)
            instParticle.transform.position = transform.position;

        // 충돌이 없더라도 목표 지점에 근접하면 투사체 파괴
        if ((transform.position - destination).sqrMagnitude <= 0.01f)
        {
            if(target == null)
            {
                Explode();
            }
        }
    }

    // 충돌 감지
    private void OnTriggerEnter(Collider col)
    {
        if (target != null && col.gameObject == target.gameObject)
        {
            target.TakeDamage(damage, shooter);
            Explode();
        }
    }

    private void Explode()
    {
        if (collisionEffect != null)
        {
            GameObject newEffect = Instantiate(collisionEffect, transform.position, Quaternion.identity);
        }
        Destroy(gameObject);
    }

    // 초기값 설정. 투사체 생성시엔 반드시 이 메서드를 실행해야 한다.
    public void SetBullet(ObjectController shooter, ObjectController target, float xVelocity, float angle, Vector3 startPosition, int damage)
    {
        this.shooter = shooter;
        this.target = target;
        this.startPosition = startPosition;
        destination = target.transform.position;
        this.damage = damage;

        // 입력된 투사체 속력은 xz 평면에 적용하며, y축 속력은 각도와 xz 평면 속력을 이용해 계산한다.
        this.xVelocity = xVelocity;

        // 입력된 각도에 공격 주체와 타겟의 각도를 더한다. 즉 입력 각도가 0이라면 투사체는 공격 주체로부터 타겟을 향해 직진.
        // 각도는 최대 75도까지만 가능
        if (angle < 0) angle = 0;
        if (angle > 75) angle = 75;
        float newAngle = angle * Mathf.Deg2Rad + Mathf.Atan((destination.y - startPosition.y) / Vector3.Distance(startPosition, destination));

        // y축 최초 속력 계산
        yVelocity = Mathf.Tan(newAngle) * xVelocity;

        launchedTime = Time.time;
    }
}
