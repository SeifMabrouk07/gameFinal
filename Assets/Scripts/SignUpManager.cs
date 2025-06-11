using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SignUpManager : MonoBehaviour
{
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public TMP_InputField Input_ConfirmPassword;
    public Button Btn_SignUp;
    public Button Btn_GoToSignIn;
    public TMP_Text Txt_SignUpMessage;

    const string USER_PREF = "USER_";
    const string PASS_PREF = "PASS_";
    public string SignInScene = "SignIn";

    void Start()
    {
        Txt_SignUpMessage.text = "";
        Btn_SignUp.onClick.AddListener(OnSignUp);
        Btn_GoToSignIn.onClick.AddListener(() =>
            SceneManager.LoadScene(SignInScene)
        );
    }

    void OnSignUp()
    {
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;
        string c = Input_ConfirmPassword.text;
        if (u == "" || p == "" || c == "")
        {
            Txt_SignUpMessage.text = "All fields are required.";
            return;
        }
        if (p != c)
        {
            Txt_SignUpMessage.text = "Passwords must match.";
            return;
        }

        string userKey = USER_PREF + u;
        if (PlayerPrefs.HasKey(userKey))
        {
            Txt_SignUpMessage.text = "Username already exists.";
            return;
        }

        PlayerPrefs.SetString(userKey, u);
        PlayerPrefs.SetString(PASS_PREF + u, p);
        PlayerPrefs.Save();

        Txt_SignUpMessage.text = "Account created! Return to Sign In.";
    }
}
