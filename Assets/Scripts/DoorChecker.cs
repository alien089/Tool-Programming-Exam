using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class DoorChecker : MonoBehaviour
{
    void Update()
    {
        // Ottieni la posizione e la direzione del raycast
        Ray ray = new Ray(transform.position, transform.forward);

        // Esegui il raycast
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 3f))
        {
            if (hit.collider.gameObject.tag == "Door")
            {
                gameObject.transform.parent.transform.position = hit.collider.transform.position;
            }
        }
    }
}
