using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using Yurowm.GameCore;

class PearlBooster : ISingleUseBooster, ILevelRuleExclusive {
    public override string FirstMessage() {
        return LocalizationAssistant.main[FirstMessageLocalizedKey()];
    }

    public bool IsCompatibleWith(LevelRule rule) {
        return rule is MatchChainRule || rule is MatchClickRule;
    }

    public override IEnumerator Logic() {
        ChipsProvider provider = new ChipsProvider(5, new IntRange(2, 3), Content.GetPrefab<Pearl>());

        yield return BoosterAssistant.main.StartCoroutine(provider.AddChips(8));

        SessionInfo.current.AddReaction(provider);
    }
}

class ChipsProvider : IReactionProvider {
    int moves = 0;
    IntRange movesPeriod;
    IntRange chipsCount;
    List<IChip> prefabs;

    // Only for deserializator
    public ChipsProvider() {}

    public ChipsProvider(IntRange movesPeriod, IntRange chipsCount, params IChip[] chipPrefabs) : this() {
        this.movesPeriod = movesPeriod;
        this.chipsCount = chipsCount;
        prefabs = new List<IChip>(chipPrefabs);

        moves = URandom.main.Range(movesPeriod);
    }

    public Func<IEnumerator> GetReactorLogic() {
        return Reaction;
    }

    IEnumerator Reaction() {
        moves--;
        if (moves <= 0) {
            moves = URandom.main.Range(movesPeriod);
            yield return BoosterAssistant.main.StartCoroutine(AddChips(2));
        }
    }

    public IEnumerator AddChips(int count = -1) {
        if (count < 0)
            count = URandom.main.Range(chipsCount);
        while (count > 0) {
            count--;
            FieldAssistant.main.Add(prefabs.GetRandom());
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void Serialize(XElement xml) {
        xml.Add(new XAttribute("moves", moves));
        xml.Add(new XAttribute("period", movesPeriod));
        xml.Add(new XAttribute("count", chipsCount));
        xml.Add(new XAttribute("prefabs", string.Join(",", prefabs.Select(x => x.name).ToArray())));
    }

    public void Deserizalie(XElement xml) {
        moves = int.Parse(xml.Attribute("moves").Value);
        movesPeriod = IntRange.Parse(xml.Attribute("period").Value);
        chipsCount = IntRange.Parse(xml.Attribute("count").Value);
        prefabs = new List<IChip>();
        foreach (string prefabName in xml.Attribute("prefabs").Value.Split(','))
            prefabs.Add(Content.GetPrefab<IChip>(prefabName));
    }
}