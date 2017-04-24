
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
    const int CAPTURELIMIT = 200;

    //UI Backing Fields
    string cameraName;
    string streamText = "Start Stream";
    Type cameraType;
    Texture2D leftStream, rightStream;
    bool updateGUI, isStreaming, showContrast;
    ColorDepthImage cameraImage;
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
        captureButton = GUILayout.Button("Place Objects", GUILayout.ExpandWidth(true));
        if (!isStreaming)
            GUI.enabled = true;
        GUILayout.Space(5);
        bool undoButton = GUILayout.Button("Undo Last", GUILayout.Width(200));
        //bool clearButton = GUILayout.Button("Clear All", GUILayout.Width(100));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
      

        //We interrupt this gross UI stuff to bring you some logic
        if (undoButton){
            while (GameObject.FindWithTag("scanned_group") != null)
            { clearObjectsInScene(); }
            
        }

        if (streamButton)
        {

            //if streaming, stop it
            if (isStreaming)
            {
                isStreaming = false;
                streamText = "Start Stream";
                if (camera != null)
                    camera.StopCapture();
            }

            //if not streaming, start it
            else
            {
                isStreaming = true;
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
        leftStream = null;
        rightStream = null;
        cameraImage = null;
        isStreaming = false;
        streamText = "Start Stream";
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

    Bounds getRenderBounds(GameObject objeto)
    {
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
        MeshRenderer render = objeto.GetComponent<MeshRenderer>();
        if (render != null)
            return render.bounds;
        return bounds;
    }

    Bounds getBounds(GameObject objeto)
    {
        Bounds bounds = getRenderBounds(objeto);
        Bounds b = new Bounds(Vector3.zero, Vector3.zero);

        foreach (var r in objeto.GetComponentsInChildren<MeshRenderer>())
        {
            if (bounds == b)
            {
                bounds = r.bounds;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }

        }

        return bounds;
    }

    public static void newObjMat(GameObject input)
    {
        Material mat = input.GetComponent<MeshRenderer>().sharedMaterial;
        input.GetComponent<MeshRenderer>().sharedMaterial = new Material(mat);
    }

    public static void newObjMesh(GameObject input)
    {
        Mesh mat = input.GetComponent<MeshFilter>().sharedMesh;
        //input.GetComponent<MeshFilter>().sharedMesh = new Mesh(mat);
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

            scanCount++;
            updateCam();
            algorithm.ProcessImage(camera, cameraImage);

            IEnumerable<GameObject> poseList = algorithm.GetShapes();

            Material blackMaterial = new Material(Shader.Find("Standard"));
            blackMaterial.color = Color.black;

            //Set parent for group objects as empty GameObject
            var parent = new GameObject() { name = "Scanned Objects"};
            parent.tag = "scanned_group";
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.transform.localScale = new Vector3(400, 1, 400);
            plane.transform.parent = parent.transform;

            newObjMat(plane);
            plane.GetComponent<Renderer>().sharedMaterial = blackMaterial;

            int i = 0;
            foreach (GameObject p in poseList)
            {
                Material redMaterial = new Material(Shader.Find("Standard"));
                redMaterial.color = Color.red;
                Material blueMaterial = new Material(Shader.Find("Standard"));
                blueMaterial.color = Color.blue;
                i++;

                p.transform.parent = parent.transform;//grouping spawned objects

                if (p.name == "Cube")
                {
                    newObjMat(p);
                    p.GetComponent<Renderer>().sharedMaterial = redMaterial;
                    //p.AddComponent<MeshCollider>();
                }
                    
                else if (p.name == "Cylinder")
                {
                    newObjMat(p);
                    p.GetComponent<Renderer>().sharedMaterial = blueMaterial;
                    //p.AddComponent<MeshCollider>(); 
                }

                

            }


            algorithm.ClearShapes();

            runningAlgorithm = false;
            isStreaming = true;

            SceneView.lastActiveSceneView.LookAt(Vector3.zero);
            SceneView.lastActiveSceneView.Repaint();
        }
    }
}

