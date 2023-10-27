using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovetheCircle : MonoBehaviour
{
    public Transform startTransform; // Assign the starting position in the Inspector
    public Transform targetTransform; // Assign the target position in the Inspector
    public float speed = 5.0f; // Adjust the speed of movement in the Inspector

    private bool movingToTarget = true;

    void Update()
    {
        Vector3 targetPosition = movingToTarget ? targetTransform.position : startTransform.position;
        float step = speed * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, step);

        if (transform.position == targetPosition)
        {
            movingToTarget = !movingToTarget;
        }
    }
}

