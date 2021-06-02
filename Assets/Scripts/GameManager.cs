using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;

public class GameManager : MonoBehaviour
{
    static GameManager instance;

    [Tooltip("Check if local storage is to be used.")]
    public bool useLocalStorage = true;

    DatabaseReference rootRef;
    FirebaseDatabase database;
    Encryptor encryptor;
    bool startInitFirebase;
    public string[] tableNames = { "users" };
    public List<string> users;

    string activePanelName;
    string activeFormName;
    [Tooltip("Set the previous scene for each scenes in the build. Format: <Current Scene>,<Previous Scene>.")]
    public string[] previousScenes;
    [Tooltip("Set the previous form for each forms in the scene. To know what form an object is, see the tags started with 'Form-' prefix. Format: <Current Form>,<Previous Form>. For forms that have no previous, don't insert here.")]
    public string[] previousForms;

    Dictionary<string,string> context;
    // * context keys:
    // * "currentActivity",
    // * "currentQuizType",
    // * "currentSpeedQuizType",
    // * "currentSpeedQuizTimeType",
    // * "currentSpeedQuizTime",

    // * Active User
    [System.Serializable]
    public class User
    {
        public string email;
        public string username;
        public string password;
    }
    public User user;

    [System.Serializable]
    public class NotificationPrefab
    {
        public GameObject greenColor;
        public GameObject yellowColor;
        public GameObject redColor;
    }
    public NotificationPrefab notificationPrefab;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);

        // ? Make this Game Object permanent
        DontDestroyOnLoad(gameObject);

        // ? Get the root reference location of the database.
        rootRef = FirebaseDatabase.DefaultInstance.RootReference;
        database = rootRef.Database;
        encryptor = GetComponent<Encryptor>();
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(GetDependencyResult);
    }

    void GetDependencyResult(Task<DependencyStatus> task)
    {
        var dependencyStatus = task.Result;
        if (dependencyStatus == Firebase.DependencyStatus.Available)
        {
            startInitFirebase = true;
        }
        else
        {
            UnityEngine.Debug.LogError(System.String.Format(
            "Could not resolve all Firebase dependencies: {0}", dependencyStatus));
            // ! Firebase Unity SDK is not safe to use here.
        }
    }

    void Update()
    {
        if (startInitFirebase)
        {
            foreach (string tableName in tableNames)
                database.GetReference(tableName).ValueChanged += DBRead;
            startInitFirebase = false;
        }

        // ********** INPUT HANDLER **********
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            BackHandler();
        }
        // ********** END OF INPUT HANDLER **********
    }

    public void BackHandler()
    {
        if (activePanelName != "")
            ManagePanel();
        else
            GoBack();
    }

    void GoBack()
    {
        string goToForm = "";
        if (activeFormName != "")
        {
            Dictionary<string, string> forms = new Dictionary<string, string>();
            foreach (string form in previousForms)
            {
                string[] strList = form.Split(',');
                forms.Add(strList[0], strList[1]);
            }
            if (forms.TryGetValue(activeFormName, out goToForm))
                SwitchForm(goToForm);
        }
        if (goToForm == "")
        {
            Dictionary<string, string> scenes = new Dictionary<string, string>();
            foreach (string scene in previousScenes)
            {
                string[] strList = scene.Split(',');
                scenes.Add(strList[0], strList[1]);
            }
            string goToScene = "";
            if (scenes.TryGetValue(SceneManager.GetActiveScene().name, out goToScene))
                OpenScene(goToScene);
            else
                ExitApp();
        }
    }

    public void SwitchForm(string target, bool reset = true)
    {
        activeFormName = target;
        Transform mainForm = GameObject.FindWithTag("Form-Main").transform;
        for (int i = 0; i < mainForm.childCount; i++)
        {
            Transform j = mainForm.GetChild(i);
            if (!j.tag.StartsWith("Form-"))
                continue;
            j.gameObject.SetActive(
                j.tag == target
            );
        }
        if (reset)
            ResetForm();
    }

    void ResetForm()
    {
        InputField[] inputFields = GameObject.FindWithTag(activeFormName).GetComponentsInChildren<InputField>();
        foreach (InputField field in inputFields)
        {
            field.text = string.Empty;
            field.ForceLabelUpdate();
        }
    }

    public void OpenScene(string name)
    {
        ManagePanel();
        activePanelName = "";
        activeFormName = "";
        SceneManager.LoadScene(name);
    }

    public string GetCurrentPanelName()
    {
        return activePanelName;
    }

    public string GetCurrentFormName()
    {
        return activeFormName;
    }

    public void ExitApp()
    {
        if (useLocalStorage)
            SaveToLocal();
        Application.Quit();
    }

    public void ManagePanel(GameObject panelObj = null)
    {
        GameObject[] allPanels = GameObject.FindGameObjectsWithTag("Panel");
        foreach (GameObject panel in allPanels)
        {
            panel.SetActive(false);
        }
        activePanelName = "";

        if (panelObj != null)
        {
            panelObj.SetActive(true);
            activePanelName = panelObj.name;
        }
    }

    public void ShowNotification(int level, string message)
    // ? Levels: 0 - Normal (green), 1 - Warning (yellow), 2 - Critical (red)
    {
        GameObject prefab = notificationPrefab.greenColor;
        if (level == 1)
            prefab = notificationPrefab.yellowColor;
        else if (level == 2)
            prefab = notificationPrefab.redColor;
        GameObject notificationObj = Instantiate(prefab,transform.position,Quaternion.identity) as GameObject;
        RectTransform mainForm = GameObject.FindWithTag("Form-Main").transform as RectTransform;
        notificationObj.transform.SetParent(mainForm);
        notificationObj.GetComponent<Descriptor>().ChangeDescription(message);
        notificationObj.GetComponent<Notifier>().StartNotify();
    }

    // * Load a Sprite from Resources (e.g. Assets/Resources/Products/Cashew)
    public static Sprite GetSprite(string filename)
    {
        return Resources.Load<Sprite>($"Products/{filename}");
    }

    // ***********************************************************************
    // *   CONTEXT CODES (Used for interscene data exchange)
    // ***********************************************************************

    public string GetContext(string key)
    {
        string value = "";
        bool valueExists = context.TryGetValue(key, out value);
        return value;
    }

    public void AddOrUpdateContext(string contextValue)
    {
        string[] contextValues = contextValue.Split(':');
        string key = contextValues[0];
        string value = contextValues[1];
        if (context.ContainsKey(key))
            context[key] = value;
        else
            context.Add(key, value);
    }

    public void DeleteContext(string key="")
    {
        if (key != "")
            context.Remove(key);
        else
            context.Clear();
    }

    // ***********************************************************************
    // *    USER & DATA CODES
    // ***********************************************************************

    public bool CreateUser(string email, string username, string password)
    {
        bool write = false;

        // ? Update the User object with parameters received
        user.email = email;
        user.username = username;
        user.password = encryptor.Encrypt(password);

        // ? Write the new user data to database
        string json = JsonUtility.ToJson(user);
        write = DBWrite("users", json);

        // ? Flush the User object parameters (user needs to login first to use the newly created account)
        user.email = string.Empty;
        user.username = string.Empty;
        user.password = string.Empty;

        return write;
    }

    public void LoginSuccess(string username, string password)
    {
        user.username = username;
        user.password = password;

        if (useLocalStorage)
            SaveToLocal();
    }

    public void LogoutSuccess()
    {
        ResetSave();
    }

    // **************** FIREBASE REALTIME DATABASE CODES ****************

    void DBRead(object sender, ValueChangedEventArgs args)
    {
        // * Error Check
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }

        // * Fetch the datas & flush the old ones
        string tableName = args.Snapshot.Key;
        IEnumerable<DataSnapshot> datas = args.Snapshot.Children;
        if (tableName == "users")
            users.Clear();

        // * Insert new datas
        foreach (DataSnapshot data in datas)
        {
            string json = data.GetRawJsonValue();
            if (tableName == "users" && !users.Contains(json))
                users.Add(json);
        }
    }

    // ? This function returns boolean as the result. Can be used to update the whole record, or just desired field
    bool DBWrite(string tableName, string json = "", string pkId = "", string key = "", string value = "")
    {
        if (json != "" && pkId == "" && key == "")
        {
            pkId = (users.Count).ToString("");
            rootRef.Child(tableName).Child(pkId).SetRawJsonValueAsync(json);
            return true;
        }
        else if (json == "" && pkId != "" && key != "")
        {
            rootRef.Child(tableName).Child(pkId).Child(key).SetValueAsync(value);
            return true;
        }
        else
        {
            Debug.LogError($"Error writing {tableName}! Params:\njson: {json}\npkId: {pkId}\nkey: {key}\nvalue: {value}");
            return false;
        }
    }

    // **************** LOCAL STORAGE CODES ****************

    public static string localStorageFilename = "localProfile.fscpns";
    string path = "";

    void Start()
    {
        // ? Load datas from Local
        if (useLocalStorage)
        {
            path = $"{Application.persistentDataPath}/{localStorageFilename}";
            LoadFromLocal();
        }
    }

    void LoadFromLocal()
    {
        if (File.Exists(path))
        {
            BinaryFormatter bf = new BinaryFormatter();
            FileStream file = File.Open(path, FileMode.Open);
            SaveData data = (SaveData)bf.Deserialize(file);
            file.Close();

            // * Start of contents to load
            user.email = data.email;
            user.username = data.username;
            user.password = data.password;
            // * End of contents to load
        }
        else
            Debug.LogWarning("No local save found.");
    }

    void SaveToLocal()
    {
        BinaryFormatter bf = new BinaryFormatter();
        FileStream file = File.Create(path);
        SaveData data = new SaveData();

        // * Start of contents to save
        data.email = user.email;
        data.username = user.username;
        data.password = user.password;
        // * End of contents to save

        bf.Serialize(file, data);
        file.Close();
    }

    public void ResetSave()
    {
        user.email = string.Empty;
        user.username = string.Empty;
        user.password = string.Empty;

        if (useLocalStorage)
            SaveToLocal();
    }
}

[Serializable]
class SaveData
{
    public string email;
    public string username;
    public string password;
}