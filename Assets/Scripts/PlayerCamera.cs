using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class PlayerCamera : MonoBehaviour
{
    private Vector3 displacement;
    private Quaternion delta;
    [SerializeField] GameObject Player;

    // Start is called before the first frame update
    void Start()
    {
        displacement = Player.transform.position - transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Player.transform.position - displacement;
    }
}
