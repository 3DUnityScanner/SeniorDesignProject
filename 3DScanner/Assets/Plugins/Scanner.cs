
using UnityEngine;
using UnityEditor;
using UnityScanner3D.CameraIO;
using System.Collections.Generic;
using System;
using UnityScanner3D.ComputerVision;

public class Scanner : EditorWindow
{
    //Object used to access the camera
    ICamera camera;
    IAlgorithm algorithm = new ColorTrackingAlgorithm();

    //UI Backing Fields
    string cameraName;
    Type cameraType;
    Texture2D feedTex, colorStream, depthStream;
    bool updateGUI;
    Rect imgRect = new Rect(300, 300, 200, 200);
    ColorDepthImage cameraImage = null;
    bool isStreaming = false;
    bool snapEnabled = false;
    int selectedCameraIndex = 0;
    string statusLabelText = "Idle";
    string streamText = "Start Stream";
    string captureText = "Capture";
    string[] cameraOptions = new string[]
    {
        "Dummy Camera",
        "Intel F200",
        "Kinect",
        "Cube [TEST]",
        "Cube on Plane [TEST]"
    };

    //A lookup that maps camera names to classes
    Dictionary<string, Type> cameraNameLookup = new Dictionary<string, Type>
    {
        { "Dummy Camera", typeof(DummyCamera) },
        { "Intel F200", typeof(IntelCamera) },
        { "Kinect", null },
        { "Cube [TEST]", null },
        { "Cube on Plane [TEST]", null }
    };

    //Shows the plugin when the user clicks the Window > 3DScanner option
    [MenuItem("Window/3D Scanner")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Scanner));
    }

    void OnGUI()
    {
        //Creates the camera selection drop down menu
        selectedCameraIndex = EditorGUILayout.Popup("Choose a Camera", selectedCameraIndex, cameraOptions);

        GUILayout.BeginVertical();

        //Draws the button that switches between capturing and scanning
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(streamText, GUILayout.Width(100)))
        {
            //if streaming, stop it
            if (isStreaming)
            {
                isStreaming = false;
                statusLabelText = "Idle";
                streamText = "Start Stream";
                if (camera != null)
                    camera.StopCapture();
            }

            //if not streaming, start it
            else
            {
                isStreaming = true;
                statusLabelText = "Streaming!";
                streamText = "Stop Stream";

                //Create an instance of the camera
                string cameraName = cameraOptions[selectedCameraIndex];
                Type cameraType = cameraNameLookup[cameraName];

                if (cameraType != null)
                {
                    camera = (ICamera)Activator.CreateInstance(cameraType);
                    camera.StartCapture();
                }
            }
        }

        if (GUILayout.Button(captureText, GUILayout.Width(100)))
        {
            if (cameraType != null)
            {
                cameraImage = camera.GetImage();
                algorithm.ProcessImage(cameraImage);

                IEnumerable<Shape> poseList = algorithm.GetShapes();

                foreach (Shape p in poseList)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.rotation = p.Rotation;
                    cube.transform.position = p.Translation;
                }

                algorithm.ClearShapes();
            }
        }

        //Creates the snap to toggle
        snapEnabled = GUILayout.Toggle(snapEnabled, "Snap to Objects");

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();

        //Draws the status label
        GUILayout.Label("Status: " + statusLabelText);

        GUILayout.EndHorizontal();

        if(camera != null && isStreaming)
        {
            ColorDepthImage camStream = camera.GetImage();

            colorStream = camStream.ColorImage;
            depthStream = camStream.DepthImage;

            colorStream.Apply();
            depthStream.Apply();
        }

        //Convoluted GUI drawing
        GUILayout.BeginArea(new Rect(0, -40, Screen.width, Screen.height));
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("RGB Feed");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (colorStream == null || !isStreaming)
            GUILayout.Box("COLOR STREAM", GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        else
            GUILayout.Box(colorStream, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label("Depth Feed");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (depthStream == null || !isStreaming)
            GUILayout.Box("DEPTH STREAM", GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        else
            GUILayout.Box(depthStream, GUILayout.MaxWidth(300), GUILayout.MaxHeight(300), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();

        GUILayout.EndVertical();

        updateGUI = false;
    }

    //Load Necessary resources (point cloud, etc..)
    void Awake()
    {
        //feedTex = new Texture2D(2, 2);
        //feedTex = Resources.Load("scannerBlocks", (typeof(Texture2D))) as Texture2D;
        updateGUI = true;
    }

    private void Update()
    {
        if (isStreaming && camera != null)
        {
            updateGUI = true;
        }

        if (updateGUI)
        {
            EditorUtility.SetDirty(GetWindow(typeof(Scanner)));
        }
    }
}
