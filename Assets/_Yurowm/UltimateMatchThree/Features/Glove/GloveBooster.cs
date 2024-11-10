using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

class GloveBooster : IMultipleUseBooster {
    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }

    Slot slot;

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
            slot.HitAndScore();
            SessionInfo.current.rule.matchDate++;
        }

        slot = null;
        BoosterAssistant.main.boosterMode = null;
    }
}