using UnityEngine;
using UnityEngine.UI;

namespace ScriptBoy.ProceduralBook
{
    [AddComponentMenu(" Script Boy/Procedural Book/  Demo/Player")]
    public class Player : MonoBehaviour
    {
        // Movement speed for the player
        [SerializeField] float m_MovementSpeed = 2;

        // Rotation speed for the player
        [SerializeField] float m_RotationSpeed = 2;

        // Zoom speed for the camera
        [SerializeField] float m_ZoomSpeed = 2;

        // UI Image for the pointer
        [SerializeField] Image m_Pointer;

        // The player's rotation angles
        Vector3 m_EulerAngles;

        // Field of view for the camera
        float m_FieldOfView;
        const float kMinFieldOfView = 15;
        const float kMaxFieldOfView = 60;

        // Currently selected book
        Book m_SelectedBook;

        void Start()
        {
            // Lock and hide the cursor at the center of the screen
            if (m_RotationSpeed != 0)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = true;
            }

            // Store the initial field of view of the camera
            m_FieldOfView = Camera.main.fieldOfView;
        }

        void Update()
        {
            // Handle player movement, rotation, zooming, and book page turning
            DoMovement();
            DoRotation();
            DoZoom();
            DoTurning();
        }

        void DoTurning()
        {
            // Create a ray from the camera based on mouse position
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // If a book is selected and it's currently turning
            if (m_SelectedBook != null && m_SelectedBook.isTurning)
            {
                // If the left mouse button is held down, update the turning of the book
                if (Input.GetMouseButton(0))
                {
                    m_SelectedBook.UpdateTurning(ray);
                }
                // If the left mouse button is released, stop turning
                else
                {
                    m_SelectedBook.StopTurning();
                }
            }
            else
            {
                // If the left mouse button is pressed and no book is selected
                if (Input.GetMouseButtonDown(0))
                {
                    // Check each book to see if it should start turning
                    foreach (var book in Book.instances)
                    {
                        if (book.StartTurning(ray))
                        {
                            m_SelectedBook = book; // Set the selected book
                            break; // Exit the loop once a book is selected
                        }
                    }
                }
            }

            if (m_Pointer != null)
            {
                if (Input.GetMouseButton(0))
                {
                    m_Pointer.color = m_SelectedBook != null && m_SelectedBook.isTurning ? Color.green : Color.red;
                }
                else
                {
                    m_Pointer.color = Color.white;
                }
            }
        }

        void DoMovement()
        {
            // Get input for horizontal and vertical movement
            float x = Input.GetAxis("Horizontal");
            float y = Input.GetAxis("Vertical");

            // Create a movement vector based on input and speed
            Vector3 move = new Vector3(x, 0, y);
            move *= m_MovementSpeed * Time.deltaTime;

            // Rotate the movement vector according to the player's current rotation
            move = Quaternion.Euler(0, transform.eulerAngles.y, 0) * move;

            // Update the player's position
            transform.position += move;
        }

        void DoRotation()
        {
            if (m_RotationSpeed == 0) return;

            // Get mouse movement input for rotation
            float x = Input.GetAxis("Mouse X");
            float y = Input.GetAxis("Mouse Y");

            // Create a rotation vector based on mouse movement
            Vector3 rotate = new Vector3(-y, x, 0);
            rotate *= m_RotationSpeed * Mathf.Lerp(0.5f, 1, Mathf.InverseLerp(kMinFieldOfView, kMaxFieldOfView, m_FieldOfView));

            // Update the player's rotation angles and clamp the vertical rotation
            m_EulerAngles += rotate;
            m_EulerAngles.x = Mathf.Clamp(m_EulerAngles.x, -85, 85);

            // Apply the rotation to the player's transform
            transform.eulerAngles = m_EulerAngles;
        }

        void DoZoom()
        {
            // Get the amount of scroll input for zooming
            float m = Input.GetAxis("Mouse ScrollWheel");
            m *= m_ZoomSpeed;

            // Adjust the field of view based on input
            m_FieldOfView -= m;
            m_FieldOfView = Mathf.Clamp(m_FieldOfView, kMinFieldOfView, kMaxFieldOfView); // Clamping the field of view

            // Apply the new field of view to the camera
            Camera.main.fieldOfView = m_FieldOfView;
        }
    }
}