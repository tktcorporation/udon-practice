using UnityEngine;
using UnityEditor;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

// Unityエディタ拡張ウィンドウのクラス
public class SimpleObjectPlacerWindow : EditorWindow
{

    // Window instance
    private static SimpleObjectPlacerWindow window;

    // Interaction settings
    private bool addInteraction = true;
    private Color[] interactionColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };
    private float scaleMultiplier = 1.5f;
    private float rotationSpeed = 90f;
    private AudioClip interactionSound;

    /// <summary>
    /// Creates and shows the SimpleObjectPlacerWindow.
    /// * This method is called when the user selects the menu item.
    /// </summary>
    [MenuItem("Tools/Interactive Cube Placer")]
    public static void ShowWindow()
    {
        window = GetWindow<SimpleObjectPlacerWindow>("Interactive Cube Placer");
        window.Show();
    }
}