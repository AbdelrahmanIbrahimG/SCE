using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.Networking;
using TMPro;
using System.Threading.Tasks;

public class ARCameraCaptureController : MonoBehaviour
{
    public Camera arCamera; // Assign the AR camera
    public RawImage rawImage; // Reference to RawImage in UI
    public VideoPlayer videoPlayer; // Reference to VideoPlayer
    public TMP_Text textObject; // Reference to the Text component
    private float currentVolume = 0.5f; // Initial volume
    private string lastGesture = ""; // To prevent repeated actions for the same gesture
    private Queue<string> gestureQueue = new Queue<string>(); // Queue for gesture commands
    private RenderTexture reusableRenderTexture;
    private Texture2D reusableTexture;

    void Start()
    {
        // Initialize reusable RenderTexture and Texture2D
        reusableRenderTexture = new RenderTexture(640, 360, 24);
        reusableTexture = new Texture2D(640, 360, TextureFormat.RGB24, false);

        // Start capturing frames periodically
        InvokeRepeating(nameof(CaptureFrameAndSend), 0f, 0.4f); // Adjust interval as needed
    }

    void CaptureFrameAndSend()
    {
        if (arCamera == null)
        {
            Debug.LogError("AR Camera is not assigned!");
            return;
        }

        // Set the camera's target texture
        arCamera.targetTexture = reusableRenderTexture;

        // Render the camera's view
        arCamera.Render();

        // Capture the frame into the reusable Texture2D
        RenderTexture.active = reusableRenderTexture;
        reusableTexture.ReadPixels(new Rect(0, 0, reusableRenderTexture.width, reusableRenderTexture.height), 0, 0);
        reusableTexture.Apply();

        // Clear target texture to avoid warnings
        arCamera.targetTexture = null;
        RenderTexture.active = null;

        // Send the frame to the Flask API asynchronously
        StartCoroutine(SendFrameToAPI(reusableTexture));
    }


    IEnumerator SendFrameToAPI(Texture2D frame)
    {
        Debug.Log("Sending frame to API");

        // Encode texture to PNG
        byte[] imageBytes = frame.EncodeToPNG();
        WWWForm form = new WWWForm();
        form.AddBinaryData("frame", imageBytes, "frame.jpg", "image/jpeg");

        // Send the POST request asynchronously
        UnityWebRequest www = UnityWebRequest.Post("http://192.168.1.30:5000", form);
        yield return www.SendWebRequest();

        Debug.Log(www.downloadHandler.text);

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Image sent successfully!");

            string responseText = www.downloadHandler.text;
            GestureResponse response = JsonUtility.FromJson<GestureResponse>(responseText);
            Debug.Log("Gesture: " + response);
            if (!string.IsNullOrEmpty(response.gesture))
            {
                lock (gestureQueue)
                {
                    gestureQueue.Enqueue(response.gesture);
                    Debug.Log("Gesture: " + response.gesture);
                }
            }
        }
        else
        {
            Debug.LogError("Error: " + www.error);
        }
    }

    void Update()
    {
        // Process gestures from the queue
        if (gestureQueue.Count > 0)
        {
            string gesture;
            lock (gestureQueue)
            {
                gesture = gestureQueue.Dequeue();
            }
            ControlVideo(gesture);
        }
    }

    private void ControlVideo(string gesture)
    {
        double targetTime;

        // Check gesture and perform corresponding actions
        switch (gesture)
        {
            case "playback":
                if (!videoPlayer.isPlaying)
                {
                    videoPlayer.Play();
                    textObject.text = "Playing";
                }
                else
                {
                    videoPlayer.Pause();
                    textObject.text = "Paused";
                }
                break;

            case "skip":
                targetTime = videoPlayer.time + 5;
                videoPlayer.time = Mathf.Clamp((float)targetTime, 0, (float)videoPlayer.length);
                textObject.text = "Skipped";
                break;

            case "drawback":
                targetTime = videoPlayer.time - 5;
                videoPlayer.time = Mathf.Clamp((float)targetTime, 0, (float)videoPlayer.length);
                textObject.text = "Drawback";
                break;

            case "volumeup":
                currentVolume = Mathf.Clamp(currentVolume + 0.1f, 0f, 1f);
                videoPlayer.SetDirectAudioVolume(0, currentVolume);
                textObject.text = "Volume Up";
                break;

            case "volumedown":
                currentVolume = Mathf.Clamp(currentVolume - 0.1f, 0f, 1f);
                videoPlayer.SetDirectAudioVolume(0, currentVolume);
                textObject.text = "Volume Down";
                break;

            default:
                textObject.text = "none";
                StartCoroutine(HideTextAfterDelay(2f)); // Hide text after 2 seconds if "none"
                break;
        }
    }

    private IEnumerator HideTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the specified delay
        textObject.text = ""; // Clear the text
    }

    private void OnDestroy()
    {
        // Cleanup reusable resources
        if (reusableRenderTexture != null) reusableRenderTexture.Release();
        if (reusableTexture != null) Destroy(reusableTexture);
    }

    [System.Serializable]
    public class GestureResponse
    {
        public string gesture;
    }
}
