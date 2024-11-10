using UnityEngine;
using UnityEngine.UI;

public class ScoreCounter : MonoBehaviour {


    Text label;

	float target = 0;
	float current = 0;
	
	void  Awake (){
		label = GetComponent<Text> ();
	} 

	void OnEnable () {
		current = SessionInfo.current.GetScore();
	}

	void  Update (){
		target = SessionInfo.current.GetScore();
		current = Mathf.MoveTowards (current, target, Time.unscaledDeltaTime * SessionInfo.current.design.thirdStarScore * 0.3f);
		label.text = Mathf.RoundToInt(current).ToString();
	}
}