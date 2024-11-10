using UnityEngine;
using System.Collections;
using Yurowm.GameCore;

public class TargetCounterColor : TargetCounter {
    ItemColor _color;

    public ItemColor color {
        get {
            return _color;
        }
        set {
            _color = value;
            foreach (var sprite in GetComponentsInChildren<SetSpriteColor>(true))
                sprite.SetColor(_color, false);
        }
    }
}
