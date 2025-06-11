using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SignInManager : MonoBehaviour
{
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public Button Btn_SignIn;
    public Button Btn_GoToSignUp;
    public TMP_Text Txt_SignInMessage;

    const string USER_PREF = "USER_";
    const string PASS_PREF = "PASS_";
    public string MainMenuScene = "MainMenu";
    public string SignUpScene = "SignUp";

    void Start()
    {
        Txt_SignInMessage.text = "";
        Btn_SignIn.onClick.AddListener(OnSignIn);
        Btn_GoToSignUp.onClick.AddListener(() =>
            SceneManager.LoadScene(SignUpScene)
        );
    }

    void OnSignIn()
    {
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;
        if (u == "" || p == "")
        {
            Txt_SignInMessage.text = "Please enter both fields.";
            return;
        }

        string userKey = USER_PREF + u;
        string passKey = PASS_PREF + u;
        if (!PlayerPrefs.HasKey(userKey))
        {
            Txt_SignInMessage.text = "User not found.";
            return;
        }

        if (PlayerPrefs.GetString(passKey) == p)
        {
            Txt_SignInMessage.text = "Success!";
            // save current user if needed
            PlayerPrefs.SetString("CURRENT_USER", u);
            PlayerPrefs.Save();
            SceneManager.LoadScene(MainMenuScene);
        }
        else
        {
            Txt_SignInMessage.text = "Incorrect password.";
        }
    }
}
