using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Match3 {
    public class actionBase : MonoBehaviour {
        // Start is called before the first frame update
        private List<action> actions;
        private List<action> actionsToAdd;
        private List<action> actionsDelayed;
        private List<action> delItems;

        public actionBase() {
            actions = new List<action>();
            delItems = new List<action>();
            actionsToAdd = new List<action>();
            actionsDelayed = new List<action>();
        }


        // Update is called once per frame
        protected void Update() {
            float dt = Time.deltaTime;
            

            foreach (var currentAction in actions)
            {
                if (!currentAction.IsRemoving() && !currentAction.Update(this, dt))
                {
                    currentAction.OnEndCallback?.Invoke();
                    delItems.Add(currentAction);
                }
            }

            if (delItems.Count()>0) {
                foreach (var delItem in delItems) {
                    actions.Remove(delItem);
                }

                delItems.Clear();
            }

            if (actions.Count()==0) {
                if (actionsToAdd.Count()>0) {

                    AddActionInternal(actionsToAdd[0]);
                    actionsToAdd.RemoveAt(0);
                }
            }

            if (actionsDelayed.Count()>0) {
                foreach (var a in actionsDelayed) {
                    AddActionInternal(a);
                }

                actionsDelayed.Clear();
            }

        }

        private void AddActionInternal(action a) {
            if (actions.Equals(null)) {
                actions = new List<action>();
            }
            actions.Add(a);

        }

        public void AddActionSeq(action a) {
            actionsToAdd.Add(a);
        }

        public void AddAction(action a) {
            actionsDelayed.Add(a);
        }

        private void RemoveActionInternal(action a) {
            actions.Remove(a);
        }
        public void RemoveAction(action a)
        {
            delItems.Add(a);
        }

        public void ClearActions() {
            actions.Clear();
            actionsToAdd.Clear();
        }
     

        public List<action> GetActions() {
            return actions;
        }

        public void ClearActionsType(eActionType a) {
            foreach(var currentAction in actions)
            {
                if (currentAction.GetActionType()==a) {
                    RemoveAction(currentAction);
                }
            }
        }

   

        public bool IsActions() {
            return actions.Count()>0 || actionsToAdd.Count()>0 || actionsDelayed.Count()>0;
        }

        public void CancelActions() {
            foreach (var currentAction in actions) {
                currentAction.Cancel();
            }

            actionsToAdd.Clear();
            actionsDelayed.Clear();
        }

        public void CancelNextActions() {
            int l = actions.Count();
            if (l > 1) {
                for (int i = 1; i < l; i++) {
                    actions[i].Cancel();
                }
            }
            actionsToAdd.Clear();
            actionsDelayed.Clear();
        }



    }
}