using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class moveAction : action {
        private Vector2 startPos;
        private Vector2 destPos;
        private int destX;
        private int destY;
        private int startX;
        private int startY;
        private float timer = 0.0f;
        private float duration;
        private bool inited = false;

        public void Init(int x, int y, Vector3 pShift) {
            duration = 0.2f;
            destX = x;
            destY = y;
            destPos = tools.iPos2Pos(x, y) + pShift;
        }


        public override bool Update(actionBase p, float dt) {
            if (!inited) {
                inited = true;
                var obj = p as Piece;
                startPos = obj.transform.position;
                startX = obj.pos.x;
                startY = obj.pos.y;
                obj.pos.x = destX;
                obj.pos.y = destY;
                duration = 0.2f;
                //obj.PositionChanged();
            }

            if (cancel) {
                return false;
            }

            timer += dt;
            if (timer >= duration) {
                var obj = p as Piece;
                obj.pos = new Vector2Int(destX, destY);
                p.transform.position = destPos;
                //OnEndCallback();
                return false;
            }

            float progress = timer / duration;
            p.transform.position = startPos + (destPos - startPos) * progress;
            return true;
        }

        public float GetProgress() {
            return timer / duration;
        }

        public override eActionType GetActionType() {
            return eActionType.kMoveAction;
        }

    }
}
