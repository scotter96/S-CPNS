using UnityEngine;
using UnityEngine.UI;

public class Home : MonoBehaviour
{
    GameManager gameManager;

    [System.Serializable]
    public class TopBar
    {
        public Text title;
        public Button backButton;
        public Button accButton;
    }
    public TopBar topBar;

    [System.Serializable]
    public class SettingsWizard
    {
        public Text version;
    }
    public SettingsWizard settingsWizard;

    [System.Serializable]
    public class HomePanel
    {
        public GameObject loading;
        public GameObject settings;
        public GameObject firstStart;
    }
    public HomePanel homePanels;

    void Start()
    {
        ChangeTopbarTitle(Application.productName);
        settingsWizard.version.text = $"version {Application.version}";
    }
    
    void Update()
    {
        if (gameManager == null) {
            gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
            if (gameManager != null && gameManager.isFirstStart)
                OpenWelcomeFirstStart();
        }

        // ********** INPUT HANDLER **********
        if (Input.GetKeyDown(KeyCode.Escape))
            Back();
        // ********** END OF INPUT HANDLER **********
    }

    void OnFormSwitched(string formTag)
    {
        if (formTag.StartsWith("Form-")) {
            string value = "";
            if (formTag.EndsWith("-Home")) {
                topBar.backButton.gameObject.SetActive(false);
                value = Application.productName;
            }
            else {
                topBar.backButton.gameObject.SetActive(true);
                string key = "";
                if (formTag.EndsWith("-Activity"))
                    key = "currentActivity";

                value = gameManager.GetContext(key);
            }
            ChangeTopbarTitle(value);
        }
    }

    void ChangeTopbarTitle(string text)
    {
        topBar.title.text = text;
    }

    void Back()
    {
        string thisPanel = gameManager.GetCurrentPanelName();
        if (thisPanel != "")
            CloseDialog();
        else {
            string thisForm = gameManager.GetCurrentFormName();
            OnFormSwitched(thisForm);
        }
    }

    // ***********************************************************************
    // *   PUBLIC METHODS
    // ***********************************************************************

    public void CloseDialog()
    {
        gameManager.ManagePanel();
    }

    public void OpenWelcomeFirstStart()
    {
        gameManager.ManagePanel(homePanels.firstStart);
        gameManager.isFirstStart = false;
    }

    public void ActionOpenSettings()
    {
        gameManager.ManagePanel(homePanels.settings);
    }

    public void ActionBack()
    {
        gameManager.BackHandler();
        Back();
    }

    public void ActionOpenExtURL(string url)
    {
        Application.OpenURL(url);
    }

    public void DoOpenScene(string name)
    {
        gameManager.OpenScene(name);
    }

    public void DoSwitchForm(string name)
    {
        gameManager.SwitchForm(name);
        OnFormSwitched(name);
    }

    public void UpdateActivity(string value)
    {
        gameManager.AddOrUpdateContext($"currentActivity:{value}");
    }

    public void UpdateQuizType(string value)
    {
        gameManager.AddOrUpdateContext($"currentQuizType:{value}");
    }

    public void UpdateSpeedQuizType(string value)
    {
        gameManager.AddOrUpdateContext($"SpeedQuizType:{value}");
    }

    public void UpdateSpeedQuizTimeType(string value)
    {
        gameManager.AddOrUpdateContext($"SpeedQuizTimeType:{value}");
    }

    public void UpdateSpeedQuizTime(string value)
    {
        gameManager.AddOrUpdateContext($"SpeedQuizTime:{value}");
    }
}