using UnityEngine;
using UnityEditor;
// using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

// Unityエディタ拡張ウィンドウのクラス
public class SimpleObjectPlacerWindow : EditorWindow
{
    // Interaction settings
    private bool addInteraction = true;
    private Color[] interactionColors = new Color[] { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta };
    private float scaleMultiplier = 1.5f;
    private float rotationSpeed = 90f;
    private AudioClip interactionSound;
    
    // Window instance
    private static SimpleObjectPlacerWindow window;
    
    // メニューにウィンドウを追加する属性
    [MenuItem("Tools/Interactive Cube Placer")]
    public static void ShowWindow()
    {
        window = GetWindow<SimpleObjectPlacerWindow>("Interactive Cube Placer");
        window.Show();
    }
    
    // 旧メニュー項目（互換性のため残す）
    [MenuItem("Tools/Place Cube At Origin")]
    public static void PlaceCubeAtOrigin()
    {
        PlaceCubeWithInteraction(Vector3.zero, true);
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Interactive Cube Placement", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();
        
        // Interaction settings
        addInteraction = EditorGUILayout.Toggle("Add Interaction", addInteraction);
        
        if (addInteraction)
        {
            EditorGUI.indentLevel++;
            
            EditorGUILayout.LabelField("Interaction Settings", EditorStyles.miniBoldLabel);
            
            // Scale settings
            scaleMultiplier = EditorGUILayout.FloatField("Scale Multiplier", scaleMultiplier);
            rotationSpeed = EditorGUILayout.FloatField("Rotation Speed", rotationSpeed);
            
            // Sound settings
            interactionSound = (AudioClip)EditorGUILayout.ObjectField("Interaction Sound", interactionSound, typeof(AudioClip), false);
            
            // Color array settings
            EditorGUILayout.LabelField("Interaction Colors:");
            EditorGUI.indentLevel++;
            
            int colorCount = EditorGUILayout.IntField("Color Count", interactionColors.Length);
            if (colorCount != interactionColors.Length)
            {
                System.Array.Resize(ref interactionColors, colorCount);
            }
            
            for (int i = 0; i < interactionColors.Length; i++)
            {
                interactionColors[i] = EditorGUILayout.ColorField($"Color {i + 1}", interactionColors[i]);
            }
            
            EditorGUI.indentLevel--;
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // Placement buttons
        if (GUILayout.Button("Place at Origin"))
        {
            PlaceCubeWithInteraction(Vector3.zero, addInteraction);
        }
        
        if (GUILayout.Button("Place at Scene View Position"))
        {
            if (SceneView.lastActiveSceneView != null)
            {
                Vector3 sceneViewPos = SceneView.lastActiveSceneView.pivot;
                PlaceCubeWithInteraction(sceneViewPos, addInteraction);
            }
            else
            {
                Debug.LogWarning("No active Scene View found. Please open a Scene View window.");
            }
        }
    }
    
    private static void PlaceCubeWithInteraction(Vector3 position, bool withInteraction)
    {
        // キューブを生成
        GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newObject.name = withInteraction ? "InteractiveCube" : "PlacedCube";
        newObject.transform.position = position;
        newObject.transform.rotation = Quaternion.identity;
        newObject.transform.localScale = Vector3.one;
        
        // Add interaction component if requested
        if (withInteraction)
        {
            // Load the CubeInteraction UdonSharpProgramAsset
            string[] guids = AssetDatabase.FindAssets("CubeInteraction t:UdonSharpProgramAsset");
            if (guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                UdonSharpProgramAsset programAsset = AssetDatabase.LoadAssetAtPath<UdonSharpProgramAsset>(path);
                
                if (programAsset != null)
                {
                    // Add UdonBehaviour component
                    UdonBehaviour udonBehaviour = newObject.AddComponent<UdonBehaviour>();
                    udonBehaviour.programSource = programAsset;
                    
                    // Set default values through serialized properties
                    SerializedObject serializedBehaviour = new SerializedObject(udonBehaviour);
                    
                    // Configure colors array
                    if (window != null)
                    {
                        SerializedProperty colorsProperty = serializedBehaviour.FindProperty("publicVariables.colors");
                        if (colorsProperty != null)
                        {
                            colorsProperty.arraySize = window.interactionColors.Length;
                            for (int i = 0; i < window.interactionColors.Length; i++)
                            {
                                colorsProperty.GetArrayElementAtIndex(i).colorValue = window.interactionColors[i];
                            }
                        }
                        
                        // Configure other properties
                        SerializedProperty scaleProperty = serializedBehaviour.FindProperty("publicVariables.scaleMultiplier");
                        if (scaleProperty != null) scaleProperty.floatValue = window.scaleMultiplier;
                        
                        SerializedProperty rotationProperty = serializedBehaviour.FindProperty("publicVariables.rotationSpeed");
                        if (rotationProperty != null) rotationProperty.floatValue = window.rotationSpeed;
                        
                        // Add audio source if sound is specified
                        if (window.interactionSound != null)
                        {
                            AudioSource audioSource = newObject.AddComponent<AudioSource>();
                            audioSource.clip = window.interactionSound;
                            audioSource.playOnAwake = false;
                            
                            SerializedProperty soundProperty = serializedBehaviour.FindProperty("publicVariables.interactionSound");
                            if (soundProperty != null) soundProperty.objectReferenceValue = audioSource;
                        }
                        
                        serializedBehaviour.ApplyModifiedProperties();
                    }
                }
                else
                {
                    Debug.LogWarning("CubeInteraction UdonSharpProgramAsset not found. Make sure the script has been compiled.");
                }
            }
            else
            {
                Debug.LogWarning("CubeInteraction asset not found. Make sure to compile UdonSharp scripts first.");
            }
            
            // Make sure it has a collider for interaction
            Collider collider = newObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = false; // Ensure it's not a trigger
            }
        }
        
        // Undo対応
        Undo.RegisterCreatedObjectUndo(newObject, withInteraction ? "Place Interactive Cube" : "Place Cube");
        Selection.activeGameObject = newObject;
        EditorUtility.SetDirty(newObject);
        
        Debug.Log($"{(withInteraction ? "Interactive " : "")}Cubeを配置しました: {position}");
    }
}
