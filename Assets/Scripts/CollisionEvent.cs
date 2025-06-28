       using UnityEngine;
       using UnityEngine.Events;
       
       public class CollisionEvent : MonoBehaviour
       {
           public UnityEvent myEvent;
           
           public void OnTriggerEnter2D(Collider2D collision)
           {
               // Check if the collision is with a specific object, if needed
               if (collision.gameObject.CompareTag("Player"))
               {
                   CallMyEvent(); // Call the event when a collision occurs
               }
           }
       
           public void CallMyEvent()
    {
        myEvent.Invoke(); // Invokes all listeners
    }
       }