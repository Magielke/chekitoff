using System;
using System.Collections.Generic;
using UnityEngine;

public class Interactions : MonoBehaviour
{
    public Color interactionColor = Color.blue;
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    
    private readonly List<GameObject> _interactions = new List<GameObject>();

    private DeskWorkstation _occupiedDesk;

    void Update()
    {
        if (_occupiedDesk && !_occupiedDesk.IsSeated && !_occupiedDesk.IsBusy)
            _occupiedDesk = null;
        if (!Input.GetKeyDown(_interactKey)) return;

        //0) Wstać od biurka
        if (_occupiedDesk)
        {
            _occupiedDesk.Interact();
            return;
        }

        GameObject obj = GetClosest();
        if(!obj) return;

        //1) biurko i timer
        var desk = obj.GetComponentInParent<DeskWorkstation>();
        if (desk)
        {
            desk.Interact();
            _occupiedDesk = desk;
            return;
        }
        
        //2) drzwi/szafki
        var animator = obj.GetComponent<Animator>();
        if (animator)
        {
            bool isOpen = animator.GetBool("Open");
            animator.SetBool("Open", !isOpen);
            return;
        }

        //3)
        obj.SendMessage("OnInteract", SendMessageOptions.DontRequireReceiver);
    }

    private GameObject GetClosest()
    {
        _interactions.RemoveAll(o => !o);
        GameObject best = null;
        float bestDistance = float.MaxValue;
        foreach (var o in _interactions)
        {
            float d = (o.transform.position - transform.position).sqrMagnitude;
            if(d < bestDistance) {bestDistance = d; best = o; }
        }
        return best;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.layer != 6) return;
        if(!_interactions.Contains(other.gameObject))
            _interactions.Add(other.gameObject);
    }

    private void OnTriggerExit(Collider other)
    {
        if(other.gameObject.layer != 6) return;
        _interactions.Remove(other.gameObject);
    }
}
