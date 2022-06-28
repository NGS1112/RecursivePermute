/*
 * File:    Program.cs
 * Author:  Nicholas Shinn
 * Date:    09/10/2021
 * Update:  09/11/2021 -> Implemented Serial Permutation
 *          09/12/2021 -> Attempts to Implement Threaded Permutation
 *          09/15/2021 -> Threaded permutation implemented
 *                        Errors fixed.
 */

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        // Global variable to track time elapsed
        static private Stopwatch timer = new Stopwatch();

        // Global variable to track number of tasks created
        static private int taskCount = 0;

        static void Main(string[] args)
        {
            // Checks for valid number of arguments
            if (args.Length < 2 || args.Length > 3 )
            {
                Console.WriteLine("ERROR: Invalid Amount of Arguments.\nArguments must be in form of [base] [n] [optional flag]");
                return;
            }

            // Try to convert second argument to an int to be used as n, closes program if not possible or n is negative
            int n;
            try
            {
                n = int.Parse(args[1]);
                if (n < 0)
                {
                    Console.WriteLine("ERROR: Argument n must be 0 or greater.");
                    return;
                }
            } 
            catch (FormatException)
            {
                Console.WriteLine("ERROR: Argument n must be an integer.");
                return;
            }

            // Checks for optional flag, if present set op to true otherwise keep as false
            bool op = false;
            if (args.Length == 3 && args[2].Equals("show"))
            {
                op = true;
            }

            // Prints the opening line with string to permute, sets up permutation counter
            Console.WriteLine("Permuting " + args[0] + "\n");
            int count;

            // Determines if program should use single-threaded or multi-threaded method
            if (n == 0)
            {
                // Starts the timer, executes the first call, and stops timer on return
                timer.Start();
                string[] permutations = Permute(args[0]);
                timer.Stop();

                // If op flag set to true, prints out the permutations
                if (op)
                {
                    foreach (var combo in permutations)
                    {
                        Console.WriteLine(combo + '\n');
                    }
                }

                // Count is set to the total number of permutations obtained
                count = permutations.Length;
            }
            else
            {
                // Wraps arguments in an object array. Arguments are: head string, tail string, tail length, number of levels to create
                Object[] set = new object[] { "", args[0], args[0].Length, n };

                // Starts the timer, executes the first call, and stops timer on return
                timer.Start();
                string[] permutations = TaskPermute(set);
                timer.Stop();

                // If op flag set to true, prints out the permutations
                if (op)
                {
                    foreach (var combo in permutations)
                    {
                        Console.WriteLine(combo + '\n');
                    }
                }

                // Count is set to the total number of permutations obtained
                count = permutations.Length;
            }

            // Grabs the elapsed time from StopWatch
            TimeSpan temp = timer.Elapsed;

            // Checks how many tasks were created, prints out the amount if more than zero
            if (taskCount > 0)
            {
                Console.WriteLine(taskCount + " tasks created.\n");
            }

            // Prints out the total number of permutations and time elased in milliseconds
            Console.WriteLine(count + " permutations generated in " + String.Format(temp.Milliseconds.ToString()) + " milliseconds");
        }

        /*
         * Function:    Permute
         * 
         * Input:       string tail -> String to be permuted
         * 
         * Output:      String array of permutations generated from tail string
         * 
         * Description: Serial recursion method for finding permutations of a string.
         *              Recursively generates permutations of given tail string.
         */
        static string[] Permute(string tail)
        {
            // Gets the size of the string being permuted
            int size = tail.Length;

            // Creates a list to hold the permutations
            var permutes = new List<string>();

            // If size of string is one character or less, returns that character in a new string array
            if (size <= 1)
            {
                return new string[] { tail };
            }

            // If size of string larger than one character, we do the permutation
            for (int i = 0; i < size; i++)
            {
                // Split the tail string into a new head and tail based on the first character
                string new_head = tail.Substring(0, 1);
                string new_tail = tail.Substring(1);

                // Recursively call Permute and store result in tails
                string[] tails = Permute(new_tail);

                //  For each string returned in tails, add the head back on and move it to permutes
                foreach ( string end in tails)
                {
                    permutes.Add(new_head + end);
                }

                // Add the head to the opposite side of the tail for next iteration
                tail = new_tail + new_head;
            }

            // Return the array of permutations
            return permutes.ToArray();
        }

        /*
         * Function:    TaskPermute
         * 
         * Input:       Object o -> [0]: head (string)
         *                          [1]: tail (string)
         *                          [2]: size (int; size of tail)
         *                          [3]: n (int; layers of tasks to create)
         *                          
         * Output:      String array of permutations generated
         * 
         * Description: Based on value of n, recursively create tasks that solve for permutations 
         *              of substrings generated from initial tail. Each recursion is another level 
         *              of tasks generated.
         */
        static string[] TaskPermute(Object o)
        {
            // Unwraps the Object o and assigns it's contents to variables
            Object[] parameters = (Object[])o;
            string head = (string)parameters[0];
            string tail = (string)parameters[1];
            int size = (int)parameters[2];
            int n = (int)parameters[3];

            // Creates a list of strings to be used for permutations, as well as a list of Tasks
            List<string> permutations = new List<string>();
            List<Task<string[]>> branch = new List<Task<string[]>>();

            // If n is zero, Task calls the serial recursion method to do the final permutation and returns the result
            if (n <= 0)
            {
                string[] perm = Permute(tail);
                foreach (var com in perm)
                {
                    permutations.Add(head + com);
                }
                return permutations.ToArray();
            }

            // Otherwise, Task creates another set of tasks to handle the permutation
            for (int index = 0; index < tail.Length; index++)
            {
                string new_head = tail.Substring(index, 1);
                string new_tail = tail.Substring(0, index) + tail[(index + 1)..];
                Object[] subset = new object[] { new_head, new_tail, new_tail.Length, n - 1 };
                Task<string[]> t = Task<string[]>.Factory.StartNew(TaskPermute, subset);
                branch.Add(t);
            }

            // Waits for all tasks in the list to finish
            Task.WaitAll(branch.ToArray());

            // Once tasks are finished, iterates through the results to grab the permutations while updating the taskCounter
            foreach (Task<string[]> child in branch)
            {
                taskCount++;
                foreach (string res in child.Result)
                {
                    permutations.Add(head + res);
                }
            }

            // Returns the final array of permutations
            return permutations.ToArray();
        }
    }
}
