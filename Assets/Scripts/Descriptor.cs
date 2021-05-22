using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Descriptor : MonoBehaviour
{
    public bool isLoading;
    public Text titleText;
    public Text descText;

    bool loadingStarted;

    void Update()
    {
        if (isLoading && !loadingStarted) {
            StartCoroutine(UpdateText());
            loadingStarted = true;
        }
    }

    public void ChangeTitle(string newTitle)
    {
        titleText.text = newTitle;
    }

    public void ChangeDescription(string newDesc)
    {
        descText.text = newDesc;
    }

    // ********** DYNAMIC LOADING TEXT CODES **********
    string[] texts = {
        "Mohon tunggu.",
        "Mohon tunggu..",
        "Mohon tunggu..."
    };
    int iteration;

    IEnumerator UpdateText(float waitTime=2f)
    {
        while (gameObject.activeInHierarchy) {
            yield return new WaitForSeconds(waitTime);
            iteration++;
            if (iteration == texts.Length)
                iteration=0;
            ChangeDescription(texts[iteration]);
        }
    }
}