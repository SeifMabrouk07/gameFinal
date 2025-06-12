using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class SignUpManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField Input_Username;
    public TMP_InputField Input_Password;
    public TMP_InputField Input_ConfirmPassword;
    public Button Btn_SignUp;
    public Button Btn_GoToSignIn;
    public TMP_Text Txt_SignUpMessage;

    [Header("Scene Names")]
    public string SignInScene = "SignIn";

    void Start()
    {
        // Clear any previous message
        Txt_SignUpMessage.text = "";

        // Wire up button callbacks
        Btn_SignUp.onClick.AddListener(OnSignUpClicked);
        Btn_GoToSignIn.onClick.AddListener(() =>
            SceneManager.LoadScene(SignInScene)
        );
    }

    void OnSignUpClicked()
    {
        // Read and trim inputs
        string u = Input_Username.text.Trim();
        string p = Input_Password.text;
        string c = Input_ConfirmPassword.text;

        // Basic validation
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

        // Disable the button to prevent double‐taps and show progress
        Btn_SignUp.interactable = false;
        Txt_SignUpMessage.text = "Creating account…";

        // Call the NetworkManager to register
        NetworkManager.Instance.Register(u, p, (success, message) =>
        {
            // Re‐enable button and show server response
            Btn_SignUp.interactable = true;
            Txt_SignUpMessage.text = message;

            if (success)
            {
                // On success, go back to Sign In after a brief delay
                Invoke(nameof(ReturnToSignIn), 1f);
            }
        });
    }

    void ReturnToSignIn()
    {
        SceneManager.LoadScene(SignInScene);
    }
}
