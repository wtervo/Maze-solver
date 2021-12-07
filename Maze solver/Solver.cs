using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Maze_solver
{
    class Solver
    {
        Dictionary<string, string> _settings;
        Maze _maze;
        Pentti _pena;
        string _chosenMazeFile;
        string _chosenMazeFileName;
        int _chosenMovementLimit;
        int[] _movementLimits = new int[] { 20, 150, 200 };
        public List<string> _unsolvableMazeLineList { get; set; } = new List<string>();
        public List<List<string>> _solutionCoordinates { get; set; } = new List<List<string>>();
        int _movementRangeMaxLimit;
        int _movementRangeMinLimit;

        /// <summary>
        /// The main runtime logic of the program 
        /// </summary>
        public void ExecuteSolver()
        {
            _settings = GetSettings();
            IntroText();
            MazeSelection();
            _maze = new Maze(_chosenMazeFile, _chosenMazeFileName, _settings);
            _pena = new Pentti(_maze);
            var userCanSelectTurnLimit = Boolean.Parse(_settings["USERSELECTTURNLIMIT"]);

            if (userCanSelectTurnLimit)
            {
                MovementLimitSelection();
            }
            SolvingProperties(userCanSelectTurnLimit);

            var solvableMaze = IsMazeSolvable();
            // If maze is not solvable, no need to print its data multiple times
            if (!solvableMaze) _movementLimits = new int[] { _movementLimits[0] };

            var resultList = new List<string>();
            resultList.AddRange(AttemptInfo());
            resultList.AddRange(EmptyLines(4));

            foreach (int movementLimit in _movementLimits)
            {
                if (solvableMaze)
                {
                    // Could not get optimal route finding to work in time
                    // SearchForOptimalRoute();
                    _chosenMovementLimit = movementLimit;

                    // New instances of maze and Pentti between each try
                    _maze = new Maze(_chosenMazeFile, _chosenMazeFileName, _settings);
                    _pena = new Pentti(_maze);

                    // Keep moving Pentti until exit is found or turn limit is filled
                    while (!_pena._exitFound)
                    {
                        if (_pena._currentTurn == _chosenMovementLimit) break;
                        _pena.Move();
                    }
                    resultList.AddRange(AttemptResult(solvableMaze));
                    resultList.AddRange(SolvableMaze());
                }
                else
                {
                    resultList.AddRange(AttemptResult(solvableMaze));
                    resultList.AddRange(UnsolvableMaze());
                }
            }

            var createdFileName = CreateResultFile(resultList);

            Console.WriteLine("");
            Console.WriteLine("~~~~++++====++++~~~~");
            Console.WriteLine("DONE! Did Pentti make it or not? Results can be seen in " + GetDirectory() + "\\mazes\\results\\" + createdFileName);
            Console.WriteLine("~~~~++++====++++~~~~");
            Console.WriteLine("");
            Console.WriteLine("Press ENTER to close the program");
            Console.ReadLine();
        }

        /// <summary>
        /// Get global settings from settings.txt
        /// </summary>
        /// <returns></returns>
        private Dictionary<string, string> GetSettings()
        {
            var settings = File.ReadAllLines(GetDirectory() + "\\settings.txt")
                .Select(l => l.Split(new[] { '=' }))
                .ToDictionary(s => s[0].Trim(), s => s[1].Trim());

            _movementRangeMaxLimit = Int32.Parse(settings["MOVEMENTRANGEMAXLIMIT"]);
            _movementRangeMinLimit = Int32.Parse(settings["MOVEMENTRANGEMINLIMIT"]);

            return settings;
        }

        private string GetDirectory()
        {
            var dirPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            int index = dirPath.LastIndexOf("\\Maze solver") + "\\Maze solver".Length;

            return dirPath.Substring(0, index);
        }

        /// <summary>
        /// Lets user the maze to solve
        /// </summary>
        private void MazeSelection()
        {
            string mazeDir = GetDirectory() + "\\mazes";
            var txtFileDict = new Dictionary<int, string>();
            int txtFileCount = 0;
            int fileInputInteger;


            if (!Directory.Exists(mazeDir))
            {
                throw new Exception("\\mazes directory not found");
            }

            string[] fileArray = Directory.GetFiles(mazeDir);

            if (fileArray.Length == 0)
            {
                throw new Exception("\\mazes directory does not contain any files");
            }

            foreach (string file in fileArray)
            {
                // Only accept TXT files to be listed
                string[] splitFileName = file.Split(".");
                if (splitFileName[1].ToLower() == "txt")
                {
                    txtFileCount += 1;
                    txtFileDict.Add(txtFileCount, file);
                }
            }

            if (txtFileCount == 0)
            {
                throw new Exception("\\mazes directory does not contain any TXT files");
            }

            // Skip listing the mazes and asking for user input if there is only one maze in the folder
            if (txtFileCount > 1)
            {
                Console.WriteLine("List of available mazes:");
                Console.WriteLine("");

                foreach (KeyValuePair<int, string> txtFile in txtFileDict)
                {
                    // When path is split with "\", the last value in the array will be the filename
                    string[] splitTxtFileName = txtFile.Value.Split("\\");
                    Console.WriteLine("(" + txtFile.Key + ") - " + splitTxtFileName[^1]);
                }

                Console.WriteLine("Select which maze you would like to solve with a number from 1-" + txtFileCount + ":");
                string fileInput = Console.ReadLine();

                while (true)
                {
                    if (!int.TryParse(fileInput, out _))
                    {
                        Console.WriteLine("This is not a valid integer");
                        fileInput = Console.ReadLine();
                        continue;
                    }

                    fileInputInteger = Int32.Parse(fileInput);

                    if (fileInputInteger > txtFileCount || fileInputInteger < 1)
                    {
                        Console.WriteLine("The integer value must be in the range of 1-" + txtFileCount);
                        fileInput = Console.ReadLine();
                        continue;
                    }

                    break;
                }

                _chosenMazeFile = txtFileDict[fileInputInteger];
            }
            else
            {
                _chosenMazeFile = txtFileDict[1];
            }

            string[] splitChosenFileName = _chosenMazeFile.Split("\\");
            _chosenMazeFileName = splitChosenFileName[^1];
            Console.WriteLine("");
            Console.WriteLine("You have chosen the maze \"" + _chosenMazeFileName + "\"");
        }

        /// <summary>
        /// Lets user choose turn limit. Off by default. Can be enabled by modifying settings.txt
        /// </summary>
        private void MovementLimitSelection()
        {
            int movementInputInteger;

            Console.WriteLine("");
            Console.WriteLine("Next, select what is the maximum number of moves Pentti can make while in the maze");
            Console.WriteLine("This value can be in the range of " + _movementRangeMinLimit + "-" + _movementRangeMaxLimit);
            Console.WriteLine("Choose the maximum number of moves: ");

            string movementInput = Console.ReadLine();

            while (true)
            {
                if (!int.TryParse(movementInput, out _))
                {
                    Console.WriteLine("This is not a valid integer");
                    movementInput = Console.ReadLine();
                    continue;
                }

                movementInputInteger = Int32.Parse(movementInput);

                if (movementInputInteger > _movementRangeMaxLimit || movementInputInteger < _movementRangeMinLimit)
                {
                    Console.WriteLine("The integer value must be in the range of " + _movementRangeMinLimit + "-" + _movementRangeMaxLimit);
                    movementInput = Console.ReadLine();
                    continue;
                }

                break;
            }

            _movementLimits = new int[] { movementInputInteger };
        }

        /// <summary>
        /// Basic information about to program
        /// </summary>
        private void IntroText()
        {
            Console.WriteLine("");
            Console.WriteLine("Welcome to the maze solver program!");
            Console.WriteLine("");
            Console.WriteLine("Poor old Pentti has got himself lost in a maze once again. Can he escape or not?");
            Console.WriteLine("Successful escape requires navigation skills, time and luck from Pentti. Mostly luck...");
            Console.WriteLine("");
            Console.WriteLine("Insert maze file(s) in TXT format to the \\mazes folder and this program will attempt to solve them and thus save Pentti from a grueling fate");
            Console.WriteLine("");
            Console.WriteLine("Check README.txt for more information!");
            Console.WriteLine("");
            Console.WriteLine("");
        }

        /// <summary>
        /// Displays information to the user about the upcoming solving attempt(s)
        /// </summary>
        /// <param name="userCanSelectTurnLimit"></param>
        private void SolvingProperties(bool userCanSelectTurnLimit)
        {
            var startCoordsList = _maze._startCoordinates;
            var startCoords = _maze.ReverseCoords(startCoordsList[0]);
            var exitCoordsList = _maze._exitCoordinates;
            var exitsString = "";
            var exit = 0;
            foreach (string coord in exitCoordsList)
            {
                var endCoords = _maze.ReverseCoords(exitCoordsList[exit]);
                exitsString += "[" + endCoords[0] + ", " + endCoords[1] + "]";
                if (exit != exitCoordsList.Count - 1)
                {
                    exitsString += ", ";
                }
                exit += 1;
            }

            Console.WriteLine("");
            if (userCanSelectTurnLimit)
            {
                Console.WriteLine("The program will now attempt to solve the maze \"" + _chosenMazeFileName + "\", which has the following properties:");
            }
            else
            {
                Console.WriteLine("The program will now attempt to solve the maze \"" + _chosenMazeFileName + "\" three times, which has the following properties:");
            }
            Console.WriteLine("");
            Console.WriteLine("Height: " + _maze._mazeHeight + ", Width: " + _maze._mazeWidth);
            Console.WriteLine("Start coordinates at [" + startCoords[0] + ", " + startCoords[1] + "]");
            Console.WriteLine("A total of " + exitCoordsList.Count + " exit(s) at coordinates " + exitsString);
            Console.WriteLine("");
            if (userCanSelectTurnLimit)
            {
                Console.WriteLine("The program will allow Pentti a maximum " + _movementLimits[0] + " movement turns before terminating");
            }
            else
            {
                Console.WriteLine("The program will allow Pentti the following maximum turns:");
                Console.WriteLine(_movementLimits[0] + " turns on the first attempt");
                Console.WriteLine(_movementLimits[1] + " turns on the second attempt");
                Console.WriteLine(_movementLimits[2] + " turns of the third attempt");
            }
            Console.WriteLine("");
            Console.WriteLine("Press ENTER to proceed with the solving");
            Console.ReadLine();
        }

        /// <summary>
        /// Checks if maze has a solution or not
        /// </summary>
        /// <returns></returns>
        private bool IsMazeSolvable()
        {
            var mazeSearchList = _maze._mazeLineList;

            var exitFound = SearchForExitRoute(_maze._startCoordinates[0], ref mazeSearchList);

            // Restore the original maze
            _maze = new Maze(_chosenMazeFile, _chosenMazeFileName, _settings);

            if (!exitFound) _unsolvableMazeLineList.AddRange(mazeSearchList);

            return exitFound;
        }

        /// <summary>
        /// Search for exits recursively by marking coordinates it has visited before
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="mazeSearchList"></param>
        /// <returns></returns>
        private bool SearchForExitRoute(string coordinate, ref List<string> mazeSearchList)
        {
            var surroundings = _pena.GetSurroundings(coordinate);
            var resultList = new List<bool>();
            var count = 0;
            foreach (KeyValuePair<string, char> surroundInfo in surroundings)
            {
                var surroundCoordinate = surroundInfo.Key.Split(":")[1];
                if (surroundInfo.Value == 'E')
                {
                    resultList.Add(true);
                }
                else if (surroundInfo.Value == ' ')
                {
                    int[] intCoord = _maze.ReverseCoords(coordinate);
                    // "Color" the current coordinate with a different symbol. Otherwise program will enter an infinite loop
                    _maze.EditLine(ref mazeSearchList, intCoord[0], intCoord[1], "+");
                    resultList.Add(SearchForExitRoute(surroundCoordinate, ref mazeSearchList));
                }
                else
                {
                    count += 1;
                    // "Color" current coordinate if it is surrounded by walls or previously checked locations on all 4 sides
                    if (count == 4)
                    {
                        int[] intCoord = _maze.ReverseCoords(coordinate);
                        _maze.EditLine(ref mazeSearchList, intCoord[0], intCoord[1], "+");
                    }
                }
            }
            return resultList.Contains(true);
        }

        /// <summary>
        /// Search for optimal value through recursion. Does not work.
        /// Leaving this here in case I feel like returning to the problem.
        /// </summary>
        private void SearchForOptimalRoute()
        {
            var mazeSearchList = _maze._mazeLineList;
            var exitCoordinates = _maze._exitCoordinates;

            foreach (string exit in exitCoordinates)
            {
                var list = SearchOptimalRecursion(exit, new List<string>(), ref mazeSearchList);
                _solutionCoordinates.Add(list);

                // Restore the original maze
                _maze = new Maze(_chosenMazeFile, _chosenMazeFileName, _settings);
            }
        }

        /// <summary>
        /// Attempt to find the optimal path similar to how SearchForExitRoute works, but in reverse from goal to start
        /// Does not work currently and is not part of runtime
        /// </summary>
        /// <param name="coordinate"></param>
        /// <param name="coordRoute"></param>
        /// <param name="mazeSearchList"></param>
        /// <returns></returns>
        private List<string> SearchOptimalRecursion(string coordinate, List<string> coordRoute, ref List<string> mazeSearchList)
        {
            var surroundings = _pena.GetSurroundings(coordinate);
            var resultList = new List<List<string>>();
            var count = 0;
            foreach (KeyValuePair<string, char> surroundInfo in surroundings)
            {
                var surroundCoordinate = surroundInfo.Key.Split(":")[1];
                if (surroundInfo.Value == '^')
                {
                    coordRoute.Add(coordinate);
                    resultList.Add(coordRoute);
                }
                else if (surroundInfo.Value == ' ')
                {
                    int[] intCoord = _maze.ReverseCoords(coordinate);
                    // "Color" the current coordinate with a different symbol. Otherwise program will enter an infinite loop
                    _maze.EditLine(ref mazeSearchList, intCoord[0], intCoord[1], "+");
                    coordRoute.Add(coordinate);
                    resultList.Add(SearchOptimalRecursion(surroundCoordinate, coordRoute, ref mazeSearchList));
                }
                else
                {
                    count += 1;
                    if (count == 4)
                    {
                        int[] intCoord = _maze.ReverseCoords(coordinate);
                        _maze.EditLine(ref mazeSearchList, intCoord[0], intCoord[1], "+");
                        resultList.Add(new List<string>());
                    }
                }
            }

            return resultList.OrderByDescending(m => m.Count()).First();
        }

        /// <summary>
        /// Creates the result file in which program's results can be seen
        /// </summary>
        /// <param name="resultList"></param>
        /// <returns></returns>
        private string CreateResultFile(List<string> resultList)
        {
            string attemptDir = GetDirectory() + "\\mazes\\results";
            var attemptFilePath = attemptDir + "\\result-" + _maze._mazeName;
            var indentedResultList = new List<string>();
            var indent = "";

            while (indent.Length <= 5) indent += " ";

            if (!Directory.Exists(attemptDir))
            {
                Directory.CreateDirectory(attemptDir);
            }

            // Add indent to the file to make it slightly more pleasant to read
            foreach (string line in resultList)
            {
                indentedResultList.Add(indent + line);
            }

            File.WriteAllLines(attemptFilePath, indentedResultList);

            return attemptFilePath.Split("\\")[^1];
        }

        /// <summary>
        /// Result file content is different depending on if the maze can be solved
        /// This method is returns information about solvable mazes
        /// </summary>
        /// <returns></returns>
        private List<string> SolvableMaze()
        {
            var solvableList = new List<string>();
            var mazeLines = _maze.DrawPath(_pena._visitedCoordinates);

            solvableList.AddRange(mazeLines);
            solvableList.Add("Maximum turns to escape: " + _chosenMovementLimit);
            solvableList.AddRange(MazeLegend());
            solvableList.AddRange(EmptyLines(6));
            solvableList.AddRange(SurroundString("ACTIONS DONE BY PENTTI EACH TURN", '*'));
            solvableList.Add("");
            solvableList.AddRange(_pena._actionLog);
            solvableList.AddRange(EmptyLines(10));

            return solvableList;
        }

        /// <summary>
        /// Result file content is different depending on if the maze can be solved
        /// This method is returns information about unsolvable mazes
        /// </summary>
        /// <returns></returns>
        private List<string> UnsolvableMaze()
        {
            var unsolvableList = new List<string>();

            unsolvableList.AddRange(_unsolvableMazeLineList);
            unsolvableList.AddRange(UnsolvableMazeLegend());

            return unsolvableList;
        }

        /// <summary>
        /// General info about the attempt
        /// </summary>
        /// <returns></returns>
        private List<string> AttemptInfo()
        {
            var infoList = new List<string>();
            string exitsString = "Exit(s) located at ";
            int count = 1;

            foreach (string exit in _maze._exitCoordinates)
            {
                exitsString += exit;
                if (count != _maze._exitCoordinates.Count) exitsString += ", ";
                count += 1;
            }

            infoList.Add("");
            infoList.Add("");
            infoList.AddRange(SurroundString("MAZE SOLVER RESULTS", '*'));
            infoList.Add("");
            infoList.Add("Attempt runtime: " + DateTime.Now.ToString());
            infoList.Add("Maze name: " + _maze._mazeName);
            infoList.Add("Dimensions: Height - " + _maze._mazeHeight + ", Width - " + _maze._mazeWidth);
            infoList.Add("Start location at " + _maze._startCoordinates[0]);
            infoList.Add(exitsString);
            infoList.Add("");

            return infoList;
        }

        /// <summary>
        /// End result of the attempt
        /// </summary>
        /// <param name="solvable"></param>
        /// <returns></returns>
        private List<string> AttemptResult(bool solvable)
        {
            var resultList = new List<string>();
            string resultString;

            if (_pena._exitFound)
            {
                resultString = "SUCCESS! Pentti escaped from the maze on turn " + _pena._currentTurn + " and is now free to continue his meaningless existence as he pleases!";
            }
            else if (!solvable)
            {
                resultString = "UNSOLVABLE! This maze does not have a solution. Thus, Pentti is doomed to wander its empty halls until his inevitable death!";
            }
            else
            {
                resultString = "FAILURE! Pentti could not escape the maze in " + _chosenMovementLimit + " turns and has died a slow and agonizing death!";
            }

            resultList.AddRange(SurroundString(resultString, '~'));
            resultList.Add("");

            return resultList;
        }

        /// <summary>
        /// Empty lines to the result file
        /// </summary>
        /// <param name="amount"></param>
        /// <returns></returns>
        private List<string> EmptyLines(int amount)
        {
            var emptyLineList = new List<string>();

            int count = 1;

            while(amount > count)
            {
                emptyLineList.Add("");
                count += 1;
            }

            return emptyLineList;
        }

        /// <summary>
        /// Map information for solvable mazes
        /// </summary>
        /// <returns></returns>
        private List<string> MazeLegend()
        {
            var legendList = new List<string>();

            legendList.Add("");
            legendList.Add("");
            legendList.Add("Legend:");
            legendList.Add("X = End of movement       O = Starting point (can be overwritten if Pentti moves through it)");
            legendList.Add("# = Wall                  Numbers 1-9 = Order of movement");
            legendList.Add("Numbers start from 1, grow by 1 every time Pentti starts a new path after backtracking. Loop back to 1 after 9.");
            legendList.Add("");
            legendList.Add("");

            return legendList;
        }

        /// <summary>
        /// Map information for unsolvable mazes
        /// </summary>
        /// <returns></returns>
        private List<string> UnsolvableMazeLegend()
        {
            var legendList = new List<string>();

            legendList.Add("");
            legendList.Add("");
            legendList.Add("Legend:");
            legendList.Add("E = Exit       ^ = Starting point");
            legendList.Add("# = Wall       + = Space connected to the starting point");

            return legendList;
        }

        /// <summary>
        /// Adds fancy character lines around a string in the result file
        /// </summary>
        /// <param name="str"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        private List<string> SurroundString(string str, char c)
        {
            var surroundList = new List<string>();
            var symbolString = "";
            var count = 1;

            while (str.Length >= count)
            {
                symbolString += c;
                count += 1;
            }

            surroundList.Add(symbolString);
            surroundList.Add(str);
            surroundList.Add(symbolString);

            return surroundList;
        }
    }
}
