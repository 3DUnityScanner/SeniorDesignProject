
using UnityEngine;
using UnityEditor;

class ScannerOptions : EditorWindow
{
    public float option1;

    public void toggleWindow()
    {
        //GetWindow(typeof(ScannerOptions)).position = new Rect(500, 500, 100, 100);
        GetWindow(typeof(ScannerOptions)).title = "Settings";
        GetWindow(typeof(ScannerOptions)).Show();
    }

    void OnGUI()
    {
        GUILayout.Label("General Settings", EditorStyles.boldLabel);
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();

        GUILayout.Label("Algorithm Settings (Color Tracking)", EditorStyles.boldLabel);
        GUILayout.BeginVertical();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Option1", GUILayout.Width(60));
        option1 = GUILayout.HorizontalSlider(option1, 0.0F, 10.0F);
        GUILayout.Box("" + option1, GUILayout.Width(70));
        GUILayout.EndHorizontal();


        GUILayout.EndVertical();

    }
}
