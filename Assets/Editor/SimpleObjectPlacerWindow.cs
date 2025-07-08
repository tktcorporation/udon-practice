using UnityEngine;
using UnityEditor;

// Unityエディタ拡張ウィンドウのクラス
public class SimpleObjectPlacerWindow : EditorWindow
{
    // メニューにウィンドウを追加する属性
    [MenuItem("Tools/Place Cube At Origin")]
    public static void PlaceCubeAtOrigin()
    {
        // キューブを生成
        GameObject newObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        newObject.name = "PlacedCube";
        newObject.transform.position = Vector3.zero;
        newObject.transform.rotation = Quaternion.identity;
        newObject.transform.localScale = Vector3.one;

        // Undo対応
        Undo.RegisterCreatedObjectUndo(newObject, "Place Cube At Origin");
        Selection.activeGameObject = newObject;
        EditorUtility.SetDirty(newObject);
        Debug.Log("Cubeを原点に配置しました");
    }
}
