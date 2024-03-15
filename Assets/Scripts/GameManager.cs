using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject cubeParent;
    private int nbDangerousCubes = 3;
    private int nbCubes;
    [SerializeField] Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        nbCubes = cubeParent.transform.childCount;

        if (nbCubes < nbDangerousCubes )
        {
            Debug.Log("Not enough total cubes!");
        }

        // Choose id's of dangerous cubes


        // Choose order of the rest
        int nbGoodCubes = nbCubes - nbDangerousCubes;

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DrawRayFromMousePosition();
        }
    }

    void DrawRayFromMousePosition()
    {
        RaycastHit hit;
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        // Launch ray
        if (Physics.Raycast(ray, out hit))
        {
            GameObject hitObject = hit.transform.gameObject;

            // If we hit an object
            if (hitObject != null)
            {
                hitObject.SetActive(false);
            }
        }
    }

}
