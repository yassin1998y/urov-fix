using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;

public class SetRandomSprite : MonoBehaviour {

    public Target target = Target.Sprite;

    public List<Sprite> sprites = new List<Sprite>();

    Component _component;
    Component component {
        get {
            if (!_component)
                _component = GetComponent();
            return _component;
        }
    }
    
    Sprite sprite {
        get {
            switch (target) {
                case Target.Sprite: return (component as SpriteRenderer).sprite;
                case Target.Image: return (component as Image).sprite;
            }
            return null;
        }
        set {
            switch (target) {
                case Target.Sprite: (component as SpriteRenderer).sprite = value; break;
                case Target.Image: (component as Image).sprite = value; break;
            }
        }
    }

    void Start() {
        Set();
    }

    public void Set() {
        if (sprites.Count == 0)
            return;
        Sprite sprite = sprites.GetRandom();
        this.sprite = sprite;
    }

    Component GetComponent() {
        switch (target) {
            case Target.Sprite: return GetComponent<SpriteRenderer>();
            case Target.Image: return GetComponent<Image>();
        }
        return null;
    }

    public enum Target {
        Sprite, Image
    }
}
