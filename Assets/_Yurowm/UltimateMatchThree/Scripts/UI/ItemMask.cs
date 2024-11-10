using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.UI;

public enum ComparisonOperator {Less, Greater, Equal, EqualLess, EqualGreater};
public enum ComparisonSource {Number, Reference, Item};
public enum ComparisonAction {Deactivate, LockButton};

public class ItemMask : MonoBehaviour {
    
    public Value A;
    public ComparisonOperator mustBe;
    public Value B;

    public bool allChild = true;
    public List<GameObject> targets = new List<GameObject>();
    
    public ComparisonAction action = ComparisonAction.Deactivate;

    void Start () {
		Refresh ();
        ItemCounter.refresh += Refresh;
	}

	void OnEnable () {
		Refresh (); // Updating when object is activated
	}

	// Refreshing
	public void Refresh () {
        int a = GetValue(A);
        int b = GetValue(B);
                     
		bool result = false;
        
        switch (mustBe) {
	        case ComparisonOperator.Less: result = a < b; break;
	        case ComparisonOperator.Greater: result = a > b; break;
	        case ComparisonOperator.EqualLess: result = a <= b; break;
	        case ComparisonOperator.EqualGreater: result = a >= b; break;
	        case ComparisonOperator.Equal: result = a == b; break;
		}
        AllTargets(result);
	}

    int GetValue(Value v) {
        switch (v.source) {
            case ComparisonSource.Item: return CurrentUser.main[v.compareItemID];
            case ComparisonSource.Number: return v.value;
            case ComparisonSource.Reference: return Reference.Get(v.reference);
        }
        return 0;
    }

    // Scenario of display / hide child objects
    void AllTargets (bool v) {
        if (allChild)
            foreach (Transform t in transform)
                Action(t.gameObject, v);

        foreach (GameObject t in targets)
            Action(t, v);
    }

    void Action(GameObject go, bool v) {
        if (action == ComparisonAction.Deactivate) {
            go.SetActive(v);
            return;
        }
        if (action == ComparisonAction.LockButton) {
            go.GetComponent<Button>().interactable = v;
            return;
        }
    }

    [Serializable]
    public class Value {
        public ComparisonSource source = ComparisonSource.Number;

        public int value = 1;
        public string reference = "";
        public ItemID compareItemID;
    }

}

[Serializable]
public class Comparer {
    public enum ComparisonOperator { Less, Greater, Equal, EqualLess, EqualGreater };

    public ComparisonOperator comparionOperator;
    public Value valueA = new Value();
    public Value valueB = new Value();

    public bool GetResult() {
        int a = valueA.GetValue();
        int b = valueB.GetValue();
        
        switch (comparionOperator) {
	        case ComparisonOperator.Less: return a < b;
	        case ComparisonOperator.Greater: return a > b;
	        case ComparisonOperator.EqualLess: return a <= b;
	        case ComparisonOperator.EqualGreater: return a >= b;
	        case ComparisonOperator.Equal: return a == b;
		}
        return false;
    }

    [Serializable]
    public class Value {
        public enum ComparisonSource { Number, Item, Reference };
        public ComparisonSource source = ComparisonSource.Number;

        public int value = 1;
        public string reference = "";
        public ItemID compareItemID;

        public int GetValue() {
            switch (source) {
                case ComparisonSource.Item: return CurrentUser.main[compareItemID];
                case ComparisonSource.Number: return value;
                case ComparisonSource.Reference: return Reference.Get(reference);
            }
            return 0;
        }
    }
}