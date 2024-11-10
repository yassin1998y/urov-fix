using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Yurowm.GameCore;
using TMPro;

public class SpinWhellItem : MonoBehaviour {

    [HideInInspector]
    public int index = 0;
    public TextMeshProUGUI counter;
    public Image icon;
    public Image background;

    public Color oddColor;
    public Color evenColor;
    public Color rareColor;
    public Color uniqueColor;

    new Animation animation;

    SpinWheelAssistant.Reward reward = null;

    [ContentSelector]
    public CollectionEffect collectionEffect;

    void Awake() {
        animation = GetComponent<Animation>();
    }

    public void SetInfo (SpinWheelAssistant.Reward reward) {
        this.reward = reward;
        counter.text = reward.count.ToString();
        icon.sprite = reward.icon;
        Color color = Color.white;
        switch (reward.type) {
            case SpinWheelAssistant.Reward.Type.Regular:
                color = index % 2 == 0 ? evenColor : oddColor; break;
            case SpinWheelAssistant.Reward.Type.Rare: color = rareColor; break;
            case SpinWheelAssistant.Reward.Type.Unique: color = uniqueColor; break;
        }

        background.color = color;
        HSBColor hsb = new HSBColor(color);
        hsb.b += 0.4f;
        hsb.h += 0.05f;
        hsb.s -= 0.4f;
        counter.color = hsb.ToColor();

        hsb = new HSBColor(color);
        hsb.b -= 0.3f;
        hsb.h -= 0.05f;
        hsb.s += 0.2f;
        counter.fontMaterial.SetColor("_OutlineColor", hsb.ToColor());

        transform.rotation = Quaternion.Euler(0, 0, 360f * index / 12);
    }

    public void Rewarded(Transform target) {
        StopAllCoroutines();
        StartCoroutine(Rewarding(target));
    }

    IEnumerator Rewarding(Transform target) {
        animation.Play();

        yield return new WaitForSeconds(0.3f);

        if (reward != null && collectionEffect) {
            int count = reward.count;
            if (count > 100)
                count = 10;
            else if (count > 5)
                count = 5;

            for (int i = 0; i < count; i++) {
                CollectionEffect effect = Content.Emit(collectionEffect);
                effect.transform.Reset();
                effect.transform.position = icon.transform.position;
                effect.SetIcon(reward.icon);
                effect.SetOrder(count - i);
                effect.SetTarget(target);
                effect.Play();

                yield return new WaitForSeconds(0.1f);
            }
        }
    }
}
