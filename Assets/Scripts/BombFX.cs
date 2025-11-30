using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class BombFX : MonoBehaviour
{
    private Sprite[] spritesList;
    protected SpriteRenderer render;
    private float timer = 0.0f;
    private int frame = 0;
    // Start is called before the first frame update
    void Start()
    {
        // Load all sprites from the Resources folder
        spritesList = Resources.LoadAll<Sprite>("explotion");
        render = GetComponent<SpriteRenderer>();

    }

    // Update is called once per frame
    void Update()
    {
        // Advance the animation based on time
        timer += Time.deltaTime;
        if (timer > 0.1f)
        {
            timer -= 0.1f;
            frame++;
            if (frame >= spritesList.Length)
            {
                Destroy(gameObject);
                return;
            }
        }
        render.sprite = spritesList[frame];
    }
}
