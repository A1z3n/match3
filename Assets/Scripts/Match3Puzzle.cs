using System;
using System.Collections.Generic;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Random = UnityEngine.Random;
using System.Linq;

namespace Match3
{
    public class Match3Puzzle
    {
        enum FallFrom
        {
            kNoFall,
            kFallGenerator,
            kFromTop,
            kFromLeft,
            kFromRight,
            kFromLeftRight,
        };


        public enum SearchType
        {
            kNull,
            kBomb,
            kLineSweeperH,
            kLineSweeperV,
            kColorSweeper,
            kTriple
        };

        struct FallData
        {
            public FallFrom from;
            private bool empty;
            public void SetEmpty(bool e, int x, int y)
            {
                empty = e;
            }

            public void SetEmpty(bool e)
            {
                empty = e;
            }
            public bool IsEmpty()
            {
                return empty;
            }
        };
        private List<Field> fields;
        private List<Piece> pieces;
        private GameObject fieldsLayer;
        private eFieldType[,] mapData;
        private int[,] piecesData;
        private int width;
        private int height;
        private Vector3 shift;
        private Piece firstChoosedPiece = null;
        private Piece secondChoosedPiece = null;
        private Vector2 clickPos;
        private bool isBusy = false;
        private float busyTimer = 0.0f;
        private bool falling = false;
        private bool unswapping = false;
        private FallData[,] fallData;
        Gui gui;
        private int actions = 0;
        private bool isMatchesChecked = false;
        private List<Vector2Int> deleteCandidates;
        private bool[,] checkedBlocks;
        public Match3Puzzle()
        {
            clickPos = Vector2.zero;
            pieces = new List<Piece>();
            fields = new List<Field>();
            fieldsLayer = GameObject.Find("fields");
            GameManager.GetInstance().SetMatch3Puzzle(this);
            gui = GameObject.Find("GUI").GetComponent<Gui>();
        }
        public void Init(eFieldType[,] pMapData, Vector3 shift)
        {
            this.shift = shift;
            mapData = pMapData;
            width = mapData.GetLength(0);
            height = mapData.GetLength(1);
            piecesData = new int[width, height];
            fallData = new FallData[width, height];
            deleteCandidates = new List<Vector2Int>();
            checkedBlocks = new bool[width, height];
        }


        public void FillPieces()
        {
            //first fill piecesData with random pieces without matches
            while (true)
            {
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        piecesData[x, y] = 0;
                        if (mapData[x, y] == eFieldType.kNormal)
                        {
                            int id = Random.Range(1, 6);
                            piecesData[x, y] = id;
                        }
                    }
                }
                if (!CheckMatchesSimple()) break;
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    CreatePieceAt(piecesData[x, y], x, y);
                    fallData[x, y].from = FallFrom.kNoFall;
                    fallData[x, y].SetEmpty(false,x,y);

                    CreateFieldAt(mapData[x, y], x, y);
                }
            }

            BuildFallMap();
        }

        public Piece CreatePieceAt(int id, int x, int y)
        {
            //Create piece game object at point (x,y) with id 1-5
            if (id <= 0 || id > 5) return null;
            GameObject g = UnityEngine.Object.Instantiate(Resources.Load("piece_" + id, typeof(GameObject)), fieldsLayer.transform, false) as
                           GameObject;
            var piece = g.GetComponent<Piece>();
            piece.pos.x = x;
            piece.pos.y = y;
            piece.id = id;
            g.transform.position = tools.iPos2Pos(piece.pos.x, piece.pos.y);
            g.GetComponent<SpriteRenderer>().sortingLayerName = "pieces";
            pieces.Add(piece);
            return piece;
        }

        private int GetPieceColor(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return -1;
            return piecesData[x, y];
        }

        public Field CreateFieldAt(eFieldType type, int x, int y)
        {
            //Create field game object at point (x,y) with type
            if (type == eFieldType.kNormal || type == eFieldType.kGenerator)
            {
                GameObject g = UnityEngine.Object.Instantiate(Resources.Load("field", typeof(GameObject)), fieldsLayer.transform, false) as
                               GameObject;
                var field = g.GetComponent<Field>();
                field.pos.x = x;
                field.pos.y = y;
                field.type = type;
                g.transform.position = tools.iPos2Pos(field.pos.x, field.pos.y);
                g.GetComponent<SpriteRenderer>().sortingLayerName = "field";
                if (type == eFieldType.kGenerator)
                    g.GetComponent<SpriteRenderer>().enabled = false;
                fields.Add(field);
                return field;
            }
            return null;
        }

        private bool CheckMatchesSimple()
        {
            //initial check for matches in piecesData
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int id = piecesData[x, y];
                    if (id == 0) continue;
                    if (x <= width - 3)
                    {
                        if (piecesData[x + 1, y] == id && piecesData[x + 2, y] == id)
                        {
                            return true;
                        }
                    }
                    if (y <= height - 3)
                    {
                        if (piecesData[x, y + 1] == id && piecesData[x, y + 2] == id)
                        {
                            return true;
                        }
                    }
                }
            }
            if (!CheckHaveMoves()) return true;
            return false;
        }

        public bool OnClick(float x, float y)
        {
            //left button click
            if (isBusy || falling || actions > 0) return false;
            foreach (var p in pieces)
            {
                Vector3 pPos = tools.iPos2Pos(p.pos.x, p.pos.y) + shift;
                float dist = Vector2.Distance(new Vector2(pPos.x, pPos.y), new Vector2(x, y));
                if (dist < 0.5f)
                {
                    if (firstChoosedPiece == null)
                    {
                        firstChoosedPiece = p;
                        clickPos = new Vector2(x, y);
                        p.Choose();
                        return true;
                    }
                    else if (secondChoosedPiece == null && p != firstChoosedPiece)
                    {
                        if (p.pos.x == firstChoosedPiece.pos.x &&
                           Mathf.Abs(p.pos.y - firstChoosedPiece.pos.y) == 1 ||
                           p.pos.y == firstChoosedPiece.pos.y &&
                           Mathf.Abs(p.pos.x - firstChoosedPiece.pos.x) == 1)
                        {
                            secondChoosedPiece = p;
                            SwapPieces();
                        }
                        else
                        {
                            firstChoosedPiece.UnChoose();
                            firstChoosedPiece = p;
                        }
                        return true;
                    }
                    else if (secondChoosedPiece == null && p == firstChoosedPiece)
                    {
                        firstChoosedPiece = null;
                        p.UnChoose();
                        return true;

                    }
                    return false;
                }
            }
            return false;
        }

        public bool OnRightClick(float x, float y)
        {
            //Cheat on right mouse click - destroy piece
            if (isBusy || falling || actions > 0) return false;
            foreach (var p in pieces)
            {
                Vector3 pPos = tools.iPos2Pos(p.pos.x, p.pos.y) + shift;
                float dist = Vector2.Distance(new Vector2(pPos.x, pPos.y), new Vector2(x, y));
                if (dist < 0.5f)
                {
                    List<Vector2Int> destroyPieces = new List<Vector2Int>();
                    destroyPieces.Add(new Vector2Int(p.pos.x, p.pos.y));
                    DestroyPieces(destroyPieces);
                    return true;
                }
            }
            return false;
        }
        public bool OnDrag(float x, float y)
        {
            //Drag mouse
            if (firstChoosedPiece == null || isBusy || falling || actions > 0) return false;
            Vector2 delta = clickPos - new Vector2(x, y);
            float dist = delta.magnitude;
            if (delta.x < -0.5f)
            {
                var p = GetPiece(firstChoosedPiece.pos.x + 1, firstChoosedPiece.pos.y);
                if (p != null)
                {
                    secondChoosedPiece = p;
                    SwapPieces();
                }
            }
            else if (delta.x > 0.5f)
            {
                var p = GetPiece(firstChoosedPiece.pos.x - 1, firstChoosedPiece.pos.y);
                if (p != null)
                {
                    secondChoosedPiece = p;
                    SwapPieces();
                }
            }
            else if (delta.y < -0.5f)
            {
                var p = GetPiece(firstChoosedPiece.pos.x, firstChoosedPiece.pos.y - 1);
                if (p != null)
                {
                    secondChoosedPiece = p;
                    SwapPieces();
                }
            }
            else if (delta.y > 0.5f)
            {
                var p = GetPiece(firstChoosedPiece.pos.x, firstChoosedPiece.pos.y + 1);
                if (p != null)
                {
                    secondChoosedPiece = p;
                    SwapPieces();
                }
            }

            return false;
        }


        public void OnRelease(float x, float y)
        {
            //Debug.Log("Release at " + x + " " + y);
        }

        public void Update(float deltaTime)
        {
            if (isBusy && busyTimer > 0.0f)
            {
                //TODO: change to without timer
                busyTimer -= deltaTime;
                if (busyTimer <= 0)
                {
                    if (!CheckPieces())
                    {
                        if (!unswapping) UnSwapPieces();
                        else
                        {
                            unswapping = false;
                            isBusy = false;
                        }
                    }
                    else
                    {
                        firstChoosedPiece = null;
                        secondChoosedPiece = null;
                        isBusy = false;
                    }
                }
            }
            {
                //Free holes check
                bool fnd = false;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        if (fallData[x, y].IsEmpty())
                        {

                            Vector2Int from = new Vector2Int(-1, -1);
                            if (fallData[x, y].from == FallFrom.kFromTop)
                                from = new Vector2Int(x, y - 1);
                            else if (fallData[x, y].from == FallFrom.kFromLeft)
                            {
                                if (piecesData[x - 1, y] > 0)
                                {
                                    var p = GetPiece(x - 1, y - 1);
                                    if(p!=null && !p.IsActions())
                                        from = new Vector2Int(x - 1, y - 1);
                                }
                            }
                            else if (fallData[x, y].from == FallFrom.kFromRight)
                            {
                                if (piecesData[x + 1, y] > 0)
                                {
                                    var p = GetPiece(x + 1, y - 1);
                                    if (p != null && !p.IsActions())
                                        from = new Vector2Int(x + 1, y - 1);
                                }
                            }
                            else if (fallData[x, y].from == FallFrom.kFromLeftRight)
                            {
                                int fromRandom = Random.Range(0, 2);
                                bool leftAndRightVNorme = false;
                                if (piecesData[x - 1, y] == 1 && piecesData[x + 1, y] == 1)
                                    leftAndRightVNorme = true;
                                if (leftAndRightVNorme)
                                {
                                    if (fromRandom == 0)
                                    {
                                        if (piecesData[x - 1, y - 1] != 0)
                                            from = new Vector2Int(x - 1, y - 1);
                                        else
                                            if (piecesData[x + 1, y - 1] != 0)
                                            from = new Vector2Int(x + 1, y - 1);
                                    }
                                    else
                                    {
                                        if (piecesData[x + 1, y - 1] != 0)
                                            from = new Vector2Int(x + 1, y - 1);
                                        else
                                            if (piecesData[x - 1, y - 1] != 0)
                                            from = new Vector2Int(x - 1, y - 1);

                                    }
                                }
                            }
                            else if (fallData[x, y].from == FallFrom.kNoFall && fallData[x, y].IsEmpty())
                            {
                                bool top = (fallData[x, y - 1].from != FallFrom.kNoFall);
                                bool left = (x > 0 && fallData[x - 1, y - 1].from != FallFrom.kNoFall);
                                bool right = (x < width - 1 && fallData[x + 1, y - 1].from != FallFrom.kNoFall);
                                if (left || right || top)
                                {
                                    BuildFallMap();
                                    continue;
                                }
                            }
                            if (from.x != -1 && from.y != -1)
                            {
                                Piece p = GetPiece(from.x, from.y);
                                if (p != null)
                                {
                                    ActionFall fallAction = p.Fall();
                                    if (fallAction != null)
                                    {
                                        fnd = true;
                                        fallAction.Init(shift);
                                        piecesData[x, y] = 0;
                                        if (fallAction.AddPath(x, y))
                                        {
                                            fallData[x, y].SetEmpty(false,x,y);
                                        }
                                        else if (!fallData[x, y].IsEmpty())
                                        {
                                            Debug.LogError("error in fallmap!");
                                            fallData[x, y].SetEmpty(true,x,y);
                                        }
                                    }
                                }
                                else
                                {
                                    Field f = GetField(from.x, from.y);
                                    if (f != null && f.type == eFieldType.kGenerator)
                                    {
                                        int newId = Random.Range(1, 6);
                                        //f.Fall(this);
                                        Piece newPiece = CreatePieceAt(newId, from.x, from.y);
                                        if (newPiece != null)
                                        {
                                            ActionFall fallAction = newPiece.Fall();
                                            if (fallAction != null)
                                            {
                                                fnd = true;
                                                fallAction.Init(shift);
                                                fallAction.SetCreatedNewBlock(true);
                                                fallAction.SetNewBlock(newId);
                                                piecesData[from.x, from.y] = 0;
                                                if (fallAction.AddPath(from.x, from.y))
                                                {
                                                    fallData[from.x, from.y].SetEmpty(false,from.x,from.y);
                                                }
                                                else if (!fallData[from.x, from.y].IsEmpty())
                                                {
                                                    Debug.LogError("error in fallmap!");
                                                    fallData[from.x, from.y].SetEmpty(true,from.x,from.y);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                if (fnd)
                {

                }
                else
                {

                    
                    falling = false;
                    //isBusy = false;
                }
            }
        }

        private void SwapPieces()
        {
            if (firstChoosedPiece == null && secondChoosedPiece == null) return;

            bool swapped = actionManager.moveTo(firstChoosedPiece, secondChoosedPiece.pos.x, secondChoosedPiece.pos.y, shift);
            if (swapped)
            {
                actionManager.moveTo(secondChoosedPiece, firstChoosedPiece.pos.x, firstChoosedPiece.pos.y, shift);
                piecesData[firstChoosedPiece.pos.x, firstChoosedPiece.pos.y] = secondChoosedPiece.id;
                piecesData[secondChoosedPiece.pos.x, secondChoosedPiece.pos.y] = firstChoosedPiece.id;
            }
            firstChoosedPiece.UnChoose();
            secondChoosedPiece.UnChoose();
            isBusy = true;
            busyTimer = 0.2f;

        }

        private void UnSwapPieces()
        {
            if (firstChoosedPiece == null || secondChoosedPiece == null) return;
            bool swapped = actionManager.moveTo(firstChoosedPiece, secondChoosedPiece.pos.x, secondChoosedPiece.pos.y, shift);
            if (swapped)
            {
                actionManager.moveTo(secondChoosedPiece, firstChoosedPiece.pos.x, firstChoosedPiece.pos.y, shift);
                piecesData[firstChoosedPiece.pos.x, firstChoosedPiece.pos.y] = secondChoosedPiece.id;
                piecesData[secondChoosedPiece.pos.x, secondChoosedPiece.pos.y] = firstChoosedPiece.id;
            }
            firstChoosedPiece.UnChoose();
            secondChoosedPiece.UnChoose();
            firstChoosedPiece = null;
            secondChoosedPiece = null;
            isBusy = true;
            busyTimer = 0.2f;
            unswapping = true;
        }

        public Piece GetPiece(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return null;
            foreach (var p in pieces)
            {
                if (p.pos.x == x && p.pos.y == y) return p;
            }
            return null;
        }

        public Field GetField(int x, int y)
        {
            if (x < 0 || x >= width || y < 0 || y >= height) return null;
            foreach (var f in fields)
            {
                if (f.pos.x == x && f.pos.y == y) return f;
            }
            return null;
        }

        private bool CheckPieces()
        {
            //Check for matches
            if(firstChoosedPiece!=null && secondChoosedPiece != null)
            {
                //check color sweep 
                bool fnd = false;
                if (firstChoosedPiece.GetPieceType()==Piece.ePieceType.kColorSweeper)
                {
                    int id = secondChoosedPiece.id;
                    DestroyPiece(secondChoosedPiece);
                    DestroyColorSweeper(firstChoosedPiece,id);
                    DestroyPiece( firstChoosedPiece);
                    firstChoosedPiece = null;
                    fnd = true;
                }
                if (secondChoosedPiece.GetPieceType() == Piece.ePieceType.kColorSweeper)
                {
                    int id = firstChoosedPiece.id;
                    DestroyPiece(firstChoosedPiece);
                    DestroyColorSweeper(secondChoosedPiece,id);
                    DestroyPiece ( secondChoosedPiece);
                    secondChoosedPiece = null;
                    fnd = true;
                }
                if (fnd)
                {
                    return true;
                }
            }

            bool result = false;
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                    checkedBlocks[x,y] = false;

            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                {
                    Check(x, y, 0);
                    Check(x, y, 1);
                }

            List<Vector2Int> destroyPieces = new List<Vector2Int>();
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (checkedBlocks[x, y])
                    {
                        int id = piecesData[x, y];
                        if (id == 0) continue;
                        int color = piecesData[x, y];
                        var list = CheckSpecials(x, y,color);

                        if (list.Item1 != SearchType.kNull)
                        {
                            if (list.Item1 == SearchType.kColorSweeper)
                            {
                                CreateSpecialPiece(list.Item2[2], SearchType.kColorSweeper);
                                list.Item2.RemoveAt(2);
                                foreach(var pos in list.Item2)
                                {
                                    destroyPieces.Add(pos);
                                }

                            }
                            else if (list.Item1 == SearchType.kLineSweeperH || list.Item1 == SearchType.kLineSweeperV)
                            {
                                CreateSpecialPiece(list.Item2[0],list.Item1);
                                list.Item2.RemoveAt(0);
                                foreach (var pos in list.Item2)
                                {
                                    destroyPieces.Add(pos);
                                }
                            }
                            else if (list.Item1 == SearchType.kBomb)
                            {
                                CreateSpecialPiece(list.Item2[0], list.Item1);
                                list.Item2.RemoveAt(0);
                                foreach (var pos in list.Item2)
                                {
                                    destroyPieces.Add(pos);
                                }
                            }
                        }
                    }
                    
                    
                }
            }
            var uniqueList = destroyPieces.Distinct().ToList();
            foreach (var p in destroyPieces)
            {
                result = true;
                uniqueList.Add(p);
            }
            DestroyPieces(uniqueList);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (CheckTriples(x, y))
                    {
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool CheckTriples(int x, int y)
        {
            // Check match3
            bool result = false;
            int id =  GetPieceColor(x, y);
            if (id == -1) return false;
            if (x <= width - 3)
            {
                if (piecesData[x + 1, y] == id && piecesData[x + 2, y] == id)
                {
                    List<Piece> destroyPieces = new List<Piece>();
                    var p1 = GetPiece(x, y);
                    var p2 = GetPiece(x + 1, y);
                    var p3 = GetPiece(x + 2, y);

                    if (p1 != null && p2 != null && p3 != null)
                    {
                        destroyPieces.Add(p1);
                        destroyPieces.Add(p2);
                        destroyPieces.Add(p3);
                        DestroyPieces(destroyPieces);
                        result = true;
                    }
                }
            }
            if (y <= height - 3)
            {
                if (piecesData[x, y + 1] == id && piecesData[x, y + 2] == id)
                {
                    List<Piece> destroyPieces = new List<Piece>();
                    var p1 = GetPiece(x, y);
                    var p2 = GetPiece(x, y + 1);
                    var p3 = GetPiece(x, y + 2);

                    if (p1 != null && p2 != null && p3 != null)
                    {
                        destroyPieces.Add(p1);
                        destroyPieces.Add(p2);
                        destroyPieces.Add(p3);
                        DestroyPieces(destroyPieces);
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool CreateSpecialPiece(Vector2Int pos, SearchType type)
        {
            //Create special piece at pos with type
            var piece = GetPiece(pos.x, pos.y);
            if (piece != null)
            {
                switch(type)
                {
                    case SearchType.kBomb:
                        piece.SetPieceType(Piece.ePieceType.kBomb);
                        break;
                    case SearchType.kLineSweeperH:
                        piece.SetPieceType(Piece.ePieceType.kLineH);
                        break;
                    case SearchType.kLineSweeperV:
                        piece.SetPieceType(Piece.ePieceType.kLineV);
                        break;
                    case SearchType.kColorSweeper:
                        piece.SetPieceType(Piece.ePieceType.kColorSweeper);
                        break;
                }
            }
            return false;
        }


        private void Check(int x, int y, int direction)
        {
            //Optimizition check for matches
            int color = piecesData[x, y];
            if (direction == 0 && x < width - 2)
            {
                if (piecesData[x + 1, y] == color &&
                    piecesData[x + 2, y] == color)
                {
                    checkedBlocks[x, y] = true;
                    checkedBlocks[x + 1, y] = true;
                    checkedBlocks[x + 2, y] = true;
                }
                
            }
            else if (y < height - 2)
            {
                if (piecesData[x, y + 1] == color &&
                   piecesData[x, y + 2] == color)
                {
                    checkedBlocks[x, y] = true;
                    checkedBlocks[x, y + 1] = true;
                    checkedBlocks[x, y + 1] = true;
                }


            }
        }

        private  Tuple<SearchType, List<Vector2Int>> CheckSpecials(int x, int y, int color)
        {
            //Check for special pieces creating
            Tuple<SearchType, List<Vector2Int> > result = new Tuple<SearchType, List<Vector2Int>>(SearchType.kNull, new List<Vector2Int>());
            SearchType found = SearchType.kNull;
            if (piecesData[x,y] != color)
                return result;
            if (!checkedBlocks[x,y])
                return result;
            checkedBlocks[x,y] = false;
            if (CheckColorSweeper(x, y))
            {
                found = SearchType.kColorSweeper;
            }
            else if (CheckBomb(x, y))
            {
                found = SearchType.kBomb;
            }
            else if (CheckLineSweeperV(x, y))
            {
                found = SearchType.kLineSweeperV;
            }
            else if (CheckLineSweeperH(x, y))
            {
                found = SearchType.kLineSweeperH;
            }
            else if (CheckTriple(x, y, false) || CheckTriple(x, y, true))
            {
                //found = kTriple;
            }

            if (found != SearchType.kNull)
            {
                List<Vector2Int> destroyList = new List<Vector2Int>();
                for (int i = 0; i < deleteCandidates.Count; i++)
                {
                    checkedBlocks[deleteCandidates[i].x,deleteCandidates[i].y] = false;
                    destroyList.Add(deleteCandidates[i]);
                }
                result = new Tuple<SearchType, List<Vector2Int>>(found, destroyList);
                deleteCandidates.Clear();
            }
            else
            {
                
                deleteCandidates.Clear();
            }

            return result;
        }

        private bool CheckColorSweeper(int x, int y)
        {
            // line of 5
            int color = GetPieceColor(x,y);
            if (color == -1) return false;
            if (x < width - 4)
            {
                bool found = true;
                for (int i = x + 1; i < x + 5; ++i)
                {
                    deleteCandidates.Add(new Vector2Int(i, y));
                    if (GetPieceColor(i, y) != color)
                    {
                        deleteCandidates.Clear();
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    return true;
                }
            }
            if (y < height - 4)
            {
                bool found = true;
                for (int i = y + 1; i < y + 5; ++i)
                {
                    deleteCandidates.Add(new Vector2Int(x, i));
                    if (GetPieceColor(x, i) != color)
                    {
                        deleteCandidates.Clear();
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    return true;
                }
            }
            return false;
        }

        private bool CheckBomb(int x, int y)
        {
            // T or L shape
            int c1 = GetPieceColor(x, y);
            if(c1==-1) return false;
            if (x < width - 2)
            {
                int c2 = GetPieceColor(x + 1, y);
                int c3 = GetPieceColor(x + 2, y);
                if (c1 == c2 && c1 == c3)
                {
                    if (CheckTriple(x, y, false))
                    {
                        deleteCandidates.Add(new Vector2Int(x + 1, y));
                        deleteCandidates.Add(new Vector2Int(x + 2, y));
                        return true;
                    }
                    if (CheckTriple(x + 1, y, false))
                    {
                        deleteCandidates.Add(new Vector2Int(x, y));
                        deleteCandidates.Add(new Vector2Int(x + 2, y));
                        return true;
                    }
                    if (CheckTriple(x + 2, y, false))
                    {
                        deleteCandidates.Add(new Vector2Int(x, y));
                        deleteCandidates.Add(new Vector2Int(x + 1, y));
                        return true;
                    }
                    deleteCandidates.Clear();
                    return false;
                }
            }
            if(y< height - 2)
            {
                int c2 = GetPieceColor(x, y + 1);
                int c3 = GetPieceColor(x, y + 2);
                if (c1 == c2 && c1 == c3)
                {
                    if (CheckTriple(x, y, true))
                    {
                        deleteCandidates.Add(new Vector2Int(x, y + 1));
                        deleteCandidates.Add(new Vector2Int(x, y + 2));
                        return true;
                    }
                    if (CheckTriple(x, y + 1, true))
                    {
                        deleteCandidates.Add(new Vector2Int(x, y));
                        deleteCandidates.Add(new Vector2Int(x, y + 2));
                        return true;
                    }
                    if (CheckTriple(x, y + 2, true))
                    {
                        deleteCandidates.Add(new Vector2Int(x, y));
                        deleteCandidates.Add(new Vector2Int(x, y + 1));
                        return true;
                    }
                    deleteCandidates.Clear();
                    return false;
                }
            }
            
            return false;
        }

        private bool CheckTriple(int x, int y, bool horizontal)
        {
            // check for match3 in line with center at (x,y)
            int c1, c2, c3, c4, c5;
            if (horizontal)
            {
                c1 = GetPieceColor(x, y);
                c2 = GetPieceColor(x + 1, y);
                c3 = GetPieceColor(x + 2, y);
                c4 = GetPieceColor(x - 1, y);
                c5 = GetPieceColor(x - 2, y);
            }
            else
            {
                c1 = GetPieceColor(x, y);
                c2 = GetPieceColor(x, y + 1);
                c3 = GetPieceColor(x, y + 2);
                c4 = GetPieceColor(x, y - 1);
                c5 = GetPieceColor(x, y - 2);
            }
            if (c1 == c2 && c1 == c3)
            {
                if (horizontal)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x + 1, y));
                    deleteCandidates.Add(new Vector2Int(x + 2, y));
                }
                else
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x, y + 1));
                    deleteCandidates.Add(new Vector2Int(x, y + 2));
                }
                return true;
            }
            else if (c1 == c2 && c1 == c4)
            {
                if (horizontal)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x + 1, y));
                    deleteCandidates.Add(new Vector2Int(x - 1, y));
                }
                else
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x, y + 1));
                    deleteCandidates.Add(new Vector2Int(x, y - 1));
                }
                return true;
            }
            else if (c1 == c4 && c1 == c5)
            {
                if (horizontal)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x - 1, y));
                    deleteCandidates.Add(new Vector2Int(x - 2, y));
                }
                else
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    deleteCandidates.Add(new Vector2Int(x, y - 1));
                    deleteCandidates.Add(new Vector2Int(x, y - 2));
                }
                return true;
            }
            deleteCandidates.Clear();
            return false;
        }

        private bool CheckLineSweeperH(int x,int y)
        {
            // line of 4 horizontally
            int color = GetPieceColor(x, y);
            if (color == -1)
                return false;
            if (y < height - 3)
            {
                bool found = true;
                for (int i = y + 1; i < y + 4; ++i)
                {
                    deleteCandidates.Add(new Vector2Int(x, i));
                    if (piecesData[x,i] != color)
                    {
                        deleteCandidates.Clear();
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    return true;
                }
            }
            return false;
        }

        private bool CheckLineSweeperV(int x, int y)
        {
            //check line of 4 vertically
            int color = GetPieceColor(x,y);
            if (color == -1)
                return false;
            if (x < width - 3)
            {
                bool found = true;
                for (int i = x + 1; i < x + 4; ++i)
                {
                    deleteCandidates.Add(new Vector2Int(i, y));
                    if (GetPieceColor(i, y) != color)
                    {
                        deleteCandidates.Clear();
                        found = false;
                        break;
                    }
                }
                if (found)
                {
                    deleteCandidates.Add(new Vector2Int(x, y));
                    return true;
                }
            }
            return false;
        }

        private void DestroyPiece(Piece p)
        {
            if (p != null) {
                if (!p.IsDestroying)
                {
                    switch (p.GetPieceType())
                    {
                        case Piece.ePieceType.kBomb:
                            DestroyBomb(p);
                            break;
                        case Piece.ePieceType.kColorSweeper:
                            DestroyColorSweeper(p, 0);
                            break;
                        case Piece.ePieceType.kLineH:
                            DestroyLineH(p);
                            break;
                        case Piece.ePieceType.kLineV:
                            DestroyLineV(p);
                            break;
                    }
                }
                pieces.Remove(p);
                GameObject.Destroy(p.gameObject);
                piecesData[p.pos.x, p.pos.y] = 0;
                fallData[p.pos.x, p.pos.y].SetEmpty(true, p.pos.x, p.pos.y);
            }
        }

        private void DestroyBomb(Piece bombPiece)
        {
            bombPiece.IsDestroying = true;
            int bx = bombPiece.pos.x;
            int by = bombPiece.pos.y;
            List<Vector2Int> destroyList = new List<Vector2Int>();
            for (int x = bx - 1; x <= bx + 1; ++x)
                for (int y = by - 1; y <= by + 1; ++y)
                {
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        if (x == bx && y == by) continue;
                        destroyList.Add(new Vector2Int(x, y));
                    }
                }
            DestroyPieces(destroyList);
            CreateFX(Piece.ePieceType.kBomb, bx, by);
        }

        private void DestroyColorSweeper(Piece colorSweeperPiece, int color)
        {
            colorSweeperPiece.IsDestroying = true;
            int cx = colorSweeperPiece.pos.x;
            int cy = colorSweeperPiece.pos.y;
            if(color==0 || color == 6)
            {
                color = Random.Range(1, 6);
            }
            List<Vector2Int> destroyList = new List<Vector2Int>();
            for (int x = 0; x < width; ++x)
                for (int y = 0; y < height; ++y)
                {
                    if(x==cx && y == cy) continue;
                    if (piecesData[x, y] == color)
                    {
                        destroyList.Add(new Vector2Int(x, y));
                    }
                }
            DestroyPieces(destroyList);
            CreateFX(Piece.ePieceType.kColorSweeper,cx, cy);
        }

        private void DestroyLineH(Piece lineHPiece)
        {
            lineHPiece.IsDestroying = true;
            int lx = lineHPiece.pos.x;
            int ly = lineHPiece.pos.y;
            List<Vector2Int> destroyList = new List<Vector2Int>();
            for (int x = 0; x < width; ++x)
            {
                if (x == lx) continue;
                destroyList.Add(new Vector2Int(x, ly));
            }
            DestroyPieces(destroyList);
            CreateFX(Piece.ePieceType.kLineH, lx, ly);
        }

        private void DestroyLineV(Piece lineVPiece)
        {
            lineVPiece.IsDestroying = true;
            int lx = lineVPiece.pos.x;
            int ly = lineVPiece.pos.y;
            List<Vector2Int> destroyList = new List<Vector2Int>();
            for (int y = 0; y < height; ++y)
            {
                if (y == ly) continue;
                destroyList.Add(new Vector2Int(lx, y));
            }
            DestroyPieces(destroyList);
            CreateFX(Piece.ePieceType.kLineV, lx, ly);
        }
        private void DestroyPieces(List<Vector2Int> destroyList)
        {
            foreach (var d in destroyList)
            {
                DestroyPiece(GetPiece(d.x,d.y));
            }
        }

        private void DestroyPieces(List<Piece> piecesToDestroy)
        {
            foreach (var p in piecesToDestroy)
            {
                DestroyPiece(p);
            }
        }

        private void BuildFallMap()
        {
            // Build fall path 
            for (int x = 0; x < width; ++x)
            {
                if (mapData[x, 0] == eFieldType.kGenerator)
                    fallData[x, 0].from = FallFrom.kFallGenerator;
                else
                    fallData[x, 0].from = FallFrom.kNoFall;
            }
            for (int y = 1; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    if (mapData[x, y] == eFieldType.kGenerator)
                    {
                        fallData[x, y].from = FallFrom.kFallGenerator;
                    }
                    else if (mapData[x, y] == eFieldType.kNormal)
                    {

                        if (y > 0 && fallData[x, y - 1].from != FallFrom.kNoFall)
                        {
                            fallData[x, y].from = FallFrom.kFromTop;
                        }
                        else
                        {
                            bool left = (x > 0 && fallData[x - 1, y - 1].from != FallFrom.kNoFall);
                            bool right = (x < width - 1 && fallData[x + 1, y - 1].from != FallFrom.kNoFall);
                            if (left)
                                if (right)
                                    fallData[x, y].from = FallFrom.kFromLeftRight;
                                else
                                    fallData[x, y].from = FallFrom.kFromLeft;
                            else
                                if (right)
                                fallData[x, y].from = FallFrom.kFromRight;
                            else
                                fallData[x, y].from = FallFrom.kNoFall;
                        }
                    }
                    else
                    {
                        fallData[x, y].from = FallFrom.kNoFall;
                    }
                }

        }

        public void SetEmpty(Vector2Int pos)
        {
            // Set position empty
            piecesData[pos.x, pos.y] = 0;
            fallData[pos.x, pos.y].SetEmpty(true,pos.x,pos.y);
        }

        public eFieldType GetFieldAt(int x, int y)
        {
            return mapData[x, y];
        }

        public void Fall(Vector2Int from, Vector2Int to)
        {
            if (from.x < width)
                if (from.y < height)
                    if (to.x < width)
                        if (to.y < height)
                        {
                            Piece block = GetPiece(from.x, from.y);
                            block.pos = to;
                            piecesData[to.x, to.y] = block.id;
                            piecesData[from.x, from.y] = 0;
                            if (GetField(from.x, from.y).type == eFieldType.kGenerator)
                            {
                                fallData[from.x, from.y].SetEmpty(false,from.x,from.y);
                            }
                            else
                            {
                                fallData[from.x, from.y].SetEmpty(true,from.x,from.y);
                            }


                            if (fallData[from.x, from.y].from == FallFrom.kNoFall)
                                BuildFallMap();
                        }
        }


        public void ShufflePieces()
        {
            //Shuffle on no moves
            List<Vector2Int> positions = new List<Vector2Int>();
            List<int> nums = new List<int>();
            foreach (var p in pieces)
            {
                positions.Add(new Vector2Int(p.pos.x, p.pos.y));
                nums.Add(p.id);
            }

            while (true)
            {
                tools.Shuffle(positions);
                int i = 0;

                foreach (var p in positions)
                {
                    piecesData[p.x, p.y] = nums[i];
                    i++;
                }


                if (!CheckMatchesSimple()) break;

            }

            for (int i = 0; i < positions.Count; i++)
            {
                if (positions[i].x == pieces[i].pos.x &&
                   positions[i].y == pieces[i].pos.y)
                {
                    //same position, skip
                    continue;
                }
                actionManager.moveTo(pieces[i], positions[i].x, positions[i].y, shift);

            }
            for (int i = 0; i < positions.Count; i++)
            {
                pieces[i].pos.x = positions[i].x;
                pieces[i].pos.y = positions[i].y;
            }

        }

        private bool CheckHaveMoves()
        {
            //Check for shuffle
            for (int y = 0; y < height; ++y)
                for (int x = 0; x < width; ++x)
                {
                    if (CheckTripletsHor(x, y))
                        return true;
                    if (CheckTripletsVer(x, y))
                        return true;
                }
            return false;
        }

        private bool CheckTripletsHor(int x, int y)
        {
            //check match3 in horizontal line
            if (x + 2 >= width)
            {
                return false;
            }
            int c1 = piecesData[x, y];
            int c2 = piecesData[x + 1, y];
            int c3 = piecesData[x + 2, y];
            if (c1 == 0) return false;
            if (c2 == 0) return false;
            if (c3 == 0) return false;
            if (c1 == c2)
            {
                if (y > 0)
                    if (piecesData[x + 2, y - 1] == c1)
                        return true;
                if (y < height - 1)
                    if (piecesData[x + 2, y + 1] == c1)
                        return true;
                if (x < width - 3)
                    if (piecesData[x + 3, y] == c1)
                        return true;
            }
            else if (c1 == c3)
            {
                if (y > 0)
                    if (piecesData[x + 1, y - 1] == c1)
                        return true;
                if (y < height - 1)
                    if (piecesData[x + 1, y + 1] == c1)
                        return true;
            }
            else if (c2 == c3)
            {
                if (y > 0)
                    if (piecesData[x, y - 1] == c2)
                        return true;
                if (y < height - 1)
                    if (piecesData[x, y + 1] == c2)
                        return true;
                if (x > 0)
                    if (piecesData[x - 1, y] == c2)
                        return true;
            }
            return false;
        }
        private bool CheckTripletsVer(int x, int y)
        {
            //check match3 in vertical line
            if (y + 2 >= height)
            {
                return false;
            }
            int c1 = piecesData[x, y];
            int c2 = piecesData[x, y + 1];
            int c3 = piecesData[x, y + 2];
            if (c1 == 0) return false;
            if (c2 == 0) return false;
            if (c3 == 0) return false;
            if (c1 == c2 )
            {
                if (x > 0)
                    if (piecesData[x - 1, y + 2] == c1)
                        return true;
                if (x < width - 1)
                    if (piecesData[x + 1, y + 2] == c1)
                        return true;
                if (y < height - 3)
                    if (piecesData[x, y + 3] == c1)
                        return true;
            }
            else if (c1 == c3 )
            {
                if (x > 0)
                    if (piecesData[x - 1, y + 1] == c1)
                        return true;
                if (x < width - 1)
                    if (piecesData[x + 1, y + 1] == c1)
                        return true;
            }
            else if (c2 == c3 )
            {
                if (x > 0)
                    if (piecesData[x - 1, y] == c2)
                        return true;
                if (x < width - 1)
                    if (piecesData[x + 1, y] == c2)
                        return true;
                if (y > 0)
                    if (piecesData[x, y - 1] == c2)
                        return true;
            }
            return false;
        }

        public static System.Action<Match3Puzzle> OnActionEnded;

        public void OnReduceActions()
        {
            // reduce actions lamda
            OnActionEnded += (puzzle) =>
            {
                puzzle.ReduceActions();
            };
        }
        public void AddActions()
        {
            // add actions num
            actions++;
        }

        public void ReduceActions()
        {
            // reduce actions num function
            actions--;
            if (actions == 0)
            {
                if (!CheckPieces())
                {
                    if (!CheckHaveMoves())
                    {
                        ShufflePieces();
                    }
                }
               
            }
        }

        private void CreateFX(Piece.ePieceType type, int x, int y)
        {
            //Create speical pieces destroy effects.
            //TODO: Add color sweeper fx
            switch (type)
            {
                case Piece.ePieceType.kLineH:
                    {
                        GameObject g = UnityEngine.Object.Instantiate(Resources.Load("line_fx", typeof(GameObject)), fieldsLayer.transform, false) as
                                   GameObject;
                        LineFX fx = g.GetComponent<LineFX>();
                        fx.Init(true, false);
                        g.transform.position = tools.iPos2Pos(x, y) + shift ;

                    }
                    {
                        GameObject g = UnityEngine.Object.Instantiate(Resources.Load("line_fx", typeof(GameObject)), fieldsLayer.transform, false) as
                                   GameObject;
                        LineFX fx = g.GetComponent<LineFX>();
                        fx.Init(true, true);
                        g.transform.position = tools.iPos2Pos(x, y) + shift;

                    }
                    break;
                    case Piece.ePieceType.kLineV:
                    {
                        GameObject g = UnityEngine.Object.Instantiate(Resources.Load("line_fx", typeof(GameObject)), fieldsLayer.transform, false) as
                                   GameObject;
                        LineFX fx = g.GetComponent<LineFX>();
                        fx.Init(false, false);
                        g.transform.position = tools.iPos2Pos(x, y) + shift;
                    }
                    {
                        GameObject g = UnityEngine.Object.Instantiate(Resources.Load("line_fx", typeof(GameObject)), fieldsLayer.transform, false) as
                                   GameObject;
                        LineFX fx = g.GetComponent<LineFX>();
                        fx.Init(false, true);
                        g.transform.position = tools.iPos2Pos(x, y) + shift;
                    }
                    break;
                case Piece.ePieceType.kBomb:
                    {
                        GameObject g = UnityEngine.Object.Instantiate(Resources.Load("bomb_fx", typeof(GameObject)), fieldsLayer.transform, false) as
                                   GameObject;
                        g.transform.position = tools.iPos2Pos(x, y) + shift;
                    }
                    break;
            }
        }

    }
}