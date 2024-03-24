using UnityEngine;

namespace UserOnboarding
{
    // Source 1: https://discussions.unity.com/t/how-to-make-a-3d-character-move/224761
    // Source 2: chatGPT
    public class PlayerController : MonoBehaviour
    {
        private bool leftPressed, rightPressed, upPressed, downPressed;
        private bool leftRotation, rightRotation;
        [SerializeField] float speed = 5.0f;
        [SerializeField] float smooth = 5.0f;

        void Update()
        {
            float horizontal, vertical, rotation;
            horizontal = vertical = rotation = 0;

            // If keys are pressed
            leftPressed = Input.GetKey(KeyCode.S);
            rightPressed = Input.GetKey(KeyCode.W);
            upPressed = Input.GetKey(KeyCode.A);
            downPressed = Input.GetKey(KeyCode.D);
            leftRotation = Input.GetKey(KeyCode.LeftArrow);
            rightRotation = Input.GetKey(KeyCode.RightArrow);

            // Choose the direction
            if (leftPressed) horizontal -= 1;
            if (rightPressed) horizontal += 1;
            if (downPressed) vertical -= 1;
            if (upPressed) vertical += 1;
            if (leftRotation) rotation -= 1;
            if (rightRotation) rotation += 1;

            // And change the position and rotation accordingly
            /*transform.position += new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime;
            Quaternion targetRotation = Quaternion.Euler(0, rotation * smooth * Time.deltaTime, 0);
            transform.rotation *= targetRotation;*/

            // Calculate movement direction relative to player's rotation
            Vector3 movement = transform.forward * vertical * speed * Time.deltaTime;
            Vector3 strafeMovement = transform.right * horizontal * speed * Time.deltaTime;

            // Move the player
            transform.position += movement + strafeMovement;

            // Apply rotation
            Quaternion deltaRotation = Quaternion.Euler(0, rotation * smooth * Time.deltaTime, 0);
            transform.rotation *= deltaRotation;
        }
    }
}
