using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class NodeEffect : StrikeEffect {
    TrailRenderer trail;

    public FloatRange speed;
    public FloatRange offsetDistace;
    public FloatRange offsetAngle;
    float _speed;

    Vector3 offset;

    public override void Initialize() {
        trail = GetComponent<TrailRenderer>();
        _speed = Random.Range(speed.min, speed.max);
        offset = Quaternion.Euler(0, 0, Random.Range(offsetAngle.min, offsetAngle.max)) * Vector3.right * Random.Range(offsetDistace.min, offsetDistace.max);
        base.Initialize();
    }

    float time = 1f;
    public override Vector3 GetNewPosition(Vector3 targetPosition) {
        time -= Time.deltaTime * _speed;
        time = Mathf.Max(0, time);

        return Vector3.Lerp(targetPosition + offset * time, startPosition, time);
    }

    public override IEnumerator Death() {
        if (trail)
            yield return new WaitForSeconds(trail.time);
    }

    
}
