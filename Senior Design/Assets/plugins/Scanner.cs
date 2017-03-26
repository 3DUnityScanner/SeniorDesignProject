
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Windows;

public class Scanner : EditorWindow
{

    string fileName = "FileName";
    string status = "Idle";
    string recordButton = "Scan";
    bool recording = false;
    bool snapTo = false;
    float lastFrameTime = 0.0f;
    int capturedFrame = 0;
    Texture2D tex = null;
    byte[] fileData;
    int selected = 0;
    string[] options = new string[]
    {
        "Intel Camera", "Kinect",
    };
    bool showPosition = true;
    string stat = "Advanced Options";

    [MenuItem("Window/3D Scanner")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(Scanner));
    }

    void OnGUI()
    {
        selected = EditorGUILayout.Popup("Choose a Camera", selected, options);

        GUILayout.BeginArea(new Rect((Screen.width / 2) - 50, 50, 100, 100));
        {
            if (GUILayout.Button(recordButton, GUILayout.Width(100)))
            {
                if (recording)
                { //recording
                    status = "Idle...";
                    recordButton = "Scan";
                    recording = false;
                }
                else
                { // idle
                    capturedFrame = 0;
                    recordButton = "Stop";
                    recording = true;
                }
            }
        }
        GUILayout.EndArea();

        EditorGUILayout.LabelField("Status: ", status);

        snapTo = GUILayout.Toggle(snapTo, "Snap to Objects");

        GUILayout.BeginArea(new Rect(195, 195, 100, 100));
        {
            EditorGUILayout.LabelField("Camera Feed:");
        }
        GUILayout.EndArea();

        if (System.IO.File.Exists("D:\\scannerBlocks.jpg"))
        {
            fileData = System.IO.File.ReadAllBytes("D:\\scannerBlocks.jpg");
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }

        EditorGUI.DrawPreviewTexture(new Rect(300, 200, 400, 400), tex);

        showPosition = EditorGUILayout.Foldout(showPosition, stat);
        if (showPosition)
            if (true)
            {
                    EditorGUILayout.Vector3Field("Position", new Vector3(4, 5, 6));
            }

        if (!Selection.activeTransform)
        {
            stat = "Advanced Options";
            showPosition = false;
        }

    }

    private void Update()
    {
        if (recording)
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
            {
                RecordImages();
                Repaint();
            }
            else
                status = "Waiting for Editor to Play";
        }
    }

    public void OnInspectorUpdate()
    {
        this.Repaint();
    }

    void RecordImages()
    {
        if (lastFrameTime < Time.time + (1 / 24f))
        { // 24fps
            status = "Captured frame " + capturedFrame;
            Application.CaptureScreenshot(fileName + " " + capturedFrame + ".png");
            capturedFrame++;
            lastFrameTime = Time.time;
        }
    }
}
