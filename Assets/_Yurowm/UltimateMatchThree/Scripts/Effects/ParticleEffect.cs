using System.Collections;
using UnityEngine;
using Yurowm.GameCore;

// The effect of the particles. Upon completion will be removed.
public class ParticleEffect : IEffect, ISounded {
	
	ParticleSystem ps;
    float deathTime;


    public override bool IsComplete() {
        return Time.time >= deathTime;
    }

    public override void Launch() {
        if (sound)
            sound.Play("Destroying");
        uint seed = (uint) Random.Range(int.MinValue, int.MaxValue);
        float duration = 0;
        foreach (var child in transform.AndAllChild(true)) {
            ps = child.GetComponent<ParticleSystem>();

            if (ps) {
                if (!ps.isPlaying) {
                    ps.randomSeed = seed;
                    ps.Play();
                }
                duration = Mathf.Max(duration, GetDuration(ps));
            }
        }

        deathTime = Time.time + duration;
    }

    public float GetDuration(ParticleSystem particleSystem) {
        return particleSystem.main.duration + particleSystem.main.startLifetimeMultiplier;
    }

    public IEnumerator GetSoundNames() {
        yield return "Destroying";
    }
}