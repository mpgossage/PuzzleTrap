using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    class Program
    {
        readonly string rootDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../Solver/levels");

        static void Main(string[] args)
        {
            new Program().SolveAStar("level079.txt");
            //new Program().SolveAll();
        }

        void SolveAll()
        {
            Solver solver = new Solver();
            System.Diagnostics.Stopwatch timer = new System.Diagnostics.Stopwatch();
            for (int i=1;i<=75;i++)
            {
                if (i==36 || i==42 || i==57) // these level take a long time
                {
                    Console.WriteLine("Skipping level {0} it takes too long", i);
                    continue;
                }
                string file = File.ReadAllText(Path.Combine(rootDir, string.Format("level{0:000}.txt",i)));
                LevelState ls = LevelState.FromString(file);
                Console.WriteLine("\n\nSolving level {0}...", i);
                timer.Restart();
                string[] result = solver.SolveAStar(ls);
                timer.Stop();
                if (result==null)
                {
                    Console.WriteLine("Unable to solve level {0} in {1} ms", i,timer.ElapsedMilliseconds);
                }
                else
                {
                    Console.WriteLine("Solved level {0} in {1} ms", i, timer.ElapsedMilliseconds);
                }
            }
            Console.WriteLine("Test Complete");
            Console.ReadLine();
        }

        void SolveAStar(string name)
        {
            string file = File.ReadAllText(Path.Combine(rootDir, name));
            LevelState ls = LevelState.FromString(file);

            Solver solver = new Solver();
            string[] result = solver.SolveAStar(ls);
            if (result != null)
            {
                Console.WriteLine("Solution Found:");
                foreach (var s in result) Console.WriteLine(s);
            }
            Console.ReadLine();
        }

        void SolveOne(string name)
        {
            string file = File.ReadAllText(Path.Combine(rootDir,name));
            LevelState ls=LevelState.FromString(file);

            Solver solver = new Solver();
            // SolveEx(ls,true) gives a step by step summary
            // SolveEx(ls) just looks for the answer with minimal display
            string[] result = solver.SolveEx(ls);
            if (result==null)
            {
                Console.WriteLine("No Result found");
                solver.SolveEx(ls,true);
            }
            else
            {
                Console.WriteLine("Solution Found:");
                foreach (var s in result) Console.WriteLine(s);
            }
            Console.ReadLine();
        }
    }
}
