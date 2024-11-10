using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class RocketEffect : StrikeEffect {

    public FloatRange speed;
    public FloatRange offsetDistace;
    public FloatRange offsetAngle;

    float _speed;

    Vector3 offset;

    float? distance = null;
    
    public override void Initialize() {
        _speed = Random.Range(speed.min, speed.max);
        offset = Quaternion.Euler(0, 0, Random.Range(offsetAngle.min, offsetAngle.max)) * Vector3.right * Random.Range(offsetDistace.min, offsetDistace.max);
        base.Initialize();
    }

    public override Vector3 GetNewPosition(Vector3 targetPosition) {
        if (!distance.HasValue)
            distance = offset == Vector3.zero ? 0 : Vector3.Distance(transform.position, targetPosition) / offset.magnitude;
        else if (distance.Value != 0)
            offset = Vector3.MoveTowards(offset, Vector3.zero, _speed * Time.deltaTime / distance.Value);

        return Vector3.MoveTowards(transform.position, targetPosition + offset, _speed * Time.deltaTime);
    }
}
