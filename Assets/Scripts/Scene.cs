using Match3;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Scene : MonoBehaviour
{
    // Start is called before the first frame update

    private Match3Puzzle puzzle;
    void Start()
    {

        GameManager.GetInstance().Load();
    }

    // Update is called once per frame
    void Update()
    {
        if (puzzle==null)
        {
            puzzle = GameManager.GetInstance().GetMatch3Puzzle();
        }
        if (puzzle!=null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                puzzle.OnClick(worldPoint.x, worldPoint.y);
            }

            if (Input.GetMouseButtonDown(1))
            {
                Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                puzzle.OnRightClick(worldPoint.x, worldPoint.y);
            }
            if (Input.GetMouseButton(0))
            {
                Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                puzzle.OnDrag(worldPoint.x, worldPoint.y);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                puzzle.OnRelease(worldPoint.x, worldPoint.y);
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                //Cheat puzzle shuffle
                puzzle.ShufflePieces();
            }
            puzzle.Update(Time.deltaTime);
        }
    }
}
