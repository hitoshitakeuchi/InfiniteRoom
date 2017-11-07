using UnityEngine;
using System.Collections;

public class AutoRotate : MonoBehaviour
{
    public float rot = -0.06f;

    void Start()
    {

    }

    void Update()
    {
        transform.Rotate(Vector3.up, Time.smoothDeltaTime * rot);
        transform.position += new Vector3(0f, 0.00002f, 0f);
    }
}
