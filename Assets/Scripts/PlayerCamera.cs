using UnityEngine;

namespace UserOnboarding
{
    public class PlayerCamera : MonoBehaviour
    {
        private Vector3 displacement;
        [SerializeField] GameObject Player;

        void Start()
        {
            displacement = Player.transform.position - transform.position;
        }

        void Update()
        {
            transform.position = Player.transform.position - displacement;
        }
    }
}
