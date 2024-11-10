using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class RandomTransform : MonoBehaviour {


    public FloatRange rotationZ;
    public FloatRange scaleX;
    public FloatRange scaleY;
    public FloatRange scale;

    void Awake() {
        if (rotationZ.interval != 0) transform.Rotate(0, 0, Random.Range(rotationZ.min, rotationZ.max));
        if (scaleX.interval != 0) transform.localScale = new Vector3(Random.Range(scaleX.min, scaleX.max), transform.localScale.y, transform.localScale.z);
        if (scaleY.interval != 0) transform.localScale = new Vector3(transform.localScale.x, Random.Range(scaleY.min, scaleY.max), transform.localScale.z);
        if (scale.interval != 0) transform.localScale *= Random.Range(scale.min, scale.max);
    }

}
