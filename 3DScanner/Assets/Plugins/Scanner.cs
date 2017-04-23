
using UnityEngine;
using UnityEditor;
using UnityScanner3D.CameraIO;
using System.Collections.Generic;
using System;
using UnityScanner3D.ComputerVision;
using System.Diagnostics;

[InitializeOnLoad]
public class Scanner : EditorWindow
{
    private static bool justRecompiled = true;

    //Object used to access the camera
    ICamera camera;

    IAlgorithm algorithm = new ColorTrackingAlgorithm();

    Stopwatch lastCameraUpdate = new Stopwatch();
    const int CAPTURELIMIT = 100;

    //UI Backing Fields
    string cameraName;
    string logText = "", statusLabelText, streamText, captureText;
    Type cameraType;
    Texture2D leftStream, rightStream;
    bool updateGUI, isStreaming, snapEnabled, showContrast;
    ColorDepthImage cameraImage;
    Stack<GameObject> scans;
    int scanCount;
    int selectedCameraIndex = 1;
    private bool runningAlgorithm;
    private bool showCool;
    private string leftStreamLabel = "RGB Stream";
    private string rightStreamLabel = "Depth Stream";
    string[] cameraOptions = new string[]
    {
        "Dummy Camera",
        "Intel F200",
        //"Kinect",
    };

    //A lookup that maps camera names to classes
    Dictionary<string, Type> cameraNameLookup = new Dictionary<string, Type>
    {
        { "Dummy Camera", typeof(DummyCamera) },
        { "Intel F200", typeof(IntelCamera) },
        //{ "Kinect", null },
    };


    //Shows the plugin when the user clicks the Window > 3DScanner option
    [MenuItem("Window/3D Scanner")]
    public static void ShowWindow()
    {
        GetWindow(typeof(Scanner));
        GetWindow(typeof(Scanner)).title = "3D Scanner";
    }

    static Scanner()
    {
        //Clean it up
        justRecompiled = true;
    }

    void OnGUI()
    {
        GUILayout.BeginVertical();

        //Creates the camera selection drop down menu
        GUILayout.BeginHorizontal();
        selectedCameraIndex = EditorGUILayout.Popup("Choose a Camera", selectedCameraIndex, cameraOptions, EditorStyles.popup);
        GUILayout.EndHorizontal();

        //Buttons and status
        bool captureButton = false;
        GUILayout.BeginHorizontal();
        bool streamButton = GUILayout.Button(streamText, GUILayout.Width(200));
        GUILayout.Space(5);
        if (GUIKeyDown(KeyCode.Space))
            streamButton = true;
        if (!isStreaming)
            GUI.enabled = false;
        captureButton = GUILayout.Button(captureText, GUILayout.ExpandWidth(true));
        if (!isStreaming)
            GUI.enabled = true;
        GUILayout.Space(5);
        bool undoButton = GUILayout.Button("Undo Last", GUILayout.Width(100));
        bool clearButton = GUILayout.Button("Clear All", GUILayout.Width(100));
        GUILayout.EndHorizontal();


        /*
        //Options
        GUILayout.BeginHorizontal();
        snapEnabled = GUILayout.Toggle(snapEnabled, "Snap to Objects");
        snapEnabled = GUILayout.Toggle(snapEnabled, "Woah Look at");
        snapEnabled = GUILayout.Toggle(snapEnabled, "All These Options");
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
  */


        GUILayout.EndVertical();
      

        //We interrupt this gross UI stuff to bring you some logic
        if (clearButton){
            while (GameObject.FindWithTag("scanned_group") != null)
            { clearObjectsInScene(); }
            
        }

        if (streamButton)
        {

            //if streaming, stop it
            if (isStreaming)
            {
                isStreaming = false;
                statusLabelText = "Idle";
                streamText = "Start Stream";
                if (camera != null)
                    camera.StopCapture();

                /*
                Texture2D[] texs = FindObjectsOfType(typeof(Texture2D)) as Texture2D[];
                for(int i = 0;i < texs.Length; i++)
                {
                    DestroyImmediate(texs[i]);
                    texs[i] = null;
                }*/
                //AssetDatabase.Refresh();
                //Resources.UnloadUnusedAssets();
                //EditorUtility.UnloadUnusedAssetsImmediate();
                //Caching.CleanCache();
                //GC.Collect();
            }

            //if not streaming, start it
            else
            {
                isStreaming = true;
                statusLabelText = "Streaming!";
                streamText = "Stop Stream";

                //Create an instance of the camera
                cameraName = cameraOptions[selectedCameraIndex];
                cameraType = cameraNameLookup[cameraName];

                if (cameraType != null)
                {
                    camera = (ICamera)Activator.CreateInstance(cameraType);
                    camera.StartCapture();
                }
            }
        }

        if (captureButton && camera != null)
                runningAlgorithm = true;

        if (undoButton)
        {
            if (scans.Count > 0)
            {
                var lastScanned = scans.Pop();
                DestroyImmediate(lastScanned);
                scanCount--;
            }
            else
            {
                UnityEngine.Debug.Log("Must have scanned before undoing");
            }
        }

        //Update ColorDepthImage
        if (isStreaming)
            updateCam();

        drawStreams();
        algorithm.DrawSettings();

        updateGUI = false;
    }

    private void drawStreams()
    {
        GUILayout.BeginVertical();
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        if (GUILayout.Button(leftStreamLabel, GUILayout.Width(640 / 2)))
        {
            showCool = !showCool;
            if (showCool)
                leftStreamLabel = "Cool Stream";
            else
                leftStreamLabel = "RGB Stream";
        }
        if (leftStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.Width(640 / 2), GUILayout.Height(480 / 2));
        else
            GUILayout.Box(leftStream, GUILayout.Width(640 / 2), GUILayout.Height(480 / 2));
        GUILayout.EndVertical();

        GUILayout.BeginVertical();

        if (GUILayout.Button(rightStreamLabel, GUILayout.Width(640 / 2)))
        {
            showContrast = !showContrast;
            if (showContrast)
                rightStreamLabel = "Processed Stream";
            else
                rightStreamLabel = "Depth Stream";
        }
        if (leftStream == null || !isStreaming)
            GUILayout.Box("", GUILayout.Width(640 / 2), GUILayout.Height(480 / 2));
        else
            GUILayout.Box(rightStream, GUILayout.Width(640 / 2), GUILayout.Height(480 / 2));
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    //Reset all the goods
    public void OnRecompile()
    {
        //
        if (camera != null)
            camera.StopCapture();
        camera = null;
        algorithm = new ColorTrackingAlgorithm();
        lastCameraUpdate = new Stopwatch();
        logText = "";
        leftStream = null;
        rightStream = null;
        cameraImage = null;
        isStreaming = false;
        statusLabelText = "Idle";
        streamText = "Start Stream";
        captureText = "Place Objects";
        snapEnabled = true;
        scans = new Stack<GameObject>();
        scanCount = 0;
        justRecompiled = false;
        showContrast = false;
        rightStreamLabel = "Depth Stream";
        leftStreamLabel = "RGB Stream";
        updateGUI = true;
        selectedCameraIndex = 1;
    }

    private void updateCam()
    {
        if (camera != null)
        {
            if (lastCameraUpdate.ElapsedMilliseconds > CAPTURELIMIT || cameraImage == null)
            {
                cameraImage = camera.GetImage();

                leftStream = cameraImage.ColorImage;
                if (showContrast)
                    rightStream = algorithm.PreviewImage(cameraImage);
                else
                    rightStream = cameraImage.DepthImage;

                leftStream.Apply();
                rightStream.Apply();

                lastCameraUpdate.Reset();
                lastCameraUpdate.Start();
            }
            else if (!lastCameraUpdate.IsRunning)
                lastCameraUpdate.Start();
        }
    }

    bool GUIKeyDown(KeyCode key)
    {
        if (Event.current.type == EventType.KeyDown)
            return (Event.current.keyCode == key);
        return false;
    }   

    private void clearObjectsInScene()
    {
        GameObject thing = GameObject.FindWithTag("scanned_group");
        DestroyImmediate(thing);
        thing = null;
    }

    private void Update()
    {
        if (justRecompiled)
            OnRecompile();

        if (isStreaming && camera != null)
            updateGUI = true;

        if (updateGUI)
            EditorUtility.SetDirty(GetWindow(typeof(Scanner)));

        if (runningAlgorithm)
        {
            clearObjectsInScene();
            logText = "Algorithm: Color Tracking\n";

            scanCount++;
            updateCam();
            algorithm.ProcessImage(cameraImage);

            IEnumerable<GameObject> poseList = algorithm.GetShapes();

            Material blackMaterial = new Material(Shader.Find("Standard"));
            blackMaterial.color = Color.black;

            //Set parent for group objects as empty GameObject
            var parent = new GameObject() { name = "Scanned Objects " + scanCount };
            parent.tag = "scanned_group";
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.localScale = new Vector3(400, 1, 400);
            plane.transform.parent = parent.transform;
            plane.GetComponent<Renderer>().material = blackMaterial;
            plane.GetComponent<Renderer>().material.color = Color.black;
            Vector3 centerVector = new Vector3();
            bool vFlag = false;
            int i = 0;
            foreach (GameObject p in poseList)
            {
                Material redMaterial = new Material(Shader.Find("Standard"));
                blackMaterial.color = Color.red;
                Material blueMaterial = new Material(Shader.Find("Standard"));
                blackMaterial.color = Color.blue;
                i++;
                if (!vFlag)
                {
                    centerVector = new Vector3(p.transform.position.x,0.0f,p.transform.position.z);
                    vFlag = true;
                }

                //thing.transform.rotation = p.transform.rotation;
                p.transform.position -= centerVector;
                p.transform.parent = parent.transform;//grouping spawned objects

                if (p.name == "Cube")
                {
                    p.GetComponent<Renderer>().material = redMaterial;
                    p.GetComponent<Renderer>().material.color = Color.red;
                }
                else if (p.name == "Cylinder")
                {
                    p.GetComponent<Renderer>().material = blueMaterial;
                    p.GetComponent<Renderer>().material.color = Color.blue;
                    //p.transform.localScale = new Vector3(70, 35, 70);
                    //thing.transform.position -= new Vector3(0, 35f, 0);
                }


                logText += "Object " + i + " : ";
                logText += "Position: " + p.transform.position.ToString() + " - Rotation: " + p.transform.rotation + "\n";
            }

            logText += "Objects Found: " + i + "\n\n";

            scans.Push(parent);//pass to lastScanned for the undo button
            algorithm.ClearShapes();

            runningAlgorithm = false;
            isStreaming = true;

            SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0, 0));
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}

