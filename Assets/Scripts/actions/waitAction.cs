using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3
{
    public class waitAction : action {
        private float timer;

        public void Init(float t) {
            timer = t;
        }

        public override bool Update(actionBase u, float dt) {
            timer -= dt;
            return !(timer <= 0);
        }

        public override eActionType GetActionType() {
            return eActionType.kWaitAction;
        }
    }
}