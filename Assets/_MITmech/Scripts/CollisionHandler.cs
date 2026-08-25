using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class CollisionHandler : MonoBehaviour
{
    public enum IncludeOrExclude { Include,Exclude}
    public IncludeOrExclude includeOrExclude;

    public UnityEvent CollisionHappenedEvent;
    public List<GameObject> collidableObjects;

    private void OnTriggerEnter(Collider other)
    {

        if(collidableObjects.Count < 0 && collidableObjects.Contains(other.gameObject))
        {
            return;
        }
        if (includeOrExclude == IncludeOrExclude.Include && collidableObjects.Contains(other.gameObject))
        {
            CollisionHappenedEvent.Invoke();
        }
        if (includeOrExclude == IncludeOrExclude.Exclude && !collidableObjects.Contains(other.gameObject))
        {
            print(other.gameObject);
            CollisionHappenedEvent.Invoke();
        }
    }
}
