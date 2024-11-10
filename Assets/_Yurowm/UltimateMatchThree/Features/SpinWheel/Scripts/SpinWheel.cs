using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Yurowm.GameCore;
using Reward = SpinWheelAssistant.Reward;

public class SpinWheel : MonoBehaviour, ISounded {

    const float spinningMaxVelocity = 500;

    public Button spinButton;
    public Transform wheel;
    public Animation notEnoughSpins;

    public Button rewardedSpinButton;
    public GameObject rewardedSpinTimer;

    Dictionary<Reward, SpinWhellItem> items = new Dictionary<Reward, SpinWhellItem>();

    ContentSound sound;

	void Awake () {
        spinButton.onClick.AddListener(Spin);
        int index = 0;
        foreach (Reward reward in SpinWheelAssistant.main.rewards) {
            SpinWhellItem item = Content.GetItem<SpinWhellItem>();
            reward.index = index++;
            item.index = reward.index;
            item.transform.SetParent(wheel);
            item.transform.Reset();
            item.name = "Item#" + item.index;
            item.SetInfo(reward);
            items.Add(reward, item);
        }

        sound = GetComponent<ContentSound>();

        rewardedSpinButton.onClick.AddListener(() => CurrentUser.main.rewardedSpin.Get());
        ItemCounter.refresh += RewardedRefresh;
    }

    void RewardedRefresh() {
        bool available = CurrentUser.main.rewardedSpin.IsAvailable();
        rewardedSpinButton.gameObject.SetActive(available);
        rewardedSpinButton.interactable = Advertising.main.CountOfReadyAds(AdType.Rewarded) > 0;
        rewardedSpinTimer.SetActive(!available);
    }

    void OnEnable() {
        RewardedRefresh();
        wheel.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    void Spin() {
        StartCoroutine(Spinning());
    }

    bool spinnig = false;
    IEnumerator Spinning() {
        if (spinnig) yield break;
        spinnig = true;

        bool pressed = CurrentUser.main[ItemID.spin] > 0;

        BerryAnalytics.Event("Spin Wheel Pressed",
            "Success:" + pressed);
        
        sound.Play("Button" + (pressed ? "Success" : "Failed"));

        #region Button Animatio
        StopCoroutine("PressButton");

        yield return StartCoroutine(PressButton(Vector3.one * (pressed ? 0.8f : 0.95f)));

        StartCoroutine(PressButton(Vector3.one, pressed ? 0.2f : 1f));
        #endregion

        if (pressed) {
            CurrentUser.main[ItemID.spin]--;
            ItemCounter.RefreshAll();

            Reward reward = SpinWheelAssistant.main.EmitReward();

            float spinVelocity = 0f;

            while (spinVelocity < spinningMaxVelocity) {
                spinVelocity = Mathf.MoveTowards(spinVelocity, spinningMaxVelocity,
                    spinningMaxVelocity * Time.unscaledDeltaTime);
                wheel.transform.Rotate(0, 0, -spinVelocity * Time.unscaledDeltaTime);
                Sound(spinVelocity);
                yield return 0;
            }
            float delay = 2f;
            while (delay > 0) {
                delay -= Time.unscaledDeltaTime;
                wheel.transform.Rotate(0, 0, -spinVelocity * Time.unscaledDeltaTime);
                Sound(spinVelocity);
                yield return 0;
            }

            float sectorSize = 360f / SpinWheelAssistant.itemCount;
            float target_angle = 360f - sectorSize * reward.index;
            target_angle -= 360 * 3;
            target_angle += Random.Range(-.5f, .5f) * sectorSize;

            float current_angle = wheel.eulerAngles.z;

            while (current_angle != target_angle) {
                spinVelocity = Mathf.MoveTowards(spinVelocity,
                    Mathf.Min(spinningMaxVelocity, Mathf.Abs(current_angle - target_angle) + 1),
                    (spinVelocity + 5f) * Time.unscaledDeltaTime);
                current_angle = Mathf.MoveTowards(current_angle, target_angle, spinVelocity * Time.unscaledDeltaTime);
                wheel.transform.eulerAngles = Vector3.forward * current_angle;
                Sound(spinVelocity);
                yield return 0;
            }

            items[reward].Rewarded(ObjectTag.GetFirst("BottomDeep").transform);

            ItemCounter.RefreshAll();
        } else
            notEnoughSpins.Play();

        spinnig = false;
    }

    IEnumerator PressButton(Vector3 scale, float speed = 1f) {
        while (spinButton.transform.localScale != scale) {
            spinButton.transform.localScale = Vector3.MoveTowards(spinButton.transform.localScale,
                scale, Time.unscaledDeltaTime * 3f * speed);
            yield return 0;
        }
    }

    float lastTime = -1;
    const float soundRate = 20;
    void Sound(float speed) {
        if (Time.unscaledTime - lastTime > soundRate / speed) {
            lastTime = Time.unscaledTime;
            sound.Play("Tick");
        }
    }

    public IEnumerator GetSoundNames() {
        yield return "Tick";
        yield return "ButtonSuccess";
        yield return "ButtonFailed";

    }
}
