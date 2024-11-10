using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

class TornadoBooster : IMultipleUseBooster, ILocalized {
    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }

    public string MixingLocalizedKey() {
        return string.Format("booster/item/{0}/mixing", itemID);
    }

    Slot slot;

    public override void Initialize() {
        base.Initialize();
        slot = null;
    }

    void OnClick(Slot slot) {
        this.slot = slot;
    }

    public override IEnumerator Logic() {
        List<Slot> targets = Slot.allActive.Values.Where(x => !x.block && x.color.IsPhysicalColor()).ToList();
        targets.ForEach(x => x.Highlight());

        ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.Click);
        ControlAssistant.main.onClick += OnClick;

        while ((!slot || !targets.Contains(slot)) && !IsCanceled())
            yield return new WaitForSeconds(0.1f);

        ControlAssistant.main.ChangeMode(ControlAssistant.ControlMode.Regular);
        ControlAssistant.main.onClick -= OnClick;
        
        targets.ForEach(x => x.Unlight());

        if (!IsCanceled()) {
            BoosterAssistant.main.boosterMode = PlayingMode.Gravity;

            //BoosterUI.main.SetMessage(LocalizationAssistant.main[MixingLocalizedKey()]);

            yield return new WaitForSeconds(0.1f);
            SessionInfo.current.rule.Shuffle();

            do
                yield return new WaitForSeconds(0.1f);
            while (Contains<IChip>(x => x.falling));
        }

        BoosterAssistant.main.boosterMode = null;
    }
    
    public override IEnumerator RequriedLocalizationKeys() {
        yield return base.RequriedLocalizationKeys();
        yield return MixingLocalizedKey();
    }
}