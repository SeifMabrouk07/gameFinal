using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class AuthResponse
{
    public int id;
    public string username;
}

[Serializable]
public class ErrorResponse
{
    public string error;
}

[Serializable]
public class UserData
{
    public int id;
    public string username;
    public int score;
}

[Serializable]
class MessageResponse
{
    public string message;
}

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server Settings")]
    public string BaseUrl = "http://localhost:3000";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
            Destroy(gameObject);
    }

    // --------- PUBLIC API ---------

    public void Register(string username, string password, Action<bool, string> callback)
        => StartCoroutine(_Register(username, password, callback));

    public void Login(string username, string password, Action<bool, string, int> callback)
        => StartCoroutine(_Login(username, password, callback));

    public void GetUser(int userId, Action<bool, string, UserData> callback)
        => StartCoroutine(_GetUser(userId, callback));

    public void UpdateScore(int userId, int newScore, Action<bool, string> callback)
        => StartCoroutine(_UpdateScore(userId, newScore, callback));

    public void DeleteUser(int userId, Action<bool, string> callback)
        => StartCoroutine(_DeleteUser(userId, callback));

    // --------- COROUTINES ---------

    IEnumerator _Register(string username, string password, Action<bool, string> cb)
    {
        var payload = JsonUtility.ToJson(new { username, password });
        using var www = new UnityWebRequest($"{BaseUrl}/register", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(payload);
        www.uploadHandler = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            var err = TryParseError(www.downloadHandler.text);
            cb(false, err);
        }
        else cb(true, "Registration successful");
    }

    IEnumerator _Login(string username, string password, Action<bool, string, int> cb)
    {
        var payload = JsonUtility.ToJson(new { username, password });
        using var www = new UnityWebRequest($"{BaseUrl}/login", "POST");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(payload);
        www.uploadHandler = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            var err = TryParseError(www.downloadHandler.text);
            cb(false, err, -1);
        }
        else
        {
            var auth = JsonUtility.FromJson<AuthResponse>(www.downloadHandler.text);
            cb(true, "Login successful", auth.id);
        }
    }

    IEnumerator _GetUser(int userId, Action<bool, string, UserData> cb)
    {
        using var www = UnityWebRequest.Get($"{BaseUrl}/user/{userId}");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            var err = TryParseError(www.downloadHandler.text);
            cb(false, err, null);
        }
        else
        {
            var user = JsonUtility.FromJson<UserData>(www.downloadHandler.text);
            cb(true, "User data retrieved", user);
        }
    }

    IEnumerator _UpdateScore(int userId, int newScore, Action<bool, string> cb)
    {
        var payload = JsonUtility.ToJson(new { id = userId, score = newScore });
        using var www = new UnityWebRequest($"{BaseUrl}/update-score", "PUT");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(payload);
        www.uploadHandler = new UploadHandlerRaw(body);
        www.downloadHandler = new DownloadHandlerBuffer();
        www.SetRequestHeader("Content-Type", "application/json");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            var err = TryParseError(www.downloadHandler.text);
            cb(false, err);
        }
        else
        {
            var msg = JsonUtility.FromJson<MessageResponse>(www.downloadHandler.text);
            cb(true, msg.message);
        }
    }

    IEnumerator _DeleteUser(int userId, Action<bool, string> cb)
    {
        using var www = UnityWebRequest.Delete($"{BaseUrl}/user/{userId}");
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            var err = TryParseError(www.downloadHandler.text);
            cb(false, err);
        }
        else
        {
            var msg = JsonUtility.FromJson<MessageResponse>(www.downloadHandler.text);
            cb(true, msg.message);
        }
    }

    // --------- HELPERS ---------

    string TryParseError(string json)
    {
        try
        {
            var e = JsonUtility.FromJson<ErrorResponse>(json);
            return e.error ?? "Unknown error";
        }
        catch
        {
            return "Server error";
        }
    }
}
