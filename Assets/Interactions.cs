using System;
using System.Collections.Generic;
using UnityEngine;

public class Interactions : MonoBehaviour
{
    private float range = 1f;
    public Color interactionColor = Color.blue;

    private Dictionary<int, Color> colors;
    private List<GameObject> interactions;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        colors = new Dictionary<int, Color>();
        interactions = new List<GameObject>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !interactions.Contains(null))
        {
            GameObject obj = interactions[0];
            if (obj.tag == "Opened")
            {
                obj.GetComponent<Animator>().SetBool("Open", true);
                obj.tag = "Closed";
            }
            else if (obj.tag == "Closed")
            {
                obj.GetComponent<Animator>().SetBool("Open", false);
                obj.tag = "Opened";
            }

        }
    }


    void OnTriggerEnter(Collider other)
    {
        Debug.Log(other.gameObject.name + "Entered");
        if (colors == null)
            colors = new Dictionary<int, Color>();
        if (interactions == null)
            interactions = new List<GameObject>();
        if (other.gameObject.layer != 6)
            return;
        // Debug.Log(other.gameObject.name);
        // Material material = other.gameObject.GetComponent<MeshRenderer>().materials[0];
        // Debug.Log(material.GetColor("_SecondShadeMaskColor"));    
        // Color color = other.gameObject.GetComponent<MeshRenderer>().materials[0].GetColor("_2nd_ShadeColor");
        // Debug.Log(color);
        // other.gameObject.GetComponent<MeshRenderer>().materials[0].SetColor("_2nd_ShadeColor",interactionColor);
        // Debug.Log(color);
        // Debug.Log(other.gameObject.GetInstanceID());
        // Debug.Log(colors);
        // colors.Add(other.gameObject.GetInstanceID(), color);
        // Debug.Log("Interaction possible");
        interactions.Add(other.gameObject);

    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != 6)
            return;
        // other.gameObject.GetComponent<MeshRenderer>().materials[0].SetColor("_2nd_ShadeColor",colors[other.gameObject.GetInstanceID()]);
        // colors.Remove(other.gameObject.GetInstanceID());
        interactions.Remove(other.gameObject);
    }
}
