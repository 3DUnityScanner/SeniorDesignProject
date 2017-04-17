
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
    Texture2D feedTex;
    bool updateGUI;
    Rect imgRect = new Rect(300, 300, 200, 200);
    ColorDepthImage cameraImage = null;
    bool isRecording = false;
    bool snapEnabled = false;
    int selectedCameraIndex = 0;
    string statusLabelText = "Idle";
    string buttonText = "Scan";
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

        //Draws the button that switches between capturing and scanning
        GUILayout.BeginArea(new Rect(0, 50, 100, 100));
        if (GUILayout.Button(buttonText, GUILayout.Width(100)))
        {
            //Click handler while recording
            if (isRecording)
            {
                statusLabelText = "Idle...";
                buttonText = "Scan";
            }

            //Click handler while not recording
            else
            {

                //Create an instance of the camera
                string cameraName = cameraOptions[selectedCameraIndex];
                Type cameraType = cameraNameLookup[cameraName];

                if (cameraType != null)
                {
                    camera = (ICamera)Activator.CreateInstance(cameraType);
                    camera.StartCapture();
                    cameraImage = camera.GetImage();
                    algorithm.ProcessImage(cameraImage);
                } else
                {
                    /*if(cameraName == "Cube")
                    {
                        algorithm.ProcessPLY("Assets/Resources/cube_testmesh.ply");
                    }
                    else if (cameraName == "Cube on Plane")
                    {
                        algorithm.ProcessPLY("Assets/Resources/cube-plane_testmesh.ply");
                    }*/
                }
                

                IEnumerable<Shape> poseList = algorithm.GetShapes();

                foreach(Shape p in poseList)
                {
                    GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.rotation = p.Rotation;
                    cube.transform.position = p.Translation;
                }

                algorithm.ClearShapes();

            }
        }
        GUILayout.EndArea();

        //Draws the status label
        EditorGUILayout.LabelField("Status: ", statusLabelText);

        //Creates the snap to toggle
        snapEnabled = GUILayout.Toggle(snapEnabled, "Snap to Objects");

        //Convoluted GUI drawing
        GUILayout.BeginArea(new Rect(0, 0, Screen.width, Screen.height));
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("RGB Feed");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (feedTex != null)
            GUILayout.Label(feedTex, GUILayout.MaxWidth(Screen.width - 130), GUILayout.MaxHeight(Screen.height - 150), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Depth Feed");
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (feedTex != null)
            GUILayout.Label(feedTex, GUILayout.MaxWidth(Screen.width - 130), GUILayout.MaxHeight(Screen.height - 150), GUILayout.MinWidth(50), GUILayout.MinHeight(50));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();
        GUILayout.EndArea();

        updateGUI = false;
    }

    //Load Necessary resources (point cloud, etc..)
    void Awake()
    {
        feedTex = new Texture2D(2, 2);
        feedTex = Resources.Load("scannerBlocks", (typeof(Texture2D))) as Texture2D;
        updateGUI = true;
    }

    private void Update()
    {
        if (isRecording && camera != null)
        {
            //cameraImage = camera.GetImage();
            //algorithm.ProcessImage(cameraImage);
        }

        if (updateGUI)
            EditorUtility.SetDirty(this);
    }
}
