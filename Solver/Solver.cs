using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    using Position = System.Tuple<int, int>;

    public class Solver
    {
        const int MAX_SOLUTIONS_PER_MOVE = 1000 * 100;
        public string[] Solve(LevelState ls)
        {
            if (!ls.IsMiceLeft()) return new string[]{ls.ToString() };
            Queue<Solution> queue = new Queue<Solution>();
            queue.Enqueue(new Solution(ls));
            while (queue.Count > 0)
            {
                Solution s = queue.Dequeue();
                if (!s.ls.IsMiceLeft()) return s.moves.ToArray();
                foreach(var m in s.ls.GetPossibleMoves())
                {
                    queue.Enqueue(new Solution(s.ls.MakeMove(m.Item2),m.Item1, s.ls.GetCell(m.Item1), s.moves.ToArray()));
                }
            }
            return null;
        }
        public string[] SolveEx(LevelState ls, bool stepByStep=false)
        {
            if (!ls.IsMiceLeft()) return new string[] { ls.ToString() };
            List<Solution> fromList = new List<Solution>();
            var sln = new Solution(ls);
            fromList.Add(sln);
            Console.WriteLine("Inital Fitness {0}", sln.fitness);
            int move = 0;
            while (fromList.Count > 0)
            {
                Console.WriteLine("Move {0}, we have {1} possible solutions:", move, fromList.Count);
                if (stepByStep)
                {
                    foreach (var s in fromList)
                    {
                        Console.WriteLine(s.ls.ToString());
                    }
                    Console.ReadLine();
                }
                move++;
                List<Solution> toList = new List<Solution>();
                int bestFitness = int.MaxValue, worstFitness = 0; 
                foreach(var s in fromList)
                {
                    if (!s.ls.IsMiceLeft()) return s.moves.ToArray();
                    foreach (var m in s.ls.GetPossibleMoves())
                    {
                        var sol = new Solution(s.ls.MakeMove(m.Item2), m.Item1, s.ls.GetCell(m.Item1), s.moves.ToArray());
                        toList.Add(sol);
                        bestFitness = Math.Min(bestFitness, sol.fitness);   // small is better
                        worstFitness = Math.Max(worstFitness, sol.fitness); // big is worse
                    }
                }
                Console.WriteLine("Best Fitness {0} Worst Fitness {1}", bestFitness, worstFitness);
                if (toList.Count<= MAX_SOLUTIONS_PER_MOVE)
                {
                    fromList = toList;
                }
                else
                {
                    // estimate fitness threshold
                    int deltaFit = worstFitness - bestFitness;
                    int threshold = bestFitness + (deltaFit * MAX_SOLUTIONS_PER_MOVE) / toList.Count;
                    fromList.Clear();
                    foreach(var s in toList)
                    {
                        if (s.fitness<= threshold)
                        {
                            fromList.Add(s);
                        }
                    }
                    Console.WriteLine("Limiting Fitness to {0} cut from {1} to {2}", threshold,toList.Count,fromList.Count);
                }

            }
            return null;
        }

        private class Solution
        {
            public LevelState ls;
            public List<string> moves;
            public int fitness;
            public Solution(LevelState l)
            {
                ls = l.Clone();
                fitness = l.ComputeFitness();
                moves = new List<string>();
                moves.Add(ls.ToString());
            }
            public Solution(LevelState l,Position pos,char cell,string[] mvs=null)
            {
                ls = l.Clone();
                fitness = l.ComputeFitness();
                moves = new List<string>(mvs);
                moves.Add(string.Format("Click at {0},{1} ({2})", pos.Item1, pos.Item2,cell));
                moves.Add(ls.ToString());
            }
        }
    }
}
