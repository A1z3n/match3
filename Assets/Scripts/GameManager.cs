
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Match3 { 
    public class GameManager
    {
        private static readonly GameManager instance = new GameManager();

        private Level currentLevel;
        private string currentLevelName;
        private int currentLevelId = 0;
        private Match3Puzzle puzzle;

        public void SetMatch3Puzzle(Match3Puzzle p)
        {
            puzzle = p;
        }

        public Match3Puzzle GetMatch3Puzzle()
        {
            return puzzle;
        }

        Dictionary<int,string> levelNames = new Dictionary<int, string>();

        private GameManager()
        {
            // level list
            levelNames[0] = "level01";
            levelNames[1] = "level02";
            levelNames[2] = "level03";
        }


        public static GameManager GetInstance()
        {
            return instance;
        }

        public void RestartLevel()
        {
            LoadLevel(currentLevelName);
        }

        public void Load()
        {
            // Load level
            currentLevelId = PlayerPrefs.GetInt("level", 0);
            currentLevelName = levelNames[currentLevelId];
            LoadLevel(currentLevelName);

        }
        void LoadLevel(string levelName)
        {

            // Load level
            currentLevel = new Level();
            currentLevel.LoadLevel(levelName);
        }

        public void WinLevel()
        {
            //Win level
            currentLevelId++;
            if (currentLevelId >= levelNames.Count())
            {
                currentLevelId = 0;
            }
            currentLevelName = levelNames[currentLevelId];
            PlayerPrefs.SetInt("level", currentLevelId);
            LoadLevel(currentLevelName);

        }
    }
}
