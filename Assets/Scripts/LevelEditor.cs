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
    private GameObject[] m_PrefabsList;
    [SerializeField] private bool[] m_SelectablesPrefabs;
    private int m_SelectedPref;
    private Vector3 m_MouseWorldPosition;
    private bool m_SnapModeEnabled = false;
    private GameObject m_SnappableObj;
    private int m_LayerIgnoreRaycast;
    private int m_PrevLayer;
        
    private void OnEnable()
    {
        m_WorkPlane = CreatePlane(new Vector3(0,1,0));
        SceneView.duringSceneGui += DuringSceneGUI;
        m_LayerIgnoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/Rooms" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        m_PrefabsList = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
        
        if (m_SelectablesPrefabs == null || m_SelectablesPrefabs.Length != m_PrefabsList.Length)
        {
            m_SelectablesPrefabs = new bool[m_PrefabsList.Length];
            m_SelectedPref = -1;
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

        
        

        AdjustWorkPlane();

        if (TryRaycastFromCamera(cam.up, out Matrix4x4 tangentToWorldMatrix))
        {
            if (Event.current.type == EventType.Repaint)
            {
                DrawBrush(tangentToWorldMatrix);

                if (m_SnapModeEnabled == false)
                    DrawPrefabPreviews();
                else
                    MoveSnapObj();
            }
        }

        SnapManagment();

        if (m_SnapModeEnabled == false && Event.current.keyCode == KeyCode.E && Event.current.type == EventType.KeyDown)
        {
            TrySpawnObject();
        }
    }

    private void AdjustWorkPlane()
    {
        bool isHoldingAlt = (Event.current.modifiers & EventModifiers.Alt) != 0;
        if (Event.current.type == EventType.ScrollWheel && isHoldingAlt)
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
    }

    private void SnapManagment()
    {
        bool isHoldingCtrl = (Event.current.modifiers & EventModifiers.Control) != 0;
        if (Event.current.keyCode == KeyCode.Alpha1 && Event.current.type == EventType.KeyDown && isHoldingCtrl)
        {
            m_SnapModeEnabled = false;
            DestroyImmediate(m_SnappableObj);
            m_SnappableObj = null;
        }

        if (m_SnapModeEnabled == false && Event.current.keyCode == KeyCode.Q && Event.current.type == EventType.KeyDown)
        {
            m_SnapModeEnabled = true;
            TrySpawnObject();
        }

        if (m_SnapModeEnabled == true && Event.current.keyCode == KeyCode.W && Event.current.type == EventType.KeyDown)
        {
            m_SnapModeEnabled = false;
            for (int i = 0; i < m_SnappableObj.transform.childCount; i++)
            {
                m_SnappableObj.transform.GetChild(i).gameObject.layer = m_PrevLayer;
            }
            m_SnappableObj = null;
        }
    }

    private void DrawPrefabPreviews()
    {
        float y = m_WorkPlane.transform.position.y + m_WorkPlane.transform.localScale.y/2;
        if (m_SelectedPref == -1 || m_MouseWorldPosition.y != y) return;

        Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(m_MouseWorldPosition, Quaternion.identity, Vector3.one);
        MeshFilter[] filters = m_PrefabsList[m_SelectedPref].GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter filter in filters)
        {
            Matrix4x4 childToPose = filter.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMtx = poseToWorldMtx * childToPose;

            Mesh mesh = filter.sharedMesh;
            Material mat = filter.GetComponent<MeshRenderer>().sharedMaterial;
            mat.SetPass(0);
            Graphics.DrawMeshNow(mesh, childToWorldMtx);
        }
    }

    private void MoveSnapObj() 
    {
        float y = m_WorkPlane.transform.position.y + m_WorkPlane.transform.localScale.y / 2;
        if (m_SelectedPref == -1 || m_MouseWorldPosition.y != y) return;

        m_SnappableObj.transform.position = m_MouseWorldPosition;
    }

    private void TrySpawnObject()
    {
        if (m_SelectedPref == -1) return;

        GameObject thingToSpawn = (GameObject)PrefabUtility.InstantiatePrefab(m_PrefabsList[m_SelectedPref]);
        Undo.RegisterCreatedObjectUndo(thingToSpawn, "Object Spawn");
        thingToSpawn.transform.SetPositionAndRotation(m_MouseWorldPosition, Quaternion.identity);

        if (m_SnapModeEnabled)
        {
            m_SnappableObj = thingToSpawn;
            m_PrevLayer = m_SnappableObj.layer;
            for(int i = 0; i < m_SnappableObj.transform.childCount; i++)
            {
                m_SnappableObj.transform.GetChild(i).gameObject.layer = m_LayerIgnoreRaycast;
            }
        }
    }

    private void DrawBrush(Matrix4x4 tangentToWorld)
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

    private bool TryRaycastFromCamera(Vector2 cameraUp, out Matrix4x4 tangentToWorldMatrix)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        int layerMask = ~(1 << m_LayerIgnoreRaycast); 
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
        {
            m_MouseWorldPosition = hit.point;
            Vector3 hitNormal = hit.normal;

            Vector3 hitTangent = Vector3.Cross(hitNormal, cameraUp).normalized;
            Vector3 hitBitangent = Vector3.Cross(hitNormal, hitTangent);

            tangentToWorldMatrix =
                Matrix4x4.TRS(hit.point, Quaternion.LookRotation(hitBitangent, hitNormal), Vector3.one);
            return true;
        }

        m_MouseWorldPosition = default;
        tangentToWorldMatrix = default;
        return false;
    }

    private void DrawGUI()
    {
        Handles.BeginGUI();

        Rect rect = new Rect(8, 8, 50, 50);

        for (int i = 0; i < m_PrefabsList.Length; i++)
        {
            GameObject prefab = m_PrefabsList[i];
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            EditorGUI.BeginChangeCheck();

            m_SelectablesPrefabs[i] = GUI.Toggle(rect, m_SelectablesPrefabs[i], new GUIContent(icon));

            if(m_SelectablesPrefabs[i] == true)
                m_SelectedPref = i;

            for(int j = 0; j < m_PrefabsList.Length; j++)
            {
                if (j != m_SelectedPref)
                    m_SelectablesPrefabs[j] = false;
            }

            if (m_SelectedPref != -1 && m_SelectablesPrefabs[m_SelectedPref] == false)
                m_SelectedPref = -1;
            

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
