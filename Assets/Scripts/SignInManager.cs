using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Proyecto26;     // RestClient + RequestException
using System;        // for [Serializable]

public class SignInManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public Button Btn_SignIn;
    public Button Btn_GoToSignUp;
    public TMP_Text Txt_SignInMessage;
    public TMP_Text Txt_Status;           // optional “Signing in…”

    [Header("Scenes & Server")]
    public string BaseUrl = "http://localhost:3000";
    public string MainMenuScene = "MainMenu";
    public string SignUpScene = "SignUp";

    // 1) Serializable payload and response types for JSON
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class LoginResponse
    {
        public int id;
        public string username;
    }

    void Start()
    {
        // Clear any previous messages
        Txt_SignInMessage.text = "";
        if (Txt_Status) Txt_Status.text = "";

        // Wire up buttons
        Btn_SignIn.onClick.AddListener(OnSignInClicked);
        Btn_GoToSignUp.onClick.AddListener(() =>
            SceneManager.LoadScene(SignUpScene)
        );
    }

    // Hook this to the Sign In button's OnClick() in the Inspector
    public void OnSignInClicked()
    {
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;

        // 2) Client‐side validation
        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p))
        {
            Txt_SignInMessage.text = "Username and password required.";
            return;
        }

        // 3) Show in‐progress status
        if (Txt_Status) Txt_Status.text = "Signing in…";
        Txt_SignInMessage.text = "";

        // 4) Build and log JSON payload
        var payload = new LoginRequest { username = u, password = p };
        Debug.Log($"[SignIn] POST -> {BaseUrl}/login Payload: {JsonUtility.ToJson(payload)}");

        // 5) Send request
        RestClient
          .Post<LoginResponse>($"{BaseUrl}/login", payload)
          .Then(res =>
          {
              // Success: clear status, save user, load MainMenu
              if (Txt_Status) Txt_Status.text = "";
              PlayerPrefs.SetInt("USER_ID", res.id);
              PlayerPrefs.SetString("USERNAME", res.username);
              PlayerPrefs.Save();
              SceneManager.LoadScene(MainMenuScene);
          })
          .Catch(err =>
          {
              // Always clear status
              if (Txt_Status) Txt_Status.text = "";

              // Handle HTTP errors
              var reqErr = err as RequestException;
              if (reqErr != null)
              {
                  if (reqErr.StatusCode == 400)
                      Txt_SignInMessage.text = "Username and password required.";
                  else if (reqErr.StatusCode == 401)
                      Txt_SignInMessage.text = "Invalid username or password.";
                  else
                      Txt_SignInMessage.text = $"Error ({reqErr.StatusCode})";
              }
              else
              {
                  Txt_SignInMessage.text = "Network error.";
              }

              Debug.LogError($"SignIn error: {err}");
          });
    }
}
