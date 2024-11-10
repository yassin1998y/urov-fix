using UnityEngine;
using System.Collections;

public class BoosterMenu : MonoBehaviour {
    public static BoosterMenu main;

    void Awake () {
        main = this;
	}
	

	public void StartLevel () {
        SessionInfo.current.boosterSelected = true;
	}
}
