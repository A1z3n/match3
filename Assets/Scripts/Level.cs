using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TiledCS;
using UnityEngine;

namespace Match3
{
    public class Level
    {
        private GameObject fieldsLayer;
        private int mapWidth;
        private int mapHeight;
        private eFieldType[,] mapData;
        Match3Puzzle match3;

        public Level()
        {
            fieldsLayer = GameObject.Find("fields");
            match3 = new Match3Puzzle();
        }
        public void LoadLevel(string name)
        {
            ParseTMX(name);
            Vector3 center = new Vector3(-mapWidth * 0.5f, mapHeight * 0.5f, 0);
            match3.Init(mapData, center);
            match3.FillPieces();
            fieldsLayer.transform.position = center;
        }

        private void ParseTMX(string name)
        {
            var map = new TiledMap("Assets/Levels/" + name + ".tmx");
            var tileset = new TiledTileset("Assets/Levels/assets_candy.tsx");
            var myLayer = map.Layers.First(l => l.name == "bg");

            int x = 0;
            int y = 0;

            mapWidth = map.Width;
            mapHeight = map.Height;
            mapData = new eFieldType[mapWidth, mapHeight];

            foreach (var p in myLayer.data)
            {
                mapData[x, y] = eFieldType.kEmpty;
                if (p == 36)
                {
                    mapData[x, y] = eFieldType.kNormal;
                }
                else if (p == 37)
                {
                    mapData[x, y] = eFieldType.kGenerator;
                }
                x++;
                if (x >= mapWidth)
                {
                    x = 0; y++;
                }
            }
        }
    }
}
