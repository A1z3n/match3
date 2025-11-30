using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineFX : MonoBehaviour
{
    // Start is called before the first frame update
    private Transform tr;
    private bool horizontal;
    private bool reversed;

    public void Init(bool hor, bool reverse)
    {
        horizontal = hor;
        reversed = reverse;
    }
    void Start()
    {
        tr = GetComponent<Transform>();
        if (reversed)
        {
            GetComponent<SpriteRenderer>().flipX = true;
        }
        if(!horizontal)
        {
            tr.Rotate(0, 0, 90);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (horizontal)
        {
            tr.position += new Vector3(!reversed ? 1 : -1, 0, 0) * Time.deltaTime * 10f;
            if(tr.position.x>Camera.main.orthographicSize* Camera.main.aspect +10f || tr.position.x < -Camera.main.orthographicSize * Camera.main.aspect -10f)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            tr.position += new Vector3(0, !reversed ? 1 : -1, 0) * Time.deltaTime * 10f;
            if(tr.position.y> Camera.main.orthographicSize +10f || tr.position.y < -Camera.main.orthographicSize -10f)
            {
                Destroy(gameObject);
            }
        }
    }

}
