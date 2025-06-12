using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System; // for Action<>

public class SignInManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public Button Btn_SignIn;
    public Button Btn_GoToSignUp;
    public TMP_Text Txt_SignInMessage;

    [Header("Scene Names")]
    public string MainMenuScene = "MainMenu";
    public string SignUpScene = "SignUp";

    void Start()
    {
        Txt_SignInMessage.text = "";

        Btn_SignIn.onClick.AddListener(OnSignInClicked);
        Btn_GoToSignUp.onClick.AddListener(() =>
            SceneManager.LoadScene(SignUpScene)
        );
    }

    void OnSignInClicked()
    {
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;

        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            Txt_SignInMessage.text = "Please enter both username and password.";
            return;
        }

        Txt_SignInMessage.text = "Signing in…";

        NetworkManager.Instance.Login(u, p, (success, message, userId) =>
        {
            // Note: this callback runs on Unity's main thread
            if (success)
            {
                // Optionally store the userId for later API calls
                PlayerPrefs.SetInt("USER_ID", userId);
                PlayerPrefs.SetString("USERNAME", u);
                PlayerPrefs.Save();

                // Load your main menu
                SceneManager.LoadScene(MainMenuScene);
            }
            else
            {
                // Show the error message returned by the server
                Txt_SignInMessage.text = message;
            }
        });
    }
}
