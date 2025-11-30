using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Match3
{
    static class actionManager {
        public static bool moveTo(Piece p, int destX, int destY, Vector3 shift) {
            // piece move to destX destY
            Vector2Int start = p.pos;  
            Vector2Int dest = new Vector2Int(destX, destY);
            if (destX == start.x && destY == start.y) {
                return false;
            }
            
            p.destPos.x = destX;
            p.destPos.y = destY;

            moveAction a = new moveAction();
            a.Init(destX, destY, shift);
            p.AddActionSeq(a);
                
            return true;
        }

    }
}
