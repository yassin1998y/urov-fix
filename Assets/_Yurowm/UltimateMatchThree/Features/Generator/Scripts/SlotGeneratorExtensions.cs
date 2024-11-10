using System.Collections.Generic;
using UnityEngine;
using Yurowm.GameCore;

public interface IGenerated {}

public interface IGeneratedWithCondition : IGenerated {
    bool GenerationCondition();
}

public interface IGeneratedWithProbability : IGenerated {}

public abstract class GeneratedChipController {
    public abstract bool CanItBeGenerated(int2 position);
}

public class GeneratedWithProbability : GeneratedChipController {

    public static readonly string keyFormat = "probability_{0}";

    Dictionary<int2, float> mask = new Dictionary<int2, float>();

    public GeneratedWithProbability(IGeneratedWithProbability chip) {}

    public void SetProbabilityMask(int2 position, float probability) {
        mask.Set(position, Mathf.Clamp01(probability));
    }

    public float GetMask(int2 position) {
        return mask.Get(position);
    }

    public override bool CanItBeGenerated(int2 position) {
        return mask.Get(position) > UnityEngine.Random.value;
    }
}

public class GeneratedWithCondition : GeneratedChipController {

    public static readonly string keyFormat = "condition_{0}";

    IGeneratedWithCondition chip;

    Dictionary<int2, bool> mask = new Dictionary<int2, bool>();

    public GeneratedWithCondition(IGeneratedWithCondition chip) {
        this.chip = chip;
    }

    public void SetConditionMask(int2 position, bool enable) {
        mask.Set(position, enable);
    }

    public bool GetMask(int2 position) {
        return mask.Get(position);
    }

    public override bool CanItBeGenerated(int2 position) {
        if (mask.Get(position))
            return chip.GenerationCondition();
        return false;
    }
}