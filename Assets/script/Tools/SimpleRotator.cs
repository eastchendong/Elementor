using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Elementor
{
    public class SimpleRotator : MonoBehaviour
    {
        [SerializeField] private float rotationSpeed = 90f; // degrees per second
        [SerializeField] private Vector3 rotationAxis = Vector3.up; // Y-axis by default

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
        }
    }
}
