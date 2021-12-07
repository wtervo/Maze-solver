using System;
using System.Collections.Generic;
using System.Text;

namespace Maze_solver
{
    class Pentti
    {
        public string _currentCoordinates { get; set; }
        public List<string> _visitedCoordinates { get; set; } = new List<string>();
        // Same as _visitedCoordinates, but does not contain backtracking movements
        public List<string> _directRouteCoordinates { get; set; } = new List<string>();
        public List<string> _foundCrossroads { get; set; } = new List<string>();
        public List<string> _directionPath { get; set; } = new List<string>();
        public List<string> _backtrackRoute { get; set; } = new List<string>();
        public List<string> _actionLog { get; set; } = new List<string>();
        public bool _exitFound { get; set; } = false;
        public int _currentTurn { get; set; } = 0;
        Maze _maze;

        public Pentti(Maze maze)
        {
            _maze = maze;
            _currentCoordinates = _maze._startCoordinates[0];
        }

        static Random rnd = new Random();

        /// <summary>
        /// Responsible for choosing where Pentti moves next
        /// </summary>
        public void Move()
        {
            var surroundings = GetSurroundings(_currentCoordinates);
            string exit = "";
            string backTrackDirection = "";
            List<string> empty = new List<string>();
            List<string> previous = new List<string>();
            List<string> wall = new List<string>();

            foreach (KeyValuePair<string, char> item  in surroundings)
            {
                if (item.Value == 'E')
                {
                    exit = item.Key;
                    // If exit is found, other coordinates do not matter
                    break;
                }
                else if (item.Value == ' ')
                {
                    empty.Add(item.Key);
                }
                else if (item.Value == '.')
                {
                    previous.Add(item.Key);
                }
                else if (item.Value == '#')
                {
                    wall.Add(item.Key);
                }
            }

            if (wall.Count == 4) throw new Exception("ERROR: Pentti cannot move, because he is surrounded by walls!");

            if (!_directRouteCoordinates.Contains(_currentCoordinates) && _backtrackRoute.Count == 0) _directRouteCoordinates.Add(_currentCoordinates);
            _visitedCoordinates.Add(_currentCoordinates);

            // Movement prioritized thus: exit > backtracking > unvisited space > previously visited space
            // If movement has multiple options of the same priority, path is chosen randomly
            // Walls are ignored completely as movement options
            if (exit != "")
            {
                _exitFound = true;
                UpdateMovement(exit, true);
            }
            else if (empty.Count > 0)
            {
                var index = rnd.Next(empty.Count);

                if (empty.Count > 1 && !_foundCrossroads.Contains(_currentCoordinates))
                {
                    // Pentti will remember all crossroads he has been to in case he needs to backtrack
                    _foundCrossroads.Add(_currentCoordinates);
                }
                else if (empty.Count == 1 && _foundCrossroads.Contains(_currentCoordinates))
                {
                    // If pentti is about to check the last available route of a crossroad
                    // he does not need to remember it any longer
                    _foundCrossroads.Remove(_currentCoordinates);
                }
                UpdateMovement(empty[index]);
            }
            else
            {
                // This is a check to make sure _foundCrossroads is updated properly
                // Sometimes situations can rise where Pentti enters a coordinate included in _foundCrossroads,
                // but still founds himself surrounded by walls/previously visited locations and this would lead to a crash
                if (_foundCrossroads.Contains(_currentCoordinates) && wall.Count + previous.Count == 4)
                {
                    _foundCrossroads.Remove(_currentCoordinates);
                }

                if (_backtrackRoute.Count == 0) Backtrack();

                // Find the direction where backtracking is happening
                foreach (string coord in previous)
                {
                    var splitCoord = coord.Split(":");
                    if (splitCoord[1] == _backtrackRoute[^1]) backTrackDirection = ReverseDirection(splitCoord[0]);
                }

                var backTrackCoord = backTrackDirection + ":" + _backtrackRoute[^1];

                UpdateMovement(backTrackCoord);
                // Remove backtracked coordinates from appropriate lists
                _backtrackRoute.RemoveAt(_backtrackRoute.Count - 1);
                _directRouteCoordinates.RemoveAt(_directRouteCoordinates.Count - 1);
            }
        }

        /// <summary>
        /// Updates various data in preparation for Pentti's next movement
        /// </summary>
        /// <param name="nextCoordinate"></param>
        /// <param name="exitFound"></param>
        private void UpdateMovement(string nextCoordinate, bool exitFound = false)
        {
            var splitNextCoord = nextCoordinate.Split(":");
            _directionPath.Add(splitNextCoord[0]);
            _currentTurn += 1;
            AddLog(splitNextCoord[1], splitNextCoord[0]);
            _currentCoordinates = splitNextCoord[1];
            if (exitFound) _visitedCoordinates.Add(_currentCoordinates); 
        }

        /// <summary>
        /// Flips directional values. Used while backtracking
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        private string ReverseDirection(string direction)
        {
            var newDirection = "";
            if (direction == "N") newDirection = "S";
            if (direction == "S") newDirection = "N";
            if (direction == "W") newDirection = "E";
            if (direction == "E") newDirection = "W";

            return newDirection;
        }

        /// <summary>
        /// Pentti is a (relatively) smart boy and can trace back his steps,
        /// if he finds himself in a situation where he is surrounded by
        /// walls or places he has been to before
        /// </summary>
        private void Backtrack()
        {
            var lastSeenCrossroad = _foundCrossroads[^1];
            var index = _directRouteCoordinates.LastIndexOf(lastSeenCrossroad);
            _backtrackRoute = _directRouteCoordinates.GetRange(index, _directRouteCoordinates.Count - index - 1);
        }

        /// <summary>
        /// Check the surrounding coordinates and their elements
        /// </summary>
        /// <param name="currentCoords"></param>
        /// <returns></returns>
        public Dictionary<string, char> GetSurroundings(string currentCoords)
        {
            var surroundings = new Dictionary<string, char>();
            var surroundingCoordinates = SurroundingCoords(currentCoords);
            var count = 0;

            foreach (int[] surrCoord in surroundingCoordinates)
            {
                count += 1;
                char character;
                string strSurrCoord;
                if (surrCoord.Length == 0)
                {
                    character = '#';
                    // A dummy value that should never be used
                    strSurrCoord = "00-00";
                }
                else
                {
                    character = CoordChar(surrCoord);
                    strSurrCoord = _maze.Coords(surrCoord[0], surrCoord[1]);
                }

                // Previously visited coordinate is represented by '.' character
                if (_visitedCoordinates.Contains(strSurrCoord)) character = '.';

                //Order to add is N, S, W, E
                // Surroundings in a format like <N:09-14, #>
                // IE. northern coordinate X:9, Y:14 has the symbol # in it
                if (count == 1) surroundings.Add("N:" + strSurrCoord, character);
                if (count == 2) surroundings.Add("S:" + strSurrCoord, character);
                if (count == 3) surroundings.Add("W:" + strSurrCoord, character);
                if (count == 4) surroundings.Add("E:" + strSurrCoord, character);
            }

            return surroundings;
        }

        /// <summary>
        /// Get surrounding coordinates based on Pentti's current location
        /// </summary>
        /// <param name="currentCoords"></param>
        /// <returns></returns>
        private List<int[]> SurroundingCoords(string currentCoords)
        {
            int[] intCoords = _maze.ReverseCoords(currentCoords);
            var x = intCoords[0];
            var y = intCoords[1];
            // Northern, southern, western and eastern coordinates from current location
            var nCoords = new int[] { x, y - 1 };
            var sCoords = new int[] { x, y + 1 };
            var wCoords = new int[] { x - 1, y };
            var eCoords = new int[] { x + 1, y };

            // Check if any of the neighbouring coordinates are outside maze borders
            // This situation can only happen if the start position is on the border
            if (nCoords[1] == 0) nCoords = new int[] { };
            if (sCoords[1] > _maze._mazeHeight) sCoords = new int[] { };
            if (wCoords[0] == 0) wCoords = new int[] { };
            if (eCoords[0] > _maze._mazeWidth) eCoords = new int[] { };

            return new List<int[]> { nCoords, sCoords, wCoords, eCoords };
        }

        private char CoordChar(int[] arrCoords)
        {
            return _maze._mazeLineList[arrCoords[1] - 1][arrCoords[0] - 1];
        }

        /// <summary>
        /// Writes a log of Pentti's movement
        /// </summary>
        /// <param name="target"></param>
        /// <param name="directionChar"></param>
        private void AddLog(string target, string directionChar)
        {
            string direction = "";
            if (directionChar == "N") direction = "north";
            if (directionChar == "S") direction = "south";
            if (directionChar == "W") direction = "west";
            if (directionChar == "E") direction = "east";

            string verb = (_backtrackRoute.Count > 0) ? "Backtracked" : "Moved";
            string logString;

            if (_exitFound)
            {
                logString = "Turn " + _currentTurn + ": Moved " + direction + " to the exit at coordinate " + target + ". Pentti has escaped the maze!";
            }
            else
            {
                logString = "Turn " + _currentTurn + ": " + verb + " " + direction + " from " + _currentCoordinates + " to " + target;
            }
            _actionLog.Add(logString);
        }
    }
}
