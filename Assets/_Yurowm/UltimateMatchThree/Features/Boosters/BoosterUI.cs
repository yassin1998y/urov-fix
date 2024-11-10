using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class BoosterUI : MonoBehaviour {
    public static BoosterUI main;

    //public Text text;
    public Button cancel;

    Animation anim;
	void Awake () {
        main = this;
        anim = GetComponent<Animation>();
        gameObject.SetActive(false);
        cancel.onClick.AddListener(Cancel);
    }

    void Cancel() {
        BoosterAssistant.main.Cancel();
    }

    //public void SetMessage(string message) {
    //    text.text = message;
    //}

	public IEnumerator Show () {
        gameObject.SetActive(true);
        yield return StartCoroutine(Play("ScoreBonusShow"));
    }

    public IEnumerator Hide() {
        yield return StartCoroutine(Play("ScoreBonusHide"));
        gameObject.SetActive(false);
    }

    IEnumerator Play(string clip) {
        anim.Play(clip);
        while (clip != "") {
            anim[clip].time += Time.unscaledDeltaTime;
            anim.enabled = true;
            anim.Sample();
            anim.enabled = false;
            if (anim[clip].time >= anim[clip].length)
                clip = "";
            yield return 0;
        }
    }
}
