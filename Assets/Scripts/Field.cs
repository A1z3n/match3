using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Match3
{
    public enum eFieldType
    {
        kEmpty = -1,//empty field
        kGenerator = 0,//generator field
        kNormal = 1,//normal field
    }
    public class Field : MonoBehaviour
    {
        public eFieldType type; 
        public Vector2Int pos;
        public Field()
        {
            type = eFieldType.kEmpty;
            pos = Vector2Int.zero;
        }
        public void Start()
        {
        }

        public void Update()
        {
        }

        public ActionFall Fall(Match3Puzzle m3 )
        {
            if (type == eFieldType.kGenerator)
            {
                // create new piece from generator
                var piece = m3.CreatePieceAt(Random.Range(1, 6), pos.x, pos.y);
                ActionFall actionFall = piece.Fall();
                actionFall.SetCreatedNewBlock(true);
                return actionFall;
            }
            return null;
        }


    }
}
