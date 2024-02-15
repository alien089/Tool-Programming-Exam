using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class LevelEditor : EditorWindow
{
    [MenuItem("Tools/LevelEditor")]
    public static void ShowWindow() => GetWindow<LevelEditor>("Level Editor");
    private GameObject m_WorkPlane;
    private GameObject[] prefabs;
    [SerializeField] bool[] selectedPrefabs;
    private void OnEnable()
    {
        m_WorkPlane = CreatePlane(new Vector3(0,1,0));
        SceneView.duringSceneGui += DuringSceneGUI;

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        
        if (selectedPrefabs == null || selectedPrefabs.Length != prefabs.Length)
        {
            selectedPrefabs = new bool[prefabs.Length];
        }

    }

    private void OnDisable()
    {
        DestroyImmediate(m_WorkPlane);
        SceneView.duringSceneGui -= DuringSceneGUI;

    }

    private void DuringSceneGUI(SceneView sceneView)
    {
        DrawGUI(); 

        Transform cam = sceneView.camera.transform;

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }

        bool isHoldingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;

        if (Event.current.type == EventType.ScrollWheel && isHoldingAlt == true)
        {
            float scrollDirection = Mathf.Sign(Event.current.delta.y);

            float y = m_WorkPlane.transform.position.y;
            y *= 1f - scrollDirection * 0.05f;

            if (y <= 0.1)
                y = 1;

            Vector3 pos = new Vector3(m_WorkPlane.transform.position.x, y, m_WorkPlane.transform.position.z);
            m_WorkPlane.transform.position = pos;
            Repaint();

            Event.current.Use();
        }

        if(TryRaycastFromCamera(cam.up, out Vector3 mouseWorldPosition, out Matrix4x4 tangentToWorldMatrix))
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawBrush(tangentToWorldMatrix);
            }
        }
    }

    void DrawBrush(Matrix4x4 tangentToWorld)
    {
        const int circleDetail = 128;
        Vector3[] points = new Vector3[circleDetail];

        Handles.DrawAAPolyLine(points);

        //Handles.DrawWireDisc(hit.point, hit.normal, radius);

        Vector3 hitPos = tangentToWorld.GetPosition();

        Handles.color = Color.red;
        Handles.DrawAAPolyLine(6, hitPos, hitPos + (Vector3)tangentToWorld.GetRow(0));
        Handles.color = Color.green;
        Handles.DrawAAPolyLine(6, hitPos, hitPos + (Vector3)tangentToWorld.GetRow(2));
        Handles.color = Color.blue;
        Handles.DrawAAPolyLine(6, hitPos, hitPos + (Vector3)tangentToWorld.GetRow(1));

        Handles.color = Color.white;

    }

    bool TryRaycastFromCamera(Vector2 cameraUp, out Vector3 mouseWorldPosition, out Matrix4x4 tangentToWorldMatrix)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            mouseWorldPosition = hit.point;
            Vector3 hitNormal = hit.normal;

            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            tangentToWorldMatrix =
                Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitBitangent, hitNormal), Vector3.one);
            return true;
        }

        mouseWorldPosition = default;
        tangentToWorldMatrix = default;
        return false;
    }

    void DrawGUI()
    {
        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 50, 50);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject prefab = prefabs[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();
            selectedPrefabs[i] = GUI.Toggle(rect, selectedPrefabs[i], new GUIContent(icon));

            if (EditorGUI.EndChangeCheck())
            {

            }

            rect.y += rect.height + 2;
        }

        Handles.EndGUI();
    }

    private GameObject CreatePlane(Vector3 pos)
    {
        Color planeColor = new Color(1f, 1f, 1f, 0f); // Imposta il colore del piano trasparente
        Vector3 planeScale = new Vector3(1f, 0.1f, 1f); // Imposta le dimensioni del piano
        // Creazione del piano
        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Material transparentMaterial = new Material(Shader.Find("Transparent/Diffuse"));
        transparentMaterial.color = planeColor;
        plane.GetComponent<Renderer>().sharedMaterial = transparentMaterial; // Imposta il colore del piano
        plane.transform.localScale = planeScale; // Imposta le dimensioni del piano

        // Imposta il piano come tangibile
        MeshCollider collider = plane.AddComponent<MeshCollider>();
        collider.convex = true;
        collider.isTrigger = false;

        //plane.hideFlags = HideFlags.HideInHierarchy;

        plane.transform.position = pos;
        return plane;
    }
}
