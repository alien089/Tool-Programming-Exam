using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor;
using UnityEngine;
using UnityEditor.PackageManager;

[ExecuteInEditMode]
public class DoorChecker : MonoBehaviour
{
    private bool m_Lock = false;
    private Vector3 m_LockPosition;
    void Update()
    {
        if (m_Lock && !Application.isPlaying)
        {
            gameObject.transform.parent.position = m_LockPosition;
            EditorUtility.SetDirty(transform.parent);
        }
        else
        {
            RaycastHit hit;
            if(Physics.Raycast(transform.position, Vector3.forward, out hit, 1f))
            {
                if(hit.collider.gameObject.tag == "Door")
                {
                    m_Lock = true;
                    m_LockPosition = gameObject.transform.parent.position;
                }
            }
        }
    }
}
