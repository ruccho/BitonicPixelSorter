using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Ruccho.Utilities.Samples
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] private Vector3 speed;
        
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            transform.Rotate(Vector3.right, speed.x * Time.deltaTime);
            transform.Rotate(Vector3.up, speed.y * Time.deltaTime);
            transform.Rotate(Vector3.forward, speed.z * Time.deltaTime);
        }
    }
}