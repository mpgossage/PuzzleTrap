using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solver
{
    using Position = System.Tuple<int, int>;

    public struct LevelState
    {
        public const char WALL = '#', EMPTY = '.', TUNNEL = '=';
        public const char MOUSE = 'M', CHEESE = 'C', TRAP = 'T';
        public const char WOOD = 'W', BOMB = '*', CRYSTAL = 'V';
        public const string REMOVABLE = "rbgyop";
        public const string FALLABLE = "rgbyopCW*";// REMOVABLE+CHEESE+WOOD+BOMB;
        public const string EXPLODABLE = "rgbyopCWMV";// FALLABLE+MOUSE+CRYSTAL-BOMB
        readonly static int[,] ADJACENT_CELLS = { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 } };

        public char[,] Grid;
        public int Width, Height;

        public LevelState Clone()
        {
            return new LevelState { Height = this.Height, Width = this.Width, Grid = (char[,])this.Grid.Clone() };
        }

        public static LevelState FromString(string str)
        {
            string[] arr = str.Replace("\r","").Split('\n').Where(s=>!string.IsNullOrEmpty(s)).ToArray();
            int w = arr[0].Length;
            int h = arr.Length;
            LevelState result = new LevelState() { Grid = new char[w, h], Width=w, Height=h };
            for(int j=0;j< h;j++)
            {
                for(int i=0;i< w;i++)
                {
                    result.Grid[i, j] = TranslateCell(arr[j][i]);
                }
            }
            return result;
        }
        public override string ToString()
        {
            string result = "";
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    result += Grid[i, j];
                }
                result += "\n";
            }
            return result;
        }
        // converts cells into the types used in the game
        public static char TranslateCell(char c)
        {
            if (c == ' ') return EMPTY;
            return c;
        }
        public static bool IsClickable(char c)
        {
            return REMOVABLE.Contains(Char.ToLower(c));
        }
        public static bool IsFallable(char c) // if it falls
        {
            return FALLABLE.Contains(c) || c==MOUSE;
        }
        public static bool IsMouseAttracting(char c)
        {
            return c == CHEESE || c == TRAP;
        }
        public static bool IsTrap(char c)
        {
            return c == TRAP;
        }
        public static bool IsLineOfSightBlocking(char c)
        {
            return !(c == EMPTY || c==TUNNEL);
        }
        public bool IsValid(int x, int y) { return x >= 0 && x < Width && y >= 0 && y < Height; }
        public bool IsValid(Position p) { return IsValid(p.Item1,p.Item2); }
        public char GetCell(int x, int y) { return (IsValid(x,y)) ? Grid[x,y] : WALL; }
        public char GetCell(Position p) { return GetCell(p.Item1, p.Item2); }
        // gets the list of the block and all its adjacent blocks of the same type
        public Position[] GetLinkedBlocks(int x,int y)
        {
            if (!IsValid(x, y)) return new Position[0];
            List<Position> result = new List<Position>();
            Queue<Position> todo = new Queue<Position>();
            char type = Grid[x, y];
            todo.Enqueue(new Position(x, y));
            while (todo.Count > 0)
            {
                Position pt = todo.Dequeue();
                var gridCopy = this.Grid;   // need a copy to pass into the iter
                // get adjacent which are valid, of the same type & not in result already
                var pts = GetAdjacentPoints(pt).Where(p => gridCopy[p.Item1, p.Item2] == type)
                    .Where(p => !result.Contains(p));
                // add to lists
                result.Add(pt);
                foreach (var p in pts) todo.Enqueue(p);
            }
            return result.ToArray();
        }

        // get adjacent points which are valid
        // uses generators as its simpler
        public IEnumerable<Position> GetAdjacentPoints(int x, int y)
        {
            for (int i = 0; i < 4; i++)
            {
                int x1=x + ADJACENT_CELLS[i, 0],y1=y + ADJACENT_CELLS[i, 1];
                if (IsValid(x1, y1)) yield return new Position(x1, y1);
            }
        }
        public IEnumerable<Position> GetAdjacentPoints(Position p)
        {
            return GetAdjacentPoints(p.Item1, p.Item2);
        }
        // gets all points affected by explosion(inc the bomb) Or NULL if it contains a mouse
        public List<Position> GetBombExplosion(int x,int y)
        {
            List<Position> result = new List<Position>();
            for(int i=x-1;i<=x+1;i++)
            {
                for(int j=y-1;j<=y+1;j++)
                {
                    if (!IsValid(i, j)) continue;
                    if (Grid[i, j] == MOUSE) return null;
                    if (EXPLODABLE.Contains(Grid[i, j]))
                    {
                        result.Add(new Position(i, j));
                    }
                }
            }
            // bombs do not destroy each other, so must include itself
            result.Add(new Position(x, y));
            return result;
        }

        // returns all of possible moves & their linked blocks
        public IEnumerable<Tuple<Position,Position[]>> GetPossibleMoves()
        {
            var done = new HashSet<Position>();
            for (int y=0;y<Height;y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (IsClickable(Grid[x, y]) && !done.Contains(new Position(x, y)))
                    {
                        var pts = GetLinkedBlocks(x, y);
                        // minimum 2 blocks to click
                        if (pts.Length >= 2)
                        {
                            done.UnionWith(pts);    // add all in
                            yield return Tuple.Create(new Position(x, y), pts);
                        }
                    }
                    else if (Grid[x,y]==BOMB)
                    {
                        // get explosion (if there is a mouse it will be null)
                        var pts = GetBombExplosion(x, y);
                        if (pts!=null)
                        {
                            yield return Tuple.Create(new Position(x, y), pts.ToArray());
                        }
                    }
                }           
            }
        }

        public static string MoveToString(Tuple<Position, Position[]> move)
        {
            return move.Item1 + ":[" + string.Join<Position>(",", move.Item2) + "]";
        }

        public LevelState MakeMove(Position[] pts)
        {
            LevelState ls = this.Clone();
            RemoveBlocks(ref ls, pts);
            ApplyGravity(ref ls);
            MoveAllMice(ref ls);
            return ls;
        }
        public void MoveAllMice(ref LevelState ls)
        {
            // its not enough to just move all mice once
            // sometimes moving mouse 2 will allow mouse 1 to now move
            bool mouseMoved = false;
            do
            {
                mouseMoved = false;
                foreach (var m in ls.GetAllMice())
                {
                    var newM = UpdateMouse(m, ref ls);
                    if (m.Item1 !=newM.Item1 || m.Item2 !=newM.Item2 )
                    {
                        mouseMoved = true; // the mouse moved
                        ApplyGravity(ref ls);
                    }
                }
            } while (mouseMoved);
        }
        // returns if two level states are equal (same grid)
        public bool IsEqual(ref LevelState ls)
        {
            if (ls.Width != Width || ls.Height != Height) return false;
            for(int h=0;h<Height;h++)
            {
                for(int w=0;w<Width;w++)
                {
                    if (ls.Grid[w, h] != Grid[w, h]) return false;
                }
            }
            return true;
        }
        // returns a state with all the specified blocks removed
        public static void RemoveBlocks(ref LevelState ls, Position[] pts)
        {
            foreach (var p in pts)
            {
                ls.Grid[p.Item1, p.Item2] = EMPTY;
            }
        }
        public static void ApplyGravity(ref LevelState ls)
        {
            bool falls = false;
            for (int i = 0; i < ls.Width; i++)
            {
                do
                {
                    falls = false;
                    for (int j = 1; j < ls.Height; j++)
                    {
                        if (ls.Grid[i, j] == EMPTY && IsFallable(ls.Grid[i, j - 1]))
                        {
                            ls.Grid[i, j] = ls.Grid[i, j - 1];
                            ls.Grid[i, j - 1] = EMPTY;
                            falls = true;
                        }
                    }
                } while (falls);    // must repeat while falling occurs
            }
        }
        // can mouse see cheese (-ve,+ve or 0 for no)
        public int CanMouseSeeCheese(Position p)
        {
            // look left first:
            for (int x = p.Item1 - 1; x >= 0; x--)
            {
                char c = Grid[x, p.Item2];
                if (IsMouseAttracting(c))
                    return -1;  // cheese to the left
                if (IsLineOfSightBlocking(c)) break; // blocked
            }
            // now try right
            for (int x = p.Item1 + 1; x < Width; x++)
            {
                char c = Grid[x, p.Item2];
                if (IsMouseAttracting(c))
                    return +1;  // cheese to the right
                if (IsLineOfSightBlocking(c)) break; // blocked
            }
            return 0;   // nothing
        }
        public IEnumerable<Position> GetAllMice()
        {
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (Grid[i, j]==MOUSE) yield return new Position(i, j);
                }
            }
        }
        public IEnumerable<Position> GetAllTraps()
        {
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (Grid[i, j] == TRAP) yield return new Position(i, j);
                }
            }
        }
        public bool IsMiceLeft()
        {        
            for (int j = 0; j < Height; j++)
            {
                for (int i = 0; i < Width; i++)
                {
                    if (Grid[i, j] == MOUSE) return true;
                }
            }
            return false;
        }
        // moves a mouse as far as possible
        // return final position of mouse (which might be on a trap if it died)
        public static Position UpdateMouse(Position mouse,ref LevelState ls)
        {
            int mx = mouse.Item1, my = mouse.Item2;
            while (true)
            {
                // special case: if mouse is directly above trap
                // can happen if the mouse drops onto the trap
                if (ls.GetCell(mx, my + 1) == TRAP)
                {
                    ls.Grid[mx, my] = EMPTY;  // no mouse
                    return new Position(mx, my + 1);    // mouse on the trap
                }

                int cheeseDir = ls.CanMouseSeeCheese(new Position(mx,my));
                if (cheeseDir == 0) break;    // no cheese, finished
                ls.Grid[mx, my] = EMPTY;  // no mouse

                // mouse will either move left/right as many steps as possible until it either reaches the cheese/trap
                // or it moves onto a place where it will fall down 
                while (true)
                {
                    mx += cheeseDir;
                    char c = ls.GetCell(mx,my);   // the cell we are on
                    if (IsTrap(c))
                    {
                        //Console.WriteLine("Mouse at {0},{1} which is a trap", mx, my);
                        return new Position(mx, my);    // mouse on the trap
                    }
                    if (c == CHEESE)
                    {
                        //Console.WriteLine("Mouse eats cheese at {0},{1}", mx, my);
                        ls.Grid[mx, my] = EMPTY;   // remove cheese
                        break;  // stop moving & look for new cheese
                    }
                    if (c != TUNNEL) // if not in a tunnel
                    {
                        if (ls.GetCell(mx, my + 1) == TRAP) // mouse is above trap, it falls in
                        {
                            return new Position(mx, my + 1);    // mouse on the trap
                        }
                        if (ls.GetCell(mx, my + 1) == EMPTY) break; // if not on ground, it falls
                    }
                }
                // fall if applicable
                while (ls.GetCell(mx, my + 1) == EMPTY) my++;
                ls.Grid[mx, my] = MOUSE;  // put mouse in (in case anything tries to fall)
                ApplyGravity(ref ls);     // everything else falls
            }
            return new Position(mx, my);    // where mouse is 
        }

        // returns an int value (lower better) on the fitness of the state:
        // based upon distance between mouse/trap
        public int ComputeFitness()
        {
            Position[] traps = GetAllTraps().ToArray();
            int fitness = 0;
            foreach(var mouse in GetAllMice())
            {
                int dist = traps.Min(p => IHypot(p,mouse));
                fitness += dist;
            }
            return fitness;
        }
        
        private static int IHypot(Position a, Position b)
        {
            int dx = a.Item1 - b.Item1, dy = a.Item2 - b.Item2;
            return (int)Math.Sqrt(dx*dx + dy*dy);
        }
    }
}
