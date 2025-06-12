using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Proyecto26;       // RestClient + RequestException
using System;

public class SignUpManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public TMP_InputField Input_ConfirmPassword;
    public Button Btn_SignUp;
    public Button Btn_GoToSignIn;      // ← add this
    public TMP_Text Txt_SignUpMessage;
    public TMP_Text Txt_Status;          // optional “Registering…”

    [Header("Scenes & Server")]
    public string SignInScene = "SignIn";
    public string BaseUrl = "http://localhost:3000";

    [Serializable]
    public class RegisterRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    public class RegisterResponse
    {
        public int id;
        public string username;
    }

    void Start()
    {
        // clear any old messages
        Txt_SignUpMessage.text = "";
        if (Txt_Status) Txt_Status.text = "";

        // wire up both buttons
        Btn_SignUp.onClick.AddListener(OnSignUpClicked);
        Btn_GoToSignIn.onClick.AddListener(OnGoToSignInClicked);
    }

    public void OnSignUpClicked()
    {
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;
        string c = Input_ConfirmPassword.text;

        // Validation
        if (string.IsNullOrEmpty(u) || string.IsNullOrEmpty(p) || string.IsNullOrEmpty(c))
        {
            Txt_SignUpMessage.text = "All fields are required.";
            return;
        }
        if (p != c)
        {
            Txt_SignUpMessage.text = "Passwords must match.";
            return;
        }

        // Show progress
        if (Txt_Status) Txt_Status.text = "Registering…";
        Txt_SignUpMessage.text = "";

        var payload = new RegisterRequest
        {
            username = u,
            password = p
        };

        RestClient
          .Post<RegisterResponse>($"{BaseUrl}/register", payload)
          .Then(res =>
          {
              // Success
              if (Txt_Status) Txt_Status.text = "";
              Txt_SignUpMessage.text = $"Created user “{res.username}”!";

              // after 1s, return to sign in
              Invoke(nameof(ReturnToSignIn), 1f);
          })
          .Catch(err =>
          {
              // Error handling
              if (Txt_Status) Txt_Status.text = "";
              var reqErr = err as RequestException;
              if (reqErr != null && reqErr.StatusCode == 409)
                  Txt_SignUpMessage.text = "That username is already taken.";
              else
                  Txt_SignUpMessage.text = $"Error: {err.Message}";
          });
    }

    // Called when the “Back to Sign In” button is pressed
    void OnGoToSignInClicked()
    {
        SceneManager.LoadScene(SignInScene);
    }

    void ReturnToSignIn()
    {
        SceneManager.LoadScene(SignInScene);
    }
}
