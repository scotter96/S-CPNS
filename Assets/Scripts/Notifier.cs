using System.Collections;
using UnityEngine;

public class Notifier : MonoBehaviour
{
    Animator notifAnimator;
    public string startTriggerName = "StartNotify";
    public string stopTriggerName = "StopNotify";

    public void StartNotify()
    {
        gameObject.SetActive(true);
        notifAnimator = GetComponent<Animator>();
        notifAnimator.SetTrigger(startTriggerName);
        StartCoroutine(WaitAndCloseNotification());
    }

    IEnumerator WaitAndCloseNotification(float waitTime=5f) {
        yield return new WaitForSeconds(waitTime);
        notifAnimator.SetTrigger(stopTriggerName);
        while (gameObject.activeInHierarchy) {
            yield return new WaitForSeconds(waitTime);
            if (notifAnimator.GetCurrentAnimatorStateInfo(0).IsName("Waiting for Trigger"))
                gameObject.SetActive(false);
        }
    }
}