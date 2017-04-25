using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    using Priority_Queue;
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
                int totalSolutions = 0, discardedSolutions = 0;
                foreach(var s in fromList)
                {
                    if (!s.ls.IsMiceLeft()) return s.moves.ToArray();
                    foreach (var m in s.ls.GetPossibleMoves())
                    {
                        totalSolutions++;
                        var newLs = s.ls.MakeMove(m.Item2);
                        // only add new Level state if its not found in the list of existing answers
                        var lsStr = newLs.ToString();
                        if (toList.Any(sol => sol.SimilarTo(lsStr)))
                        {
                            discardedSolutions++;
                        }
                        else
                        {
                            var newSol = new Solution(newLs, m.Item1, s.ls.GetCell(m.Item1), s.moves.ToArray());
                            toList.Add(newSol);
                            bestFitness = Math.Min(bestFitness, newSol.fitness);   // small is better
                            worstFitness = Math.Max(worstFitness, newSol.fitness); // big is worse
                        }
                    }
                }
                Console.WriteLine("Total Solutions {0} Discared {1} Final {2}", totalSolutions,discardedSolutions,toList.Count);
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

        /** A Star Based Solver
        */
        public string[] SolveAStar(LevelState ls)
        {
            if (!ls.IsMiceLeft()) return new string[] { ls.ToString() };
            var liveQueue = new SimplePriorityQueue<Solution>(); // items to try
            var deadQueue = new HashSet<string>();  // states which we tried and didn't work

            Solution sol = new Solution(ls);
            liveQueue.Enqueue(sol, sol.fitness); // lower fitness = higher priority
            int count = 0, totalSolutions = 0;
            while (liveQueue.Count > 0)
            {
                var s = liveQueue.Dequeue();
                string stateStr = s.ls.ToString();
                if (count % 10000 == 0)
                {
                    Console.WriteLine("Step {0}, total Solutions {1}, liveList {2}, deadList {3} current State", count, totalSolutions, liveQueue.Count, deadQueue.Count);
                    Console.WriteLine(stateStr);
                }
                count++;
                deadQueue.Add(stateStr);    // this has been tried
                foreach (var m in s.ls.GetPossibleMoves())
                {
                    totalSolutions++;
                    var newLs = s.ls.MakeMove(m.Item2);

                    // ignore existing solutions
                    var newLsStr = newLs.ToString();
                    if (deadQueue.Contains(newLsStr)) continue;
                    // should compare against the live list too...
                    // this makes a BIG difference on level 16
                    if (liveQueue.FirstOrDefault((sln) => { return sln.SimilarTo(newLsStr); }) != null)
                        continue;
                    
                    var newSol = new Solution(newLs, m.Item1, s.ls.GetCell(m.Item1), s.moves.ToArray());

                    if (!newLs.IsMiceLeft())
                    {
                        Console.WriteLine("Found Solution on Step {0}, total Solutions {1}, liveList {2}, deadList {3} current State", count, totalSolutions, liveQueue.Count, deadQueue.Count);
                        return newSol.moves.ToArray();   // solution found
                    }
                    liveQueue.Enqueue(newSol, newSol.fitness);  // add to queue
                }
            }
            Console.WriteLine("Failed: tried {0} steps, {1} total solutions, {2} unique solutions", count, totalSolutions, deadQueue.Count);
            return null; // not found
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
            // returns if a solution is similar (has the same level state) as the provided level
            // its FAR quicker to convert to string & do a string compare than a 2d array compare
            public bool SimilarTo(string levelState)
            {
                return levelState == moves.Last();  // moves.last is always the level state stringified
            }
        }
    }
}
