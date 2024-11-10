using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public class LineExplosionEffect : ParticleEffect {

    IEffect effector;

    public override void Initialize() {
        Transform tEffector = transform.Find("Effector");
        if (tEffector) {
            effector = tEffector.GetComponent<IEffect>();
        }
        base.Initialize();
    }

    public void SetSides(List<Side> sides) {
        if (effector && sides.Count > 0) {
            effector.transform.rotation = Quaternion.Euler(0, 0, sides[0].ToAngle());
            effector.gameObject.SetActive(true);
            for (int i = 1; i < sides.Count; i++) {
                GameObject go = Instantiate(effector.gameObject);
                IEffect newEffect = go.GetComponent<IEffect>();
                newEffect.transform.position = effector.transform.position;
                newEffect.transform.rotation = Quaternion.Euler(0, 0, sides[i].ToAngle());
                newEffect.transform.SetParent(effector.transform.parent);
            }
        }
    }

    public override bool IsComplete() {
        return base.IsComplete() && effector != null;
    }
}
