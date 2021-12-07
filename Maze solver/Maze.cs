using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Maze_solver
{
    public class Maze
    {
        public string _mazeName { get; set; }
        int _drawCharacter = 1;
        public List<string> _mazeLineList { get; set; } = new List<string>();
        public Dictionary<string, char> _mazeCharacters { get; set; } = new Dictionary<string, char>();
        public int _mazeHeight { get; set; }
        public int _mazeWidth { get; set; }
        public List<string> _startCoordinates { get; set; } = new List<string>();
        public List<string> _exitCoordinates { get; set; } = new List<string>();
        int _maximumExits;
        int _maxHeight;
        int _maxWidth;
        int _minHeight;
        int _minWidth;


        /// <summary>
        /// The maze is read and its validity checked immediately after
        /// 
        /// Maze coordinates defined are as such: a string of length 5 xx-yy where x is the line and y the character on that line
        /// IE. the third character of the first (valid) row is represented by coordinate 01-03
        /// </summary>
        /// <param name="mazeFilePath"></param>
        public Maze(string mazeFilePath, string mazeName, Dictionary<string, string> settings)
        {
            _mazeName = mazeName;
            _maximumExits = Int32.Parse(settings["MAZEMAXEXITS"]);
            _maxHeight = Int32.Parse(settings["MAZEMAXHEIGHT"]);
            _maxWidth = Int32.Parse(settings["MAZEMAXWIDTH"]);
            _minHeight = Int32.Parse(settings["MAZEMINHEIGHT"]);
            _minWidth = Int32.Parse(settings["MAZEMINWIDTH"]);
            ReadMazeFile(mazeFilePath);
        }

        /// <summary>
        /// Reads maze from file and checks its validity
        /// </summary>
        /// <param name="mazeFilePath"></param>
        private void ReadMazeFile(string mazeFilePath)
        {
            string[] mazeLineArray = File.ReadAllLines(mazeFilePath);
            int lineCount = 0;

            foreach (string line in mazeLineArray)
            {
                // Trim accidental empty spaces / empty rows
                string trimmedLine = line.Trim();

                // Ignore empty lines, no matter where in the file they are
                if (trimmedLine == "") continue;

                lineCount += 1;

                CheckMazeLine(trimmedLine, lineCount);

                _mazeLineList.Add(trimmedLine);
            }
            _mazeHeight = lineCount;

            if (_mazeHeight > _maxHeight || _mazeHeight < _minHeight)
            {
                throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze's height needs to be in range of " + _minHeight + "-" + _maxHeight + " (was " + _mazeHeight + ")");
            }

            if (_startCoordinates.Count == 0)
            {
                throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze does not contain a starting point");
            }
            else if (_exitCoordinates.Count == 0)
            {
                throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze does not contain any exits");
            }

            // Last line needs to be checked again incase it contains empty spaces
            CheckMazeLine(_mazeLineList[^1], lineCount, true);
        }

        /// <summary>
        /// Check each line of the maze and their validities
        /// </summary>
        /// <param name="line"></param>
        /// <param name="lineNum"></param>
        /// <param name="isLastLine"></param>
        private void CheckMazeLine(string line, int lineNum, bool isLastLine = false)
        {
            char[] allowedCharacters = { ' ', '#', '^', 'E' };
            // Empty spaces are not allowed in border coordinates
            char[] allowedBorderCharacters = { '#', '^', 'E' };
            int charCount = 0;


            if (lineNum == 1)
            {
                if (line.Length > _maxWidth || line.Length < _minWidth)
                {
                    throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze's width needs to be in range of " + _minWidth + "-" + _maxWidth + " (was " + line.Length + ")");
                }
                _mazeWidth = line.Length;
            }
            else if (line.Length != _mazeWidth)
            {
                // All lines must be of same length. Any line does not match the length of the first line, an error is thrown
                throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze lines are not of equal length (found on maze line " + lineNum + ")");
            }

            foreach (char c in line)
            {
                charCount += 1;

                // Check that there aren't any unknown or unallowed characters in the lines
                if (lineNum == 1 || isLastLine)
                {
                    // Top and bottom borders
                    CheckCharacterValidity(c, allowedBorderCharacters, lineNum);
                }
                // Left and right borders do not need to be checked for empty spaces because each line is trimmed earlier during runtime
                else
                {
                    CheckCharacterValidity(c, allowedCharacters, lineNum);
                }

                // Only one start region allowed per maze
                if (c == '^' && _startCoordinates.Count == 1 && !isLastLine)
                {
                    throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze contains more than one starting point");
                }
                else if (c == '^')
                {
                    _startCoordinates.Add(Coords(charCount, lineNum));
                }

                if (c == 'E' && _exitCoordinates.Count == _maximumExits)
                {
                    throw new Exception("The maze file \"" + _mazeName + "\" is invalid: maze contains more than " + _maximumExits + " exits");
                }
                else if (c == 'E')
                {
                    _exitCoordinates.Add(Coords(charCount, lineNum));
                }

                // Add coordinate with its respective maze character to a dictionary
                if (!isLastLine) _mazeCharacters.Add(Coords(charCount, lineNum), c);
            }
        }

        /// <summary>
        /// Checks if an invidual maze character is valid
        /// </summary>
        /// <param name="c"></param>
        /// <param name="allowedCharacters"></param>
        /// <param name="lineNumber"></param>
        private void CheckCharacterValidity(char c, char[] allowedCharacters, int lineNumber)
        {
            if (!allowedCharacters.Contains(c))
            {
                throw new Exception("The maze file \"" + _mazeName + "\" is invalid: unallowed character '" + c + "' found on line " + lineNumber);
            }
        }

        public string TransformCoordinate(int coordinate)
        {
            string handledCoordinate;

            if (coordinate < 10 )
            {
                // Add a frontal zero to single digit coordinates
                handledCoordinate = "0" + coordinate.ToString();
            }
            else
            {
                handledCoordinate = coordinate.ToString();
            }

            return handledCoordinate;
        }

        public string CreateCoordinate(string x, string y)
        {
            return x + "-" + y;
        }

        /// <summary>
        /// Integer coordinates to string
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public string Coords(int x, int y)
        {
            return CreateCoordinate(TransformCoordinate(x), TransformCoordinate(y));
        }

        /// <summary>
        /// String coordinate to integer array
        /// </summary>
        /// <param name="coordinate"></param>
        /// <returns></returns>
        public int[] ReverseCoords(string coordinate)
        {
            var splitCoord = coordinate.Split("-");
            int x = Int32.Parse(splitCoord[0]);
            int y = Int32.Parse(splitCoord[1]);

            return new int[] { x, y };
        }

        /// <summary>
        /// Draws the path Pentti took during his adventure
        /// </summary>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        public List<string> DrawPath(List<string> coordinates)
        {
            var count = 0;
            var mazeLines = _mazeLineList;
            var visitedCoords = new List<string>();
            var isBackTracking = false;

            foreach (string coord in coordinates)
            {
                // Set a different path drawing symbol after backtracking has finished
                if (visitedCoords.Contains(coord))
                {
                    isBackTracking = true;
                }
                else if (!visitedCoords.Contains(coord) && isBackTracking)
                {
                    isBackTracking = false;
                    _drawCharacter = (_drawCharacter == 9) ? 1 : _drawCharacter + 1;
                }

                visitedCoords.Add(coord);

                var intCoord = ReverseCoords(coord);
                var x = intCoord[0];
                var y = intCoord[1];
                count += 1;

                if (count == 1)
                {
                    EditLine(ref mazeLines, x, y, "O");
                }
                else if (count == coordinates.Count)
                {
                    EditLine(ref mazeLines, x, y, "X");
                }
                else
                {
                    EditLine(ref mazeLines, x, y, _drawCharacter.ToString());
                }
            }

            return mazeLines;
        }

        /// <summary>
        /// Edit a specific character of a specific row
        /// </summary>
        /// <param name="maze"></param>
        /// <param name="character"></param>
        /// <param name="line"></param>
        /// <param name="newChar"></param>
        public void EditLine(ref List<string> maze, int character, int line, string newChar)
        {
            maze[line - 1] = maze[line - 1].Remove(character - 1, 1).Insert(character - 1, newChar);
        }
    }
}
