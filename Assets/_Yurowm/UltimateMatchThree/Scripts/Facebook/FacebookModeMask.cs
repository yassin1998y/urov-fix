using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class FacebookModeMask : MonoBehaviour {

    public FacebookMode targetMode = FacebookMode.Online;
    public ComparisonAction action = ComparisonAction.Deactivate;

    public bool negative = false;

    public bool allChild = true;
    public List<GameObject> targets = new List<GameObject>();

    void Start() {
        Refresh();
        ItemCounter.refresh += Refresh;
    }

    void OnEnable() {
        Refresh(); // Updating when object is activated
    }

    void Refresh() {
        AllTargets(Online.main.mode == targetMode);

        if (Online.main.mode == FacebookMode.Disabled && targetMode != FacebookMode.Disabled) {
            targets.ForEach(Destroy);
            Destroy(this);
        }
    }

    void AllTargets(bool v) {
        if (negative) v = !v;

        if (allChild)
            foreach (Transform t in transform)
                Action(t.gameObject, v);

        foreach (GameObject t in targets)
            Action(t, v);
    }

    void Action(GameObject go, bool v) {
        if (!go) return;
        if (action == ComparisonAction.Deactivate) {
            go.SetActive(v);
            return;
        }
        if (action == ComparisonAction.LockButton) {
            go.GetComponent<Button>().interactable = v;
            return;
        }
    }
}

