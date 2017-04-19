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
        static void Main(string[] args)
        {
            string rootDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../Solver/levels");
            string file = File.ReadAllText(Path.Combine(rootDir, "level020.txt"));
            LevelState ls=LevelState.FromString(file);

            Solver solver = new Solver();
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
