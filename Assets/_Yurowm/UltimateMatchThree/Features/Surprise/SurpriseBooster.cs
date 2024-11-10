using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Yurowm.GameCore;

class SurpriseBooster : ISingleUseBooster {
    [ContentSelector]
    public List<ISingleUseBooster> boostersInside;

    IBooster logic;

    public override void Initialize() {
        base.Initialize();
        logic = Content.Emit(boostersInside.Where(x => BoosterPanel.Validate(x)).GetRandom());
        logic.transform.SetParent(transform);
        logic.transform.Reset();
    }

    public override void OnKill() {
        if (logic) logic.Kill();
        base.OnKill();
    }

    public override string FirstMessage() {
        return logic.FirstMessage();
    }

    public override IEnumerator Logic() {
        return logic.Logic();
    }
}