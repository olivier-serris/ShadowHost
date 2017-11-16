﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Steering : MonoBehaviour {

    static readonly float slowingRadius = 2;

            // component
    protected Rigidbody rb;
    private Puppet puppet;

        // data
    [HideInInspector] public Vector3 OnPlanNormal;
    protected Vector3 steering;
    Vector3 computedVelocity;
    float separationRayFactor = 1f; // 

        // updateFrequencie
    bool updateNeighbours = true;
    Collider[] closeNeighbours;
    Collider[] farawayNeighbours;
    float timer = 0;
    float delay = 0.1f;

    // info : 
    bool isOnGround = false;

    #region getterSetters
    public Vector3 GetSteering
    {
        get
        {
            return steering;
        }
    }
    #endregion
    public void Awake()
    {
        steering = Vector3.zero;
        puppet = GetComponent<Puppet>();
        rb = GetComponent<Rigidbody>();
    }
    public void Start()
    {
#if UNITY_EDITOR
        giz.separateSphereLenght = puppet.Extents.magnitude * separationRayFactor;
#endif
    }
    /// <summary>
    /// Renvoi la velocité finale calculée par le steering 
    /// /!\ Ne dois pas être appellé + d'une fois par fixedUpdate
    /// /!\ Renvois 0 si aucune force de steering
    /// </summary>
    public Vector3 ComputedVelocity
    {
        get
        {
            if (steering == Vector3.zero || !isOnGround)
            {
                EndFrameReset();
                //return Vector3.zero;
                return rb.velocity;
            }

            float ySave = 0; // save y, pour ne pas écraser le fait que l'on tombe
            ySave = rb.velocity.y;
                //On clamp pour que max == acceleration
            steering = Vector3.ClampMagnitude(steering, puppet.stats.Get(Stats.StatType.move_speed));
                // On vire la composante y
            Vector3 velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                // On ajoute l'acceleration
                //velocity += (steering  * puppet.stats.Get(Stats.StatType.maxAcceleration));
            velocity += (steering * 50 * Time.fixedDeltaTime * puppet.stats.Get(Stats.StatType.maxAcceleration));
            // On clamp à maxSpeed
            velocity = Vector3.ClampMagnitude(velocity, puppet.stats.Get(Stats.StatType.move_speed));
            // On recupère la composante y de base
            computedVelocity = new Vector3(velocity.x, ySave + velocity.y, velocity.z);

            EndFrameReset();
            return computedVelocity;
        }
    }
    void EndFrameReset()
    {
#if UNITY_EDITOR
        giz.steeringForce = steering;
#endif
        steering = Vector3.zero;
        timer += Time.fixedDeltaTime;
        if (timer > delay)
        {
            updateNeighbours = true;
            timer = 0;
        }
    }


    public void FixedUpdate()
    {
        GroundGravityCheck();
    }

    #region Basic steering Movement
    public void Seek(Vector3 target, float factor = 1)
    {
        Vector3 desiredVelocityPlan = GetDvOnPlan(target, OnPlanNormal);
        Vector3 force = desiredVelocityPlan - rb.velocity;

        force = Vector3.ClampMagnitude(force, puppet.stats.Get(Stats.StatType.move_speed));
        steering += force * factor;
    }
    public void Flee(Vector3 target, float factor = 1)
    {
        Vector3 desiredVelocityPlan = -GetDvOnPlan(target, OnPlanNormal);
        Vector3 force = desiredVelocityPlan - rb.velocity;

        force = Vector3.ClampMagnitude(force, puppet.stats.Get(Stats.StatType.move_speed));
        steering += force * factor;

    }
    public void Arrival(Vector3 target,float factor = 1)
    {
        Vector3 desiredVelocityPlan = GetDvOnPlan(target, OnPlanNormal);
        float distance = desiredVelocityPlan.magnitude;

        if (desiredVelocityPlan.magnitude < slowingRadius)
        {
            desiredVelocityPlan = desiredVelocityPlan * (distance / slowingRadius);
        }
        Vector3 force = desiredVelocityPlan - rb.velocity;
        force = Vector3.ClampMagnitude(force, puppet.stats.Get(Stats.StatType.move_speed));
        steering += force * factor;
    }
    public void Pursuit(Vector3 target, Vector3 targetVelocity,float factor = 1)
    {
        float T = 0.5f;
        Vector3 futurePosition = target + targetVelocity * T;
        Seek(futurePosition,factor);

    }

    public void Evade(Vector3 target, Vector3 targetVelocity, float factor = 1)
    {
        float T = 0.5f;
        Vector3 futurePosition = target + targetVelocity * T;
        Flee(futurePosition, factor);

    }
    float wanderAngle = 0;
    /*
         Je choisis une direction random. 
         Je cree un cercle ayant pour centre la direction actuelle.
         et un rayon arbitraire. 
         Je choisis un point aléatoire dans ce cercle. 
         Je décide de seek vers cette direction. 
         Ou
         Je choisis une direction random. 
         Je cree un cercle ayant pour centre la direction actuelle.
         et un rayon arbitraire. 
         Je choisis un point aléatoire dans ce cercle. 
         Ma direction actuelle devient cette direction. 
     */
    public void Wander(float factor = 1)
    {
        float wanderT = 30;
        float circleRadius = 1;
        float circleDistance = 2;
        Vector3 circleCenter = transform.position + rb.velocity.normalized * circleDistance;

        wanderAngle += Random.Range(-wanderT, wanderT);
        Vector3 circleOffset = new Vector3(Mathf.Cos(wanderAngle * Mathf.Deg2Rad), 0, Mathf.Sin(wanderAngle * Mathf.Deg2Rad)) * circleRadius;
        Vector3 target = circleCenter + circleOffset;
        Seek(target, factor);
    }
    #endregion
    #region Advanced Steering Movement 
    // Peut être améliorer : Lorsqu'une unité est un milieu d'un gros groupe elle est coincée et vibre.
    // de plus ici on push dans une sphere autour de nous.  Mais ce qui compte c'est surtout d'éviter de vers quoi on avance.
    public void Separation(float factor = 1)
    {
        Collider[] puppetsCollided;
        Vector3 separationForce = Vector3.zero;
        float sphereCheckRadius = puppet.Extents.magnitude * separationRayFactor;
        puppetsCollided = GetNeighbouringPuppet();
        if (puppetsCollided != null)
        {
            int i;
            for (i = 0; i < puppetsCollided.Length; i++)
            {
                if (puppetsCollided[i] !=null&& puppetsCollided[i].transform != this.transform)
                {
                    Vector3 vecFromOther = puppet.transform.position - puppetsCollided[i].transform.position;
                    float distance = vecFromOther.magnitude;
                    vecFromOther.Normalize();
                    if (distance != 0)
                    {
                        vecFromOther = vecFromOther * sphereCheckRadius / distance * puppet.stats.Get(Stats.StatType.move_speed);
                        separationForce += vecFromOther;
                    }
                }
            }
            separationForce = Vector3.ClampMagnitude(separationForce, puppet.stats.Get(Stats.StatType.move_speed));
#if UNITY_EDITOR
            giz.separateForce = separationForce;
#endif
            steering += separationForce* factor;
        }
    }
    public void Alignement(float factor = 1)
    {
        Vector3 averageDirection = Vector3.zero;
        closeNeighbours = GetNeighbouringPuppet();
        for (int i = 0; i < closeNeighbours.Length; i++)
        {
            if (closeNeighbours[i].transform!= transform)
                averageDirection += closeNeighbours[i].attachedRigidbody.velocity;
        }
        steering += (averageDirection.normalized) * puppet.stats.Get(Stats.StatType.move_speed) * factor;
    }
    public void Alignement(Vector3 leaderVel,float factor = 1)
    {
        Vector3 averageDirection = leaderVel;
        steering += (averageDirection.normalized) * puppet.stats.Get(Stats.StatType.move_speed) * factor;
    }
    public void Cohesion(float factor = 1)
    {
        Vector3 averagePosition = Vector3.zero;
        closeNeighbours = GetNeighbouringPuppet();
        for (int i = 0; i < closeNeighbours.Length; i++)
        {
            averagePosition += closeNeighbours[i].transform.position;
        }
        Seek(averagePosition / closeNeighbours.Length);
    }
    #endregion


    public Collider[] GetNeighbouringPuppet()
    {

        if (updateNeighbours)
        {
            int separationMask = LayerMask.GetMask(new string[] { "Puppet" });
            Vector3 separationForce = Vector3.zero;
            float sphereCheckRadius = puppet.Extents.magnitude * separationRayFactor;
            closeNeighbours = Physics.OverlapSphere(transform.position, sphereCheckRadius, separationMask);

            updateNeighbours = false;
        }
        return closeNeighbours;
    }
    protected virtual void GroundGravityCheck()
    {
        // peut être optimi en changeant layer + pas à chaques frames.
        RaycastHit hit;
        Vector3 center = transform.position + Vector3.up * puppet.Extents.y;
        if (Physics.Raycast(center, -puppet.Extents.y * Vector3.up, out hit))
        {
            OnPlanNormal = hit.normal;
            isOnGround = true;
        }
        else
        {
            isOnGround = false;
        }
    }
    public virtual Vector3 GetDvOnPlan(Vector3 target, Vector3 planNormal)
    {
        Vector3 dV = (target - transform.position);
        float distance = dV.magnitude;
        dV.Normalize();
        Vector3 right = Vector3.Cross(dV, planNormal);
        Vector3 planDv = Vector3.Cross(planNormal, right);

        if (distance > puppet.stats.Get(Stats.StatType.move_speed))
            return planDv.normalized * puppet.stats.Get(Stats.StatType.move_speed);
        else
            return planDv.normalized * distance;

    }


#if UNITY_EDITOR
    [System.Serializable]
    public struct GizmosForSteering 
        {
        public bool Velocity;
        public bool Steering;
        public bool SeparateCheckSphere;
        public bool Separate;

        public Vector3 steeringForce;
        public Vector3 separateForce;
        public float separateSphereLenght;
    }
    public GizmosForSteering giz;
#endif
}
