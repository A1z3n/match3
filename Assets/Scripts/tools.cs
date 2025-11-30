using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;


namespace Match3
{
    static class tools {
        public static Vector3 iPos2PosB(int x, int y) {
            return new Vector3(x, -y, 0.0f);
        }

        public static Vector3 iPos2Pos(int x, int y) {
            return new Vector3(0.5f + x, -0.5f - y,0.0f);
        }

        public static int RoundPosX(float x) {
             return (int)Math.Round(x-0.5f);
        }

        public static int RoundPosY(float y) {
            //pos.y = -0.32f - y * 0.64f;
            return (int)Math.Round(-0.5f - y);
        }

        public static int CoordsToPosX(float x) {
            return RoundPosX(x *3.125f/ 32.0f);
        }

        public static int CoordsToPosY(float y) {
            return -RoundPosY( y * 3.125f / 32.0f);
        }
        public static int GetDirection(Vector2Int pos) {
            
            double ang = 0.0;
            if(pos.x!=0){
                ang = Math.Atan2(pos.y, pos.x) * 180 / Math.PI;
            }
            else if (pos.y > 0 ) {
               ang = 90;
            }
            else {
                ang = 270;
            }
            return (int)(ang/22.5f);
        }

        public static Vector2Int GetNearestFromRect(Vector2Int from, RectInt rect) {
            Dictionary<Vector2Int, float> distances = new Dictionary<Vector2Int, float>();
            for (int x = rect.x; x < rect.x + rect.width;x++) {
                for (int y = rect.y; y < rect.y + rect.height; y++)
                {
                    Vector2Int dist = new Vector2Int(x,y);
                    distances[dist] = Vector2Int.Distance(dist, from);
                    
                }
            }

            float distMin = 9999999.0f;
            Vector2Int result = new Vector2Int();
            foreach (var d in distances) {
                if (d.Value < distMin) {
                    distMin = d.Value;
                    result = d.Key;
                }
            }
            return result;
        }

      

        public static int EncodeInt(List<int> value) {
            int result = 0;
            foreach (var v in value) {
                result += v;
                result += 10;
            }
            return result;
        }

        public static List<int> DecodeInt(int value) {
            List<int> result = new List<int>();
            int count = value / 10;
            return result;
        }


        public static void Shuffle<T>(this List<T> list)
        {
            System.Random rng = new System.Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }
}
