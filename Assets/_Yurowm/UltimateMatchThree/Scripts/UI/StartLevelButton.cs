using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent (typeof (Button))]
public class StartLevelButton : MonoBehaviour {

	public void OnClick () {
		if (CPanel.uiAnimation > 0) return;
        SessionAssistant.main.StartSession();
	}
}
