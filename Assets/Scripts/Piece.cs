using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

namespace Match3
{
    public class Piece : actionBase
    {
        public enum ePieceType
        {
            kNormal,
            kBomb,
            kLineH,
            kLineV,
            kColorSweeper
        }
        public Vector2Int pos;
        public float speed;
        public Vector2Int destPos;
        public int id;
        private bool isChoosed = false;
        private bool scaleUpAnimation = false;
        private bool scaleDownAnimation = false;
        private float animationTimer = 0f;
        private ActionFall fallAction = null;
        private bool isDestroying = false;
        private ePieceType pieceType = ePieceType.kNormal;
        private Dictionary<string, Sprite> spritesMap;
        protected SpriteRenderer render;
        public bool IsDestroying { get { return isDestroying; } set { isDestroying = value; } }

        // Start is called before the first frame update
        void Start()
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>("assets_candy");

            spritesMap = new Dictionary<string, Sprite>();

            foreach (Sprite sprite in sprites)
            {
                spritesMap[sprite.name] = sprite;
            }

            render = GetComponent<SpriteRenderer>();
        }



        // Update is called once per frame
        protected void Update()
        {
            base.Update();
            if (scaleUpAnimation)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1.2f, 1.2f, 1), 0.1f);
            }
            else if(scaleDownAnimation)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, new Vector3(1f, 1f, 1), 0.1f);
                transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0, 0, 0), 0.1f);
            }

            if (isChoosed)
            {
                animationTimer += Time.deltaTime;
                // Shake animation
                transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(animationTimer * 10f) * 5f);
            }
        }

        public void Choose()
        {
            if (isChoosed)
            {
                UnChoose();
                return;
            }
           
            isChoosed = true;
            scaleUpAnimation = true;
            scaleDownAnimation = false;
            animationTimer = 0.0f;
        }

        public void UnChoose()
        {
            isChoosed = false;
            scaleUpAnimation = false;
            scaleDownAnimation = true;
        }
        public ActionFall Fall()
        {
            if (fallAction!=null)
                return fallAction;
            if (!isDestroying)
            {
                fallAction = new ActionFall();
                AddAction(fallAction);
                return fallAction;
            }
            return null;
        }

        public void FallComplete()
        {
            fallAction = null ;
        }

        public ePieceType GetPieceType()
        {
            return pieceType;
        }

        public void SetPieceType(ePieceType type)
        {
            pieceType = type;
            if (type == ePieceType.kNormal)
                render.sprite = spritesMap["candy_" + id];
            else if (type == ePieceType.kBomb)
                render.sprite = spritesMap["bomb_" + id];
            else if (type == ePieceType.kLineH)
                render.sprite = spritesMap["candy_" + id + "_hor"];
            else if (type == ePieceType.kLineV)
                render.sprite = spritesMap["candy_" + id + "_ver"];
            else if (type == ePieceType.kColorSweeper)
            {
                render.sprite = spritesMap["rainbow"];
                id = 6;
            }
        }
    }
}
