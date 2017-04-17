
using UnityEngine;
using UnityEditor;
using System.Collections;
using UnityEngine.Windows;
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
    Rect imgRect = new Rect(300, 300, 200, 200);
    ColorDepthImage cameraImage = null;
    bool isRecording = false;
    bool snapEnabled = false;
    bool showPosition = true;
    int selectedCameraIndex = 0;
    string statusLabelText = "Idle";
    string buttonText = "Scan";
    string stat = "Advanced Options";
    string[] cameraOptions = new string[]
    {
        "Dummy Camera",
        "Intel Camera",
        "Kinect",
        "Cube",
        "Cube on Plane"
    };

    //A lookup that maps camera names to classes
    Dictionary<string, Type> cameraNameLookup = new Dictionary<string, Type>
    {
        { "Dummy Camera", typeof(DummyCamera) },
        { "Intel Camera", typeof(IntelCamera) },
        {"Kinect", null },
        {"Cube", null },
        {"Cube on Plane", null }
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
        GUILayout.BeginArea(new Rect((Screen.width / 2) - 50, 50, 100, 100));
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

        //Draws the label for the camera feed
        GUILayout.BeginArea(new Rect(195, 195, 100, 100));
            EditorGUILayout.LabelField("Camera Feed:");
        GUILayout.EndArea();

        //Draws the image from the camera
        if (cameraImage != null && isRecording)
        {
            EditorGUI.DrawPreviewTexture(imgRect, cameraImage.ColorImage);
        }
    }

    private void Update()
    {
        if (isRecording && camera != null)
        {
            //cameraImage = camera.GetImage();
            //algorithm.ProcessImage(cameraImage);
        }
    }
}
