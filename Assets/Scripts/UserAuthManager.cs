using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class UserAuthManager : MonoBehaviour
{
    [Header("Sign In UI References")]
    public TMP_InputField Input_Username_SignIn;
    public TMP_InputField Input_Password_SignIn;
    public Button Btn_SignIn;
    public TMP_Text Txt_SignInMessage;

    [Header("Sign Up UI References")]
    public TMP_InputField Input_Username_SignUp;
    public TMP_InputField Input_Password_SignUp;
    public TMP_InputField Input_ConfirmPassword_SignUp;
    public Button Btn_SignUp;
    public TMP_Text Txt_SignUpMessage;

    // Name of the Scene to load after successful sign-in/up:
    public string NextSceneName = "MainMenu";

    // Keys for PlayerPrefs (prefix to avoid collisions)
    const string PREF_USERNAME_PREFIX = "USER_";      // e.g. USER_alice
    const string PREF_PASSWORD_PREFIX = "PASS_";      // e.g. PASS_alice

    void Start()
    {
        // Clear any leftover messages:
        Txt_SignInMessage.text = "";
        Txt_SignUpMessage.text = "";

        // Hook up the button click listeners:
        Btn_SignIn.onClick.AddListener(OnSignInClicked);
        Btn_SignUp.onClick.AddListener(OnSignUpClicked);
    }

    void OnDestroy()
    {
        Btn_SignIn.onClick.RemoveListener(OnSignInClicked);
        Btn_SignUp.onClick.RemoveListener(OnSignUpClicked);
    }

    #region Sign In

    void OnSignInClicked()
    {
        string username = Input_Username_SignIn.text.Trim();
        string password = Input_Password_SignIn.text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            Txt_SignInMessage.text = "Please fill in both fields.";
            return;
        }

        // Build PlayerPrefs keys
        string userKey = PREF_USERNAME_PREFIX + username;
        string passKey = PREF_PASSWORD_PREFIX + username;

        // Check if user exists
        if (!PlayerPrefs.HasKey(userKey))
        {
            Txt_SignInMessage.text = "User not found. Please sign up first.";
            return;
        }

        // Retrieve stored password (in real app you’d hash & salt)
        string storedPassword = PlayerPrefs.GetString(passKey, "");

        if (password == storedPassword)
        {
            Txt_SignInMessage.text = "Sign in successful!";
            // Optionally: store “current user” in PlayerPrefs
            PlayerPrefs.SetString("CURRENT_USER", username);

            // Load next scene
            StartCoroutine(DelayedLoadNextScene());
        }
        else
        {
            Txt_SignInMessage.text = "Invalid password.";
        }
    }

    IEnumerator DelayedLoadNextScene()
    {
        // Small delay so user can see “Success!” message
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(NextSceneName);
    }

    #endregion

    #region Sign Up

    void OnSignUpClicked()
    {
        string username = Input_Username_SignUp.text.Trim();
        string password = Input_Password_SignUp.text;
        string confirmPassword = Input_ConfirmPassword_SignUp.text;

        if (string.IsNullOrEmpty(username) ||
            string.IsNullOrEmpty(password) ||
            string.IsNullOrEmpty(confirmPassword))
        {
            Txt_SignUpMessage.text = "All fields are required.";
            return;
        }

        if (password != confirmPassword)
        {
            Txt_SignUpMessage.text = "Passwords do not match.";
            return;
        }

        // Build PlayerPrefs keys
        string userKey = PREF_USERNAME_PREFIX + username;
        string passKey = PREF_PASSWORD_PREFIX + username;

        // Check if username already exists
        if (PlayerPrefs.HasKey(userKey))
        {
            Txt_SignUpMessage.text = "Username already taken.";
            return;
        }

        // Save to PlayerPrefs (in real life: hash & salt first)
        PlayerPrefs.SetString(userKey, username);
        PlayerPrefs.SetString(passKey, password);
        PlayerPrefs.Save();

        Txt_SignUpMessage.text = "Sign up successful! You can sign in now.";

        // (Optionally) clear fields:
        Input_Username_SignUp.text = "";
        Input_Password_SignUp.text = "";
        Input_ConfirmPassword_SignUp.text = "";
    }

    #endregion
}
