using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject cubeParent;
    private int nbDangerousCubes = 3;
    private int nbCubes;
    [SerializeField] Camera cam;
    private Dictionary<GameObject, int> cubeIndices;
    private List<GameObject> cubeList;

    // Start is called before the first frame update
    void Start()
    {
        nbCubes = cubeParent.transform.childCount;

        if (nbCubes < nbDangerousCubes)
        {
            Debug.Log("Not enough total cubes!");
        }

        cubeIndices = new Dictionary<GameObject, int>();
        cubeList = new List<GameObject>();

        for (int i = 0; i < nbCubes; i++)
        {
            cubeList.Add(cubeParent.transform.GetChild(i).gameObject);
        }

        // shuffle elements
        cubeList.OrderBy(x => UnityEngine.Random.value).ToList();

        // Add Safe cubes in order
        int nbGoodCubes = nbCubes - nbDangerousCubes;
        for (int i = 0; i < nbGoodCubes; i++)
        {
            cubeIndices.Add(cubeList.ElementAt(i), i);
        }
        for (int i = nbGoodCubes; i < nbGoodCubes + nbDangerousCubes; i++)
        {
            cubeIndices.Add(cubeList.ElementAt(i), -1);
        }
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

            // If we hit an object, deactivate it
            if (hitObject != null && hitObject.tag == "Cube")
            {
                hitObject.SetActive(false);
                Debug.Log(cubeIndices[hitObject]);
            }
        }
    }
}
