
using UnityEngine;
using UnityEditor;
using System.Collections;

public class Scanner : EditorWindow
{
    string status = "Idle";
    string button = "Scan";
    bool groupEnabled;
    bool scanning = false;

    [MenuItem("Window/3D Scanner")]

    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(Scanner));
    }

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect((Screen.width / 2) - 100, (Screen.height / 2) - 150, 250, 200));
        GUILayout.FlexibleSpace();
        GUILayout.BeginHorizontal(EditorStyles.helpBox);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button(button, GUILayout.Width(250), GUILayout.Height(20)))
        {
            if(!scanning)
            {
                status = "Scanning";
                button = "Stop Scanning";
                scanning = true;
            }
            else
            {
                status = "Idle";
                button = "Scan";
                scanning = false;
            }
        }

        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect((Screen.width / 2) - 50, (Screen.height / 2) - 200, 200, 200));
        EditorGUILayout.LabelField("Status: " + status);
        GUILayout.EndArea();

    }

    private void Update()
    {
        if(scanning)
        {
            while(status != "Scanning...")
            {
                status = status + ".";
            }
            status = "Scanning";
        }
    }
}
