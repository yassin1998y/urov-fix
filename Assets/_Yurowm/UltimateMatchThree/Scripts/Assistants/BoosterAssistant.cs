using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Yurowm.GameCore;
using UnityEngine;
using UnityEngine.UI;
using System.Xml.Linq;

public class BoosterAssistant : MonoBehaviourAssistant<BoosterAssistant>, ILocalized {

    internal PlayingMode? boosterMode = null;

    IBooster currentLogic;

    internal void Cancel() {
        if (currentLogic != null && currentLogic is IMultipleUseBooster)
            (currentLogic as IMultipleUseBooster).Cancel();            
    }

    public IEnumerator Run (IBooster prefab) {
        if (CurrentUser.main[prefab.itemID] < 1) {
            UIAssistant.main.ShowPage("Store");
            yield break;
        }

        BerryAnalytics.Event("Booster Started", "ItemID:" + prefab.itemID);

        currentLogic = Content.Emit(prefab);
        currentLogic.transform.SetParent(FieldAssistant.main.sceneFolder);
        currentLogic.transform.Reset();
        currentLogic.Initialize();

        //BoosterUI.main.SetMessage(currentLogic.FirstMessage());

        yield return StartCoroutine(BoosterUI.main.Show());
        yield return StartCoroutine(currentLogic.Logic());

        boosterMode = null;
        if (currentLogic is ISingleUseBooster || !(currentLogic as IMultipleUseBooster).IsCanceled()) {
            BerryAnalytics.Event("Booster Used", "ItemID:" + prefab.itemID);
            CurrentUser.main[prefab.itemID]--;
            UserUtils.WriteProfileOnDevice(CurrentUser.main);
            ItemCounter.RefreshAll();
        }

        yield return StartCoroutine(BoosterUI.main.Hide());

        currentLogic = null;
    }

    public IEnumerator RequriedLocalizationKeys() {
        foreach (ILocalized localized in Content.GetPrefabList<IBooster>(x => x is ILocalized).Cast<ILocalized>())
            yield return localized.RequriedLocalizationKeys();
    }
}

public abstract class IBooster : ILiveContent, ISounded, IAnimated {
    [HideInInspector]
    public bool localized = false;

    public const string editLocalizationPattern = "booster/item/{0}/";
    public const string titleLocalizationKey = "booster/item/{0}/title";
    public const string descriptionLocalizationKey = "booster/item/{0}/description";
    public const string firstMessageKeyFormat = "booster/item/{0}/1message";

    internal ContentAnimator animator;
    internal ContentSound sound;

    public override void Initialize() {
        base.Initialize();

        animator = GetComponent<ContentAnimator>();
        sound = GetComponent<ContentSound>();
    }

    public ItemID itemID;
    [HideInInspector]
    public Sprite icon;
    [HideInInspector]
    public string title = "";
    [HideInInspector]
    public string description = "";

    public abstract IEnumerator Logic();
    public abstract string FirstMessage();


    public string FirstMessageLocalizedKey() {
        return string.Format(firstMessageKeyFormat, itemID);
    }
    
    public virtual IEnumerator RequriedLocalizationKeys() {
        yield return FirstMessageLocalizedKey();
        if (localized) {
            yield return string.Format(titleLocalizationKey, itemID);
            yield return string.Format(descriptionLocalizationKey, itemID);
        }
    }

    public virtual IEnumerator GetAnimationNames() {
        yield break;
    }

    public virtual IEnumerator GetSoundNames() {
        yield break;
    }
}

public abstract class ISingleUseBooster : IBooster {}

public abstract class IMultipleUseBooster : IBooster {
    bool canceled = false;
    public override void Initialize() {
        canceled = false;
    }

    public void Cancel() {
        canceled = true;
    }

    public bool IsCanceled() {
        return canceled;
    }
}