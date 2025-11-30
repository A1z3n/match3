using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Match3
{
    public class ActionFall : action
    {

        private bool inited = false;
        private Vector2 startPos;
        private Vector2 destPos;
        private int destX;
        private int destY;
        private int startX;
        private int startY;
        private float timer = 0.0f;
        private float duration; 
        private Vector2Int lastAdded;
        private bool falling = false;
        private Queue<Vector2Int> steps;
        private Vector3 shift;
        private Vector2 path;
        private Match3Puzzle puzzle;
        private Piece piece = null;
        private bool createdNewBlock = false;
        bool fieldPosChanged = false;
        bool isNewBlock;
        int newBlockId;

        public ActionFall()
        {
            createdNewBlock = false;
            isNewBlock = false;
            newBlockId = -1;
            puzzle = GameManager.GetInstance().GetMatch3Puzzle();
        }

        ~ActionFall()
        {
        }

        public void Init(Vector3 pShift)
        {
            shift = pShift;
            lastAdded.y = -1;
            lastAdded.x = 0;
            duration = 0.2f;
            steps = new Queue<Vector2Int>();
        }

        public override bool Update(actionBase p, float dt)
        {
            if (!inited)
            {
                inited = true;
                piece = p as Piece;
                startPos = piece.transform.position;
                if (createdNewBlock)
                {
                    startPos.x += shift.x;
                    startPos.y += shift.y;
                }
                startX = piece.pos.x;
                startY = piece.pos.y;
                puzzle.AddActions();
            }

            if (cancel)
            {
                puzzle.ReduceActions();
                return false;
            }

            timer += dt;

            if (piece == null)
            {
                if (!fieldPosChanged)
                    puzzle.SetEmpty(new Vector2Int(destX, destY));
                else if (steps.Count() > 0)
                {
                    puzzle.SetEmpty(steps.Peek());
                }
                puzzle.ReduceActions();
                return false;
            }

            if (falling)
            {
                
                if (!fieldPosChanged)
                {
                    fieldPosChanged = true;
                    /*if (isNewBlock)
                    {
                        puzzle.FallNew(new Vector2Int(destX,destY), newBlockId);
                        isNewBlock = false;
                    }
                    else*/
                    {
                        Vector2Int blockPos = piece.pos;
                        //Debug.Log("Action Fall from " + blockPos + " to " + new Vector2Int(destX,destY));
                        puzzle.Fall(blockPos, new Vector2Int(destX,destY));
                        
                        
                    }
                }

            }

            float progress = timer / duration;
            if (progress > 1.0f)
            {
                progress = 1.0f;
            }
            p.transform.position = startPos + (destPos - startPos) * progress;
            if (timer >= duration)
            {
                piece.pos = new Vector2Int(destX, destY);
                p.transform.position = destPos;
                startPos = destPos;
                if (steps.Count()>0)
                {
                    destX = steps.Peek().x;
                    destY = steps.Peek().y;
                    steps.Dequeue();
                    destPos = tools.iPos2Pos(destX, destY) + shift;
                    fieldPosChanged = false;
                    path = destPos - startPos;
                    timer = 0;
                    return true;

                }
                falling = false;
                timer = 0;

                //OnEndCallback();
                piece.FallComplete();
                puzzle.ReduceActions();
                return false;
            }

            return true;
        }

        public float GetProgress()
        {
            return timer / duration;
        }

        public override eActionType GetActionType()
        {
            return eActionType.kFallAction;
        }


        public bool AddPath(int x, int y)
        {
            if (lastAdded.y < y)
            {
                if (falling)
                {
                    steps.Enqueue(new Vector2Int(x,y));
                }
                else
                {
                    falling = true;
                    fieldPosChanged = false;
                    destX = x;
                    destY = y;
                    destPos = tools.iPos2Pos(x, y) + shift;
                    path = destPos - startPos;
                    timer = 0;
                }
                lastAdded.x = x;
                lastAdded.y = y;


                return true;
            }
            return false;
        }
        public void SetCreatedNewBlock(bool value)
        {
            createdNewBlock = value;
        }

        public void SetNewBlock(int id)
        {
            isNewBlock = true;
            newBlockId = id;
            if (createdNewBlock)
            {
               // blockWeak.lock ()->drawSpec.SetSourceRect(SexyRect(0, 0, data->tileStep.x, 0));
            }
        }
    }
}
