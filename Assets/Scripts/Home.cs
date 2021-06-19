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
        public Text username;
        public Text email;
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
        topBar.backButton.gameObject.SetActive(false);
    }
    
    void Update()
    {
        if (gameManager == null) {
            gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
            if (gameManager != null) {
                settingsWizard.username.text = $"{gameManager.user.username}";
                settingsWizard.email.text = $"{gameManager.user.email}";
                if (gameManager.isFirstStart)
                    OpenWelcomeFirstStart();
            }
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
                if (formTag.EndsWith("-Activity"))
                    value = gameManager.context.currentActivity;
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
        gameManager.DoSaveToLocal();
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

    public void DoLogout()
    {
        gameManager.Logout();
    }

    public void UpdateActivity(string value)
    {
        gameManager.context.currentActivity = value;
    }

    public void UpdateQuizType(string value)
    {
        gameManager.context.currentQuizType = value;
    }

    public void UpdateSpeedQuizType(string value)
    {
        gameManager.context.currentSpeedQuizType = value;
    }

    public void UpdateSpeedQuizTimeType(string value)
    {
        gameManager.context.currentSpeedQuizTimeType = value;
    }

    public void UpdateSpeedQuizTime(string value)
    {
        gameManager.context.currentSpeedQuizTime = value;
    }
}