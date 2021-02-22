using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class Plane : MonoBehaviour {

    /*
        can either go for higher obstacleaversion distance which looks more natural but cant find any disadventages atm
            or give avoidObstacleHeading in RotateTowards a higher priority (e.g. *3) 

        lower (1-normalHeadingFactor) if we are not DIRECTLY flying towards the target. e.g. if we are 10 meters away
            and we are not on a way to collide with the obstacle we dont need to fully steer away from it
            maybe angle between plane forward and dir to obstacle
    */


    public float speed = 30;
    public float turnSpeed = 90;

    public float nextShotTime = 0;


    public GameObject[] targets;

    void Start() {
        speed *= UnityEngine.Random.Range(0.7f, 1.4f);
        turnSpeed *= UnityEngine.Random.Range(0.7f, 1.4f);

        transform.position = new Vector3(0, UnityEngine.Random.Range(-20f,20f), 0) + transform.position;
    }

    float EvaluateTarget(Transform target) {
        float angle = VectorAngle(transform.forward, (target.position - transform.position));
        bool isInFront = angle < 90;
        float timeToTurn = angle / turnSpeed;
        float distance = (target.position - transform.position).magnitude;

        float forwardDiffAngle = VectorAngle(transform.forward, target.forward);
        bool flyingAwayFromEachOther = angle >= 90;

        float optimalDistance = obstacleAversionDistance * 2;

        var x = distance / optimalDistance / 2;
        float y;
        if (x <= 0.5f) {
            y = -math.pow(x-0.5f, 2)*4+1;
        } else {
            y = -(x-0.5f)*0.4f+1;
        }

        if (angle < 0.3f) y *= 10;

        if (isInFront) y *= 3;

        //testing
        if (closestDst > distance) closestDst = distance;

        return y;

        //if (distance < 5) return 0;

        // find function for distance (doesnt work for very long dst)
        //float dstValue = math.pow(50-distance, 2);
        //return math.pow(180-angle, 2) * 3 + math.pow(180-forwardDiffAngle, 2) + dstValue * 6;

        // best case: angle low, forwardDiffAngle low, distance "middle"

        // if forwardDiffAngle is low, target is either directly in front or back of us
        //      so if angle is low, we are the attacker, if angle is high we are the defender
    }

    // maybe higher prio for t in 90/180 degree cone in a range of maybe obstacleAversionDistance, obstacleAversionDistance+100

    Transform ChooseTarget() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 1000);
        float curMax = -9999999999999999;
        int curInd = -1;

        // testing
        closestDst = 9999999999999999;

        for (int i = 0; i < hitColliders.Length; i++) {
            if (hitColliders[i].transform.gameObject == this.gameObject) {
                continue;
            }
            var val = EvaluateTarget(hitColliders[i].transform);
            /* float angle = VectorAngle(transform.forward, (hitColliders[i].transform.position - transform.position));
            bool isInFront = angle < 90;
            var dst = Vector3.Distance(transform.position, hitColliders[i].transform.position);
            if (isInFront) dst /= 2;*/
            if (val > curMax) {
                curMax = val;
                curInd = i;
            }
        }

        if (curInd == -1) {
            return null;
        }

        weight = curMax;
        dst = math.distance(transform.position, hitColliders[curInd].transform.position);

        return hitColliders[curInd].transform;
    }

    /*int ChooseTarget(GameObject[] targets) {
        float[] values = new float[targets.Length];

        int chosen = -1;
        float curMax = -1;

        for (int i = 0; i < targets.Length; i++) {
            var target  = targets[i];
            float angle = VectorAngle(transform.forward, (target.transform.position - transform.position));
            bool isInFront = angle < 90;
            float timeToTurn = angle / turnSpeed;
            float distance = (target.transform.position - transform.position).magnitude;

            float forwardDiffAngle = VectorAngle(transform.forward, target.transform.forward);
            bool flyingAwayFromEachOther = angle >= 90;

            //if (distance < 5) return 0;

            // find function for distance (doesnt work for very long dst)
            float dstValue = math.pow(50-distance, 2);
            //var res = math.pow(180-angle, 2) * 3 + math.pow(180-forwardDiffAngle, 2) + dstValue * 6;
            var res = distance;
            //values[i] = EvaluateTarget(targets[i]);
            if (res > curMax) {
                curMax = res;
                chosen = i;
            }
        }

        return chosen;
    }*/

    int TargetStatus() {
        // target in front of us 
        // (are able/not able to turn in time to attack)
        // target fling towards/away from us

        return -1;
    }

    float VectorAngle(float3 a, float3 b) {
        return math.acos(math.dot(math.normalizesafe(a), math.normalizesafe(b))) * 57.2958f;
    }

    void RotateTowards(float3 targetForward) {
        targetForward = math.normalizesafe(targetForward);
        var forward = (float3)transform.forward;

        var maxRot = math.PI * Time.deltaTime * .3f;
        var rot = math.acos(math.dot(forward, targetForward));

        if (rot <= 0.01f) return;

        var nextHeading = math.select( 
            math.cos(maxRot) * forward + math.sin(maxRot) *  (math.normalizesafe(math.cross(math.cross(forward,targetForward), forward))),
            targetForward,
            rot <= maxRot );

        transform.rotation = Quaternion.RotateTowards(transform.rotation, quaternion.LookRotationSafe(targetForward, math.mul(transform.rotation, new float3(0,1,0))), Time.deltaTime * turnSpeed);
    }

    public float timeUntilAutoTarget = -10;
    Vector3 targetPos = Vector3.zero;
    public bool auto = true;

    public float obstacleAversionDistance = 50;
    public Vector3 obstaclePos;
    public float obstacleDistance = 51;

    public void CheckForObstacle() {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, obstacleAversionDistance);
        float curMin = 9999999999999999;
        int curInd = -1;

        for (int i = 0; i < hitColliders.Length; i++) {
            if (hitColliders[i].transform.gameObject == this.gameObject) {
                continue;
            }
            var dst = Vector3.Distance(transform.position, hitColliders[i].transform.position);
            if (dst < curMin) {
                curMin = dst;
                curInd = i;
            }
        }

        if (curInd == -1) {
            obstacleDistance = obstacleAversionDistance + 1;
            return;
        }

        obstacleDistance = curMin;
        obstaclePos = hitColliders[curInd].transform.position;  // can also get gameobject here to get rotation and calulate bullet lead
    }

    // TODO: add going for new target if we find a new better one and add the go back to auto if a new one is found

    Transform currentTarget = null;

    public float weight;
    public float dst;
    public float closestDst;

    public bool showDebugInfo = false;

    public GameObject bullet;

    void Update() {

        CheckForObstacle();
        var t = ChooseTarget();

        if (t == null) {
            print("NO ENEMY");
            return;
        }

        var target = t.position;
        //var targedIndex = ChooseTarget(targets);
        //var target = targets[targedIndex].transform.position;

        //if ((t != currentTarget && weight > 1) || closestDst > obstacleAversionDistance * 4) {
        //if ((t != currentTarget && weight > 1 && Vector3.Distance(transform.position, target) > obstacleAversionDistance * 3) || closestDst > obstacleAversionDistance * 4) {#
        if (weight > 1 && Vector3.Distance(transform.position, target) > obstacleAversionDistance * 4) {
            auto = true;
            currentTarget = t;
            /*if (math.distance(target, transform.position) > obstacleAversionDistance) {
                targetPos = target + (target - transform.position).normalized * obstacleAversionDistance * 2;
                timeUntilAutoTarget = Time.time + 7;
            } else {
                auto = true;
                currentTarget = t;
            }*/
        }

        if (!auto) {
            if (timeUntilAutoTarget < Time.time || math.distance(targetPos, transform.position) < obstacleAversionDistance) {
                auto = true;
            }
        } else {
            if (Vector3.Distance(transform.position, target) < obstacleAversionDistance) {
                auto = false;
                targetPos = target + UnityEngine.Random.onUnitSphere * obstacleAversionDistance * 3;
                //targetPos = target + (target - transform.position).normalized * obstacleAversionDistance * 3;
                timeUntilAutoTarget = Time.time + 15;
            } else {
                targetPos = target;
            }
        }

        var targetHeading = (targetPos - transform.position).normalized;
        var avoidObstacleHeading = (transform.position - obstaclePos).normalized;

        var angleToObstacle = VectorAngle(transform.forward, obstaclePos - transform.position) / 360;

        var normalHeadingFactor = math.clamp((obstacleDistance - 5) / (obstacleAversionDistance-5), 0, 1);

        RotateTowards(math.normalizesafe(normalHeadingFactor * targetHeading + (1-normalHeadingFactor) * math.pow((1-angleToObstacle), 7) * avoidObstacleHeading));

        transform.position += transform.forward * Time.deltaTime * speed;


        if (nextShotTime < Time.time) {
            nextShotTime = Time.time + 0.05f;
            var angleToTaret = VectorAngle(transform.forward, target - transform.position);
            if (angleToTaret < 10) {
                Instantiate(bullet, transform.position, Quaternion.LookRotation(targetHeading));
            }
        }
    }

    // TODO: allow dodge left right, completely random so that its not all in one line in the end
    //  AND make it so that we do not retarget while "moving away after attack" if the enemy is in the back
    // maybe im also just stupid and i have to think a lot about a STUPIDLY EASY WAY to do this task.

    void OnDrawGizmos() {
        if (!UnityEditor.EditorApplication.isPlaying) return;
        if (!showDebugInfo) return;
        Gizmos.DrawLine(transform.position, targetPos);
    }
}
