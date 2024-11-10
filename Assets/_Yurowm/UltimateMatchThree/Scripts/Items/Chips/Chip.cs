using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Yurowm.GameCore;
using UnityEngine;
using System.Xml.Linq;

public abstract class IChip : ISlotContent {
    public static List<IChip> busyList = new List<IChip>();
    
	internal bool move = false;
    float velocity = 0;

    internal bool falling;

    const float offsetThreshold = 1f;
    const float maxImpuls = 6f;
    const float minImpuls = .5f;

    Vector3 impuls = new Vector3();

    public override void Initialize() {
        base.Initialize();
        StartCoroutine(ChipPhysics());
    }

    internal bool toLand = true;
    public virtual void OnLand() {
        animator.Play("Landing");
        if (isActiveContent)
            sound.Play("Landing");
    }

    IEnumerator ChipPhysics() {
        IDestroyable destroyable = this as IDestroyable;
        while (true) {
            yield return 0;

            if (!SessionInfo.current.isPlaying || (destroyable != null && destroyable.destroying)) continue;
            
            if (!slot) {
                if (destroyable == null || !destroyable.destroying) Kill();
                yield break;
            }

            slot.GravityReaction();

            #region Gravity
            while (transform.localPosition != Vector3.zero || impuls != Vector3.zero) {
                falling = true;

                while (SessionInfo.current.rule.GetMode() != PlayingMode.Gravity
                    && SessionInfo.current.rule.GetMode() != PlayingMode.Shuffle
                    && impuls == Vector3.zero
                    && transform.localPosition == Vector3.zero)
                    yield return 0;

                if (destroyable != null && destroyable.destroying)
                    break;

                if (impuls == Vector3.zero) {
                    if (velocity == 0) velocity = Project.main.chip_start_velosity;
                    velocity += Project.main.chip_acceleration * Time.deltaTime;
                    if (velocity > Project.main.chip_max_velocity) 
                        velocity = Project.main.chip_max_velocity;

                    transform.localPosition = transform.localPosition.Scale(z: 0);

                    float threshold = (offsetThreshold * offsetThreshold) * Time.deltaTime;
                    if (slot && transform.localPosition.sqrMagnitude < threshold) {
                        if (!slot.GravityReaction()) {
                            while (transform.localPosition != Vector3.zero) {
                                transform.localPosition = Vector3.MoveTowards(transform.localPosition, Vector3.zero, Time.deltaTime * velocity);
                                yield return 0;
                            }
                            falling = false;
                            velocity = 0f;
                            if (toLand) {
                                OnLand();
                                toLand = false;
                            }
                            break;
                        }
                    }

                    Vector3 moveVector = -transform.localPosition;
                    if (Mathf.Abs(transform.localPosition.x) > 0.1f) {
                        if (transform.localPosition.x < 0) moveVector.x = 1;
                        if (transform.localPosition.x > 0) moveVector.x = -1;
                    }
                    if (Mathf.Abs(transform.localPosition.y) > 0.1f) {
                        if (transform.localPosition.y < 0) moveVector.y = 1;
                        if (transform.localPosition.y > 0) moveVector.y = -1;
                    }
                    moveVector = moveVector.normalized * velocity;
                    transform.localPosition += moveVector * Time.deltaTime;
                
                } else {
                    float threshold = Project.main.slot_offset / 2;
                    if (transform.localPosition.sqrMagnitude < threshold)
                        if (slot) slot.GravityReaction();

                    if (impuls.sqrMagnitude > maxImpuls * maxImpuls)
                        impuls = impuls.normalized * maxImpuls;

                    transform.position += impuls * Time.deltaTime;
                    transform.position -= transform.localPosition * Time.deltaTime;
                    impuls -= impuls * Time.deltaTime;
                    impuls -= transform.localPosition * 90f * Time.deltaTime;
                    impuls *= Mathf.Max(0, 1f - Time.deltaTime * 6f);

                    threshold = (offsetThreshold * offsetThreshold) * Time.deltaTime;
                    if (transform.localPosition.sqrMagnitude < threshold)
                        if (impuls.sqrMagnitude < minImpuls * minImpuls)
                            impuls = Vector3.zero;
                }

                yield return 0;
                falling = false;
            }
            #endregion
        }
    }

    public static void Explode(Vector3 center, float radius, float force) {
        List<IChip> chips = GetAll<IChip>(x => x.isActiveContent);
        force *= Project.main.explosion_multiplier;
        Vector3 impuls;
        foreach (IChip chip in chips) {
            if (!chip.slot || chip.slot.block) continue;
            if ((chip.transform.position - center).magnitude > radius) continue;
            impuls = (chip.transform.position - center) * force;
            impuls *= Mathf.Pow((radius - (chip.transform.position - center).magnitude) / radius, 2);
            impuls = impuls.Scale(z: 0);
            chip.impuls += impuls;
        }
    }

    public void AddImpuls(Vector3 impuls) {
        this.impuls += impuls;
    }

    public void Flashing() {
        animator.Play("Flashing");
        sound.Play("Flashing");
        Project.onSwapSuccess.AddListener(StopFlashing);
    }

    void StopFlashing() {
        animator.Complete("Flashing");
        Project.onSwapSuccess.RemoveListener(StopFlashing);
    }

    // separation of the chips from the parent slot
    public void  ParentRemove (){
        if (!slot) return;
        slot.chip = null;
    }
}