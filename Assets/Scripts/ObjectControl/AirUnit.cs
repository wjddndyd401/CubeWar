using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class AirUnit : Unit
{
    float remainingDistance;
    bool isStopped;
    Vector3 airDestination;

    protected override void Start()
    {
        base.Start();

        transform.position = new Vector3(transform.position.x, Global.AirUnitHeight, transform.position.z);
        airDestination = transform.position;
        isStopped = true;
        isOnAir = true;
    }

    protected override void Update()
    {
        base.Update();

        if (!isStopped)
        {
            if ((transform.position - airDestination).sqrMagnitude >= stoppingDistance * stoppingDistance)
            {
                transform.position = Vector3.MoveTowards(transform.position, airDestination, speed * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(airDestination - transform.position), angularSpeed * Mathf.Deg2Rad * Time.deltaTime);
            }
        }
        else
        {
            transform.position = new Vector3(transform.position.x + Random.Range(-0.001f, 0.001f), Global.AirUnitHeight, transform.position.z + Random.Range(-0.001f, 0.001f));
            airDestination = transform.position;
        }

        remainingDistance = Vector3.Distance(transform.position, airDestination);
    }

    public override void Stop()
    {
        base.Stop();

        isStopped = true;
    }

    protected override void MoveToPosition(Vector3 point)
    {
        base.MoveToPosition(point);
       
        airDestination = new Vector3(point.x, Global.AirUnitHeight, point.z);
        remainingDistance = Vector3.Distance(transform.position, airDestination);
        isStopped = false;
    }

    protected override void ChangeState(State state)
    {
        base.ChangeState(state);
    }

    protected override void StopInStoppingDistance()
    {
        base.StopInStoppingDistance();

        if (remainingDistance <= stoppingDistance)
        {
            Stop();
        }
    }

    public override void StopMovingBeforeAutoAttack()
    {
        isStopped = true;
    }

    private void OnTriggerStay(Collider col)
    {
        if(col.CompareTag("SelectableObject"))
        {
            Vector3 evadeCollisionDistance = (transform.position - col.transform.position).normalized * 1f * Time.deltaTime;
            evadeCollisionDistance.y = 0;
            transform.position += evadeCollisionDistance;
        }
    }
}
