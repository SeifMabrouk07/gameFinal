using UnityEngine;
using Proyecto26;           // RestClient + RequestException
using System;
using UnityEngine.SceneManagement;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    [Tooltip("Point this at your Node.js server (e.g. http://localhost:3000)")]
    public string BaseUrl = "http://localhost:3000";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);
    }

    // ─── Payload / Response DTOs ─────────────────────────────────────────

    [Serializable]
    public class RegisterRequest { public string username; public string password; }
    [Serializable]
    public class RegisterResponse { public int id; public string username; }

    [Serializable]
    public class LoginRequest { public string username; public string password; }
    [Serializable]
    public class LoginResponse { public int id; public string username; }

    [Serializable]
    public class ProgressRequest { public int userId; public int level; public float percent; }
    [Serializable]
    public class ProgressResponse { public float bestPercent; }

    // ─── Authentication ─────────────────────────────────────────────────

    /// <summary>
    /// Register a new user.
    /// </summary>
    public void Register(string username, string password, Action<bool, string> callback)
    {
        var req = new RegisterRequest { username = username, password = password };
        RestClient
          .Post<RegisterResponse>($"{BaseUrl}/register", req)
          .Then(res => callback(true, $"Welcome, {res.username}!"))
          .Catch(err =>
          {
              var re = err as RequestException;
              if (re != null && re.StatusCode == 409)
                  callback(false, "Username already taken.");
              else
                  callback(false, $"Error: {err.Message}");
          });
    }

    /// <summary>
    /// Login an existing user.
    /// </summary>
    public void Login(string username, string password, Action<bool, string, int> callback)
    {
        var req = new LoginRequest { username = username, password = password };
        RestClient
          .Post<LoginResponse>($"{BaseUrl}/login", req)
          .Then(res => callback(true, null, res.id))
          .Catch(err =>
          {
              var re = err as RequestException;
              if (re != null && re.StatusCode == 401)
                  callback(false, "Invalid username or password.", -1);
              else if (re != null && re.StatusCode == 400)
                  callback(false, "Username and password required.", -1);
              else
                  callback(false, $"Error: {err.Message}", -1);
          });
    }

    // ─── Progression ────────────────────────────────────────────────────

    /// <summary>
    /// Gets the bestPercent for a given user & level.
    /// </summary>
    public void GetProgress(int userId, int level, Action<bool, string, float> callback)
    {
        RestClient
          .Get<ProgressResponse>($"{BaseUrl}/progress/{userId}/{level}")
          .Then(res => callback(true, null, res.bestPercent))
          .Catch(err => callback(false, err.Message, 0f));
    }

    /// <summary>
    /// Uploads this run’s percent; server will keep the max.
    /// </summary>
    public void UpdateProgress(int userId, int level, float percent, Action<bool, string, float> callback)
    {
        var req = new ProgressRequest { userId = userId, level = level, percent = percent };
        RestClient
          .Put<ProgressResponse>($"{BaseUrl}/progress", req)
          .Then(res => callback(true, null, res.bestPercent))
          .Catch(err => callback(false, err.Message, percent));
    }
}
