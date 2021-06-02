using System.Collections.Generic;
using System.Threading.Tasks;
using Google;
using UnityEngine;
using UnityEngine.UI;

public class Login : MonoBehaviour
{
    public string WEB_CLIENT_ID = "<your client id here>";

    GoogleSignInConfiguration configuration;
    GameManager gameManager;

    [System.Serializable]
    public class FormLogin
    {
        public InputField usernameField;
        public InputField passwordField;
        public Image showPasswordImage;
        public Sprite showPasswordSprite;
        public Sprite hidePasswordSprite;
    }
    public FormLogin loginForm;

    [System.Serializable]
    public class FormRegister
    {
        public InputField emailField;
        public InputField usernameField;
        public InputField passwordField;
    }
    public FormRegister registerForm;

    [System.Serializable]
    public class LoginPanel
    {
        public GameObject loading;
        public GameObject wrongLogin;
        public GameObject userExists;
        public GameObject nullFields;
        public GameObject incorrectEmailFormat;
    }
    public LoginPanel loginPanels;

    // Defer the configuration creation until Awake so the web Client ID
    // Can be set via the property inspector in the Editor.
    void Awake()
    {
        configuration = new GoogleSignInConfiguration
        {
            WebClientId = WEB_CLIENT_ID,
            RequestIdToken = true
        };
    }

    // Update is called once per frame
    void Update()
    {
        if (gameManager == null)
            gameManager = GameObject.FindWithTag("GameController").GetComponent<GameManager>();
    }

    public void DoOpenScene(string name)
    {
        gameManager.OpenScene(name);
    }

    public void DoSwitchForm(string name)
    {
        gameManager.SwitchForm(name);
    }

    public void CloseDialog()
    {
        gameManager.ManagePanel();
    }

    public void ShowPassword(InputField field)
    {
        if (field.contentType == InputField.ContentType.Password) {
            field.contentType = InputField.ContentType.Standard;
            loginForm.showPasswordImage.sprite = loginForm.showPasswordSprite;
        }
        else if (field.contentType == InputField.ContentType.Standard) {
            field.contentType = InputField.ContentType.Password;
            loginForm.showPasswordImage.sprite = loginForm.hidePasswordSprite;
        }
        field.ForceLabelUpdate();
    }

    // ********** LOGIN CODES **********
    public void StartLogin()
    {
        bool loginPassed = false;
        string user = "";
        if (loginForm.usernameField.text != "" && loginForm.passwordField.text != "")
        {
            gameManager.ManagePanel(loginPanels.loading);
            // ? Check credentials inputted by user
            foreach (string userData in gameManager.users)
            {
                GameManager.User userObj = CreateUserFromJSON(userData);
                // ? If the username & password match a record,
                if (userObj.username == loginForm.usernameField.text || userObj.email == loginForm.usernameField.text)
                {
                    string decryptedPassword = gameManager.GetComponent<Encryptor>().Decrypt(userObj.password);
                    if (decryptedPassword == loginForm.passwordField.text)
                    {
                        // ? Then passed the login and redirected to main menu
                        loginPassed = true;
                        user = userObj.username;
                        break;
                    }
                }
            }
            // ? Else, show the wrong warning popup
            if (!loginPassed)
            {
                gameManager.ManagePanel(loginPanels.wrongLogin);
            }
            else
                PostLogin(user);
        }
        else
            gameManager.ManagePanel(loginPanels.nullFields);
    }

    void PostLogin(string user)
    {
        gameManager.LoginSuccess(
            loginForm.usernameField.text,
            loginForm.passwordField.text
        );
        CloseDialog();
        gameManager.OpenScene("Home");
        // TODO: Fix notification below to work between scenes:
        // gameManager.ShowNotification(0,$"Login berhasil. Selamat datang, {user}!");
    }
    // ********** END OF LOGIN CODES **********

    // ********** REGISTER CODES **********
    public void StartRegister()
    {
        string attemptError = "";

        if (registerForm.usernameField.text != "" && registerForm.passwordField.text != "" && registerForm.emailField.text != "")
        {
            // ? Check the email input
            if (!registerForm.emailField.text.Contains(".") || !registerForm.emailField.text.Contains("@"))
                gameManager.ManagePanel(loginPanels.incorrectEmailFormat);
            else
            {
                gameManager.ManagePanel(loginPanels.loading);
                // ? Check credentials inputted by user
                foreach (string userData in gameManager.users)
                {
                    GameManager.User userObj = CreateUserFromJSON(userData);
                    // ? If the username or email match a record,
                    if (userObj.email == registerForm.emailField.text)
                    {
                        // ? Then mark this attempt as failed
                        attemptError = "Email";
                        break;
                    }
                    else if (userObj.username == registerForm.usernameField.text)
                    {
                        // ? Then mark this attempt as failed
                        attemptError = "Username";
                        break;
                    }
                }
                // ? If indeed failed, show the warning popup
                if (attemptError != "")
                {
                    gameManager.ManagePanel(loginPanels.userExists);
                    loginPanels.userExists.GetComponent<Descriptor>().ChangeDescription($"{attemptError} sudah digunakan.");
                }
                else
                    PostRegister();
            }
        }
        // ? If one of the required fields are empty, also mark this attempt as failed
        else
        {
            attemptError = "Null";
            gameManager.ManagePanel(loginPanels.nullFields);
        }
    }

    void PostRegister()
    {
        // ? When the data is written on database, redirect to Login screen
        if (gameManager.CreateUser(
            username: registerForm.usernameField.text,
            password: registerForm.passwordField.text,
            email: registerForm.emailField.text
        ))
        {
            CloseDialog();
            gameManager.ShowNotification(0,$"Username {registerForm.usernameField.text} berhasil didaftarkan!");
            DoSwitchForm("Form-Login");
        }
    }
    // ********** END OF REGISTER CODES **********

    GameManager.User CreateUserFromJSON(string json)
    {
        GameManager.User user = JsonUtility.FromJson<GameManager.User>(json);
        return user;
    }

    // ********** GOOGLE SIGN-IN CODES **********
    public void OnSignIn()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
          OnAuthenticationFinished);
    }

    public void OnSignOut()
    {
        GoogleSignIn.DefaultInstance.SignOut();
    }

    public void OnDisconnect()
    {
        GoogleSignIn.DefaultInstance.Disconnect();
    }

    internal void OnAuthenticationFinished(Task<GoogleSignInUser> task)
    {
        if (task.IsFaulted)
        {
            using (IEnumerator<System.Exception> enumerator =
                    task.Exception.InnerExceptions.GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    GoogleSignIn.SignInException error =
                            (GoogleSignIn.SignInException)enumerator.Current;
                    gameManager.ShowNotification(2,$"Got Error: {error.Status} {error.Message}");
                }
                else
                {
                    gameManager.ShowNotification(2,$"Got Unexpected Exception: {task.Exception}");
                }
            }
        }
        else if (task.IsCanceled)
        {
            gameManager.ShowNotification(1,"Masuk dengan Google dibatalkan.");
        }
        else
        {
            PostLogin(task.Result.DisplayName);
        }
    }

    public void OnSignInSilently()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = false;
        GoogleSignIn.Configuration.RequestIdToken = true;

        GoogleSignIn.DefaultInstance.SignInSilently()
              .ContinueWith(OnAuthenticationFinished);
    }


    public void OnGamesSignIn()
    {
        GoogleSignIn.Configuration = configuration;
        GoogleSignIn.Configuration.UseGameSignIn = true;
        GoogleSignIn.Configuration.RequestIdToken = false;

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(
          OnAuthenticationFinished);
    }
    // ********** END OF GOOGLE SIGN-IN CODES **********
}