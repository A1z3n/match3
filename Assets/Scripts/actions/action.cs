using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Match3 {
    public class action {

        public delegate void actionCallback();

        protected bool cancel = false;
        protected bool remove = false;

        public virtual bool Update(actionBase u, float dt) {
            return true;
        }

        public virtual void Cancel() {
            OnEndCallback = null;
            cancel = true;
        }

        public virtual void Remove() {
            remove = true;
        }

        public virtual bool IsRemoving() {
            return remove;
        }

        public virtual eActionType GetActionType() {
            return eActionType.kActionNone;
        }

        public actionCallback OnEndCallback = null;
    }

    public class actionSeq : action {

        private List<action> actions;
        //public OnEndCallback callback;

        public actionSeq() {
            actions = new List<action>();
        }

        public override bool Update(actionBase u, float dt) {
            if (actions.Count == 0) {
                return false;
            }

            if (!actions[0].Update(u, dt)) {
                actions.RemoveAt(0);
                if (actions.Count == 0) {
                    OnEndCallback?.Invoke();
                    return false;
                }
            }

            return true;
        }

        public void AddAction(action a) {
            actions.Add(a);
        }

        public override void Cancel() {
            foreach (var a in actions) {
                a.Cancel();
            }
        }

    }

    public class actionSim : action {
        List<action> actions;

        //public OnEndCallback callback;
        public actionSim() {
            actions = new List<action>();
        }

        public override bool Update(actionBase u, float dt) {

            foreach (var a in actions) {
                if (!a.Update(u, dt)) {
                    actions.RemoveAt(0);
                    if (actions.Count == 0) {
                        a.OnEndCallback?.Invoke();
                        return false;
                    }
                }
            }

            return true;
        }

        public void AddAction(action a) {

            actions.Add(a);
        }

        public override void Cancel() {
            foreach (var a in actions) {
                a.Cancel();
            }
        }
    }

}