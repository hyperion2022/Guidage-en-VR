using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject cubeParent;
    [SerializeField] GameObject textParent;
    private int nbDangerousCubes = 3;
    private int nbCubes;
    [SerializeField] Camera cam;
    private Dictionary<GameObject, int> cubeIndices;
    private List<GameObject> cubeList;
    private int currentIndex, nbGoodCubes;

    // Start is called before the first frame update
    void Start()
    {
        currentIndex = 0;
        nbCubes = cubeParent.transform.childCount;

        if (nbCubes < nbDangerousCubes)
        {
            Debug.Log("Not enough total cubes!");
        }

        cubeIndices = new Dictionary<GameObject, int>();
        cubeList = new List<GameObject>();

        // Create list with all cubes and shuffle it
        for (int i = 0; i < nbCubes; i++)
        {
            cubeList.Add(cubeParent.transform.GetChild(i).gameObject);
        }
        Shuffle(cubeList);

        // Add Safe Cube indices
        nbGoodCubes = nbCubes - nbDangerousCubes;
        for (int i = 0; i < nbGoodCubes; i++)
        {
            GameObject currentCube = cubeList.ElementAt(i);
            cubeIndices.Add(currentCube, i);
            int initialIndex = int.Parse(currentCube.name.Substring(5));
            textParent.transform.GetChild(initialIndex).GetComponent<TextMeshProUGUI>().text = (i + 1).ToString();
        }
        // Add Dangerous Cubes indices
        for (int i = nbGoodCubes; i < nbGoodCubes + nbDangerousCubes; i++)
        {
            GameObject currentCube = cubeList.ElementAt(i);
            cubeIndices.Add(currentCube, -1);
            int initialIndex = int.Parse(currentCube.name.Substring(5));
            textParent.transform.GetChild(initialIndex).GetComponent<TextMeshProUGUI>().text = "Bomb";
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
                if (currentIndex == nbGoodCubes)
                {
                    Debug.Log("You win!");
                }
                else if (cubeIndices[hitObject] == -1 || cubeIndices[hitObject] != currentIndex)
                {
                    Debug.Log("You lose!");
                }
                else
                {
                    currentIndex++;
                }
                Debug.Log(cubeIndices[hitObject]);
            }
        }
    }

    // Function to shuffle a list using the Fisher-Yates shuffle algorithm
    // Source: chatGPT
    public static void Shuffle<T>(List<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = UnityEngine.Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
