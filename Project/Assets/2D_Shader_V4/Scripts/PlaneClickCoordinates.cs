using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

namespace _2D_Shader_V4 {

[RequireComponent(typeof(MainSimulationRunner))]
public class PlaneClickCoordinates : MonoBehaviour
{
    private GameObject planeObject; // Assign the plane GameObject in the inspector
    [SerializeField] private bool debug = false;
    private MainSimulationRunner simRunner;

    void OnEnable(){
        simRunner = GetComponent<MainSimulationRunner>();
        planeObject = this.gameObject;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left mouse button click
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            // Check if the ray hits the plane
            if (Physics.Raycast(ray, out hit))
            {
                // Ensure the hit object is the plane
                if (hit.collider.gameObject == planeObject)
                {
                    Vector2 texturePoint = hit.textureCoord;

                    if(debug){
                        Debug.Log(texturePoint);
                    }

                    simRunner.Click(texturePoint);
                }
            }
        }
    }
}

}
