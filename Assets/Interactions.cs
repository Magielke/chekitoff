using System.Collections.Generic;
using UnityEngine;

public class Interactions : MonoBehaviour
{
    [SerializeField] private KeyCode _interactKey = KeyCode.E;
    public Color interactionColor = Color.blue;

    private readonly List<GameObject> interactions = new List<GameObject>();
    private DeskWorkstation _occupiedDesk;

    void Update()
    {
        if (TodoItemUI.AnyEditing || NamePromptPopup.IsOpen || FocusRewardPopup.IsOpen) return;

        if (_occupiedDesk != null && !_occupiedDesk.IsSeated && !_occupiedDesk.IsBusy)
            _occupiedDesk = null;

        if (!Input.GetKeyDown(_interactKey)) return;

        if (_occupiedDesk != null)
        {
            _occupiedDesk.Interact();
            return;
        }

        GameObject obj = GetClosest();
        if (obj == null) return;

        var desk = obj.GetComponentInParent<DeskWorkstation>();
        if (desk != null)
        {
            desk.Interact();
            _occupiedDesk = desk;
            return;
        }

        var animator = obj.GetComponent<Animator>();
        if (animator != null)
        {
            animator.SetBool("Open", !animator.GetBool("Open"));
            return;
        }

        obj.SendMessage("OnInteract", SendMessageOptions.DontRequireReceiver);
    }

    private GameObject GetClosest()
    {
        interactions.RemoveAll(o => o == null);

        GameObject best = null;
        float bestDist = float.MaxValue;
        foreach (var o in interactions)
        {
            float d = (o.transform.position - transform.position).sqrMagnitude;
            if (d < bestDist) { bestDist = d; best = o; }
        }
        return best;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != 6) return;
        if (!interactions.Contains(other.gameObject))
            interactions.Add(other.gameObject);
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.layer != 6) return;
        interactions.Remove(other.gameObject);
    }
}