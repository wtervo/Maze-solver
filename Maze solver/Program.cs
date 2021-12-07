// Oskari Tervo, December 2021

using System;

namespace Maze_solver
{
    class Program
    {
        static void Main(string[] args)
        {
            var solver = new Solver();
            try
            {
                solver.ExecuteSolver();
            }
            catch (Exception e)
            {
                // All non-input related errors are caught here and displayed to the user
                Console.WriteLine("");
                Console.WriteLine("=====");
                Console.WriteLine("ERROR: " + e);
                Console.WriteLine("=====");
                Console.WriteLine("");
            }
        }
    }
}
