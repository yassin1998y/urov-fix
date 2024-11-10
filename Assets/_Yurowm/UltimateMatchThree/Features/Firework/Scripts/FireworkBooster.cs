using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

class FireworkBooster : IMultipleUseBooster {
    [ContentSelector]
    public RocketEffect rocketEffect;
    [ContentSelector]
    public StrikeEffect strikeEffect;

    Slot slot;

    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }
    
    public override void Initialize() {
        slot = null;
        base.Initialize();
    }

    void OnClick(Slot slot) {
        this.slot = slot;
    }
    
    public override IEnumerator Logic() {
        List<Slot> targets = Slot.allActive.Values.Where(x => x.color.IsPhysicalColor()).ToList();
        targets.ForEach(x => x.Highlight());

        ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.Click);
        ControlAssistant.main.onClick += OnClick;

        while ((!slot || !targets.Contains(slot)) && !IsCanceled())
            yield return new WaitForSeconds(0.1f);

        ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.Regular);
        ControlAssistant.main.onClick -= OnClick;
        targets.ForEach(x => x.Unlight());

        if (!IsCanceled()) {
            ItemColor color = slot.color;

            targets.RemoveAll(x => x.color != color);

            bool exploded = false;

            RocketEffect firework = Content.Emit(rocketEffect);
            Vector3 targetPosition = GameCamera.camera.transform.position;
            targetPosition += new Vector3(Random.Range(-2f, 2f), Random.Range(-2f, 2f), 0);
            targetPosition = targetPosition.Scale(z: 0);
            firework.transform.position = targetPosition - new Vector3(4, 12, 0);
            firework.SetTarget(targetPosition);
            firework.onReach += () => exploded = true;
            firework.Play();

            while (!exploded)
                yield return 0;

            foreach (Slot target in targets) {
                StrikeEffect effect = Content.Emit(strikeEffect);
                effect.transform.position = firework.transform.position;
                effect.Repaint(color);
                effect.SetTarget(target.transform);
                effect.Play();
                effect.onReach += target.HitAndScore;
            }

            SessionInfo.current.rule.matchDate++;

            BoosterAssistant.main.boosterMode = null;
        }
    }
}