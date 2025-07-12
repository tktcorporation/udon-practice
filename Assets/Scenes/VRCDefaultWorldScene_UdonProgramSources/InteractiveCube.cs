using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class InteractiveCube : UdonSharpBehaviour
{
    void Start()
    {

    }

    public override void Interact()
    {
        // Handle interaction logic here
        Debug.Log("Cube interacted with!");

        // Example interaction logic
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = Color.red; // Change color on interaction
        }
        else
        {
            Debug.LogWarning("Renderer component not found on InteractiveCube");
        }
    }
}
