﻿using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using Solver;
using Position = System.Tuple<int, int>;

namespace PuzzleTrap
{
    [TestClass]
    public class UnitTest1
    {
        string rootDir = Path.Combine(Directory.GetCurrentDirectory(), "../../../Solver/tests");

        LevelState LoadLevel(string fname)
        {
            fname = Path.Combine(rootDir, fname);
            //Console.WriteLine("load file {0}", fname);
            string file = File.ReadAllText(fname);
            return LevelState.FromString(file);
        }

        [TestMethod]
        public void TestLevelStateFromFile()
        {
            string fname = Path.Combine(rootDir, "test_level_load.txt");
            string file = File.ReadAllText(fname);
            Assert.IsTrue(file.Length > 0);
            LevelState ls = LevelState.FromString(file);
            Assert.IsNotNull(ls);
            Assert.AreEqual(ls.Width, 9);
            Assert.AreEqual(ls.Height, 6);
            Assert.AreEqual(ls.Grid[2, 2], LevelState.MOUSE);
            Assert.AreEqual(ls.Grid[5, 2], LevelState.CHEESE);
            Assert.AreEqual(ls.Grid[0, 3], LevelState.CHEESE);
            Assert.AreEqual(ls.Grid[3, 5], LevelState.CHEESE);
            Assert.AreEqual(ls.Grid[5, 5], LevelState.TRAP);
            Assert.AreEqual(ls.Grid[6, 5], LevelState.TRAP);
        }
        [TestMethod]
        public void TestGetLinkedBlocks()
        {
            LevelState ls = LoadLevel("test_level_load.txt");
            Assert.IsNotNull(ls);
            var res = ls.GetLinkedBlocks(2, 2);   // the mouse
            Assert.AreEqual(res.Length, 1);
            Assert.AreEqual(res[0].Item1, 2);
            Assert.AreEqual(res[0].Item2, 2);
            res = ls.GetLinkedBlocks(3, 2);   // 2xR
            Assert.AreEqual(res.Length, 2);
            Assert.IsTrue(res.Contains(new Position(3, 2)));
            Assert.IsTrue(res.Contains(new Position(4, 2)));
            // get the 4 r's from 3 differen points
            var res1 = ls.GetLinkedBlocks(1, 3);
            var res2 = ls.GetLinkedBlocks(2, 3);
            var res3 = ls.GetLinkedBlocks(1, 5);
            Assert.IsTrue(res1.Length == res2.Length && res2.Length == res3.Length);
            foreach (var p in res1)
                Assert.IsTrue(res2.Contains(p) && res3.Contains(p));
        }
        [TestMethod]
        public void TestGetPossibleMoves()
        {
            LevelState ls = LoadLevel("test_level_load.txt");
            Assert.IsNotNull(ls);
            var res = ls.GetPossibleMoves();
            Assert.AreEqual(res.Count(), 3);
            System.Diagnostics.Debug.WriteLine(
                string.Join("\n", res.Select(m => LevelState.MoveToString(m))));
            // 2,4,4 items
            Assert.AreEqual(res.Max(m => m.Item2.Length), 4);
            Assert.AreEqual(res.Min(m => m.Item2.Length), 2);
            Assert.AreEqual(res.Sum(m => m.Item2.Length), 10);
        }
        [TestMethod]
        public void TestApplyGravity()
        {
            LevelState ls = LoadLevel("test_gravity.txt");
            Assert.IsNotNull(ls);
            Console.WriteLine("Before \n" + ls);
            LevelState.ApplyGravity(ref ls);
            Console.WriteLine("After \n" + ls);
            Assert.AreEqual(ls.Grid[0, 0], LevelState.EMPTY);
            Assert.AreEqual(ls.Grid[0, 1], 'r');
            Assert.AreEqual(ls.Grid[0, 2], 'r');
            Assert.AreEqual(ls.Grid[0, 3], 'r');
            Assert.AreEqual(ls.Grid[1, 0], 'g');
            Assert.AreEqual(ls.Grid[2, 0], LevelState.EMPTY);
            Assert.AreEqual(ls.Grid[2, 1], LevelState.EMPTY);
            Assert.AreEqual(ls.Grid[2, 2], LevelState.EMPTY);
            Assert.AreEqual(ls.Grid[2, 3], 'g');
        }
        [TestMethod]
        public void TestRemoveBlocksApplyGravity()
        {
            LevelState ls = LoadLevel("test_level_load.txt");
            Assert.IsNotNull(ls);
            Position[] move = { new Position(3, 3), new Position(4, 3), new Position(5, 3), new Position(6, 3) };
            LevelState.RemoveBlocks(ref ls, move);
            for (int i = 3; i <= 6; i++)
                Assert.AreEqual(ls.Grid[i, 3], LevelState.EMPTY);
            Console.WriteLine("Before:\n" + ls);
            LevelState.ApplyGravity(ref ls);
            Console.WriteLine("After:\n" + ls);
            for (int i = 3; i <= 6; i++)    // 2 line is now clear
                Assert.AreEqual(ls.Grid[i, 2], LevelState.EMPTY);
            Assert.AreEqual(ls.Grid[3, 3], 'r');
            Assert.AreEqual(ls.Grid[4, 3], 'r');
            Assert.AreEqual(ls.Grid[5, 3], LevelState.CHEESE);
            Assert.AreEqual(ls.Grid[6, 3], LevelState.EMPTY);
        }
        [TestMethod]
        public void TestCanMouseSeeCheese()
        {
            LevelState ls = LoadLevel("test_level_load.txt");
            Assert.IsNotNull(ls);
            // silly pos
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(0, 0)), 0);
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(8, 0)), 0);
            // in wall
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(4, 4)), 0);
            // cheese to left
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(6, 2)), -1);
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(7, 2)), -1);
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(8, 2)), -1);
            // blocked left
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(7, 3)), 0);
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(8, 3)), 0);
            // cheese right
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(2, 5)), 1);
            // blocked right
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(0, 5)), 0);
            // between cheese & trap (goes left)
            Assert.AreEqual(ls.CanMouseSeeCheese(new Position(4, 5)), -1);
        }
        [TestMethod]
        public void TestUpdateMouse()
        {
            LevelState ls = LoadLevel("test_mouse.txt");
            Assert.IsNotNull(ls);
            // mouse at 0,0 should walk to cheese
            Position mouse = new Position(0, 0);
            mouse = LevelState.UpdateMouse(mouse, ref ls);
            Assert.AreEqual(ls.GetCell(0, 0), LevelState.EMPTY);    // mouse has moved
            Assert.AreEqual(new Position(5, 0), mouse); // mouse at 5,0
            Assert.AreEqual(ls.GetCell(6, 0), LevelState.TRAP);    // mouse on trap
            // mouse at 3,2 will eat cheese at 0,2 and 6,2
            mouse = LevelState.UpdateMouse(new Position(3, 2), ref ls);
            Assert.AreEqual(new Position(6, 2), mouse); // mouse at 6,2
            Assert.AreEqual(LevelState.MOUSE, ls.GetCell(6, 2));    // mouse at 6,2
            Assert.AreEqual(LevelState.EMPTY, ls.GetCell(3, 2));    // mouse has moved
            Assert.AreEqual(LevelState.EMPTY, ls.GetCell(0, 2));    // cheese is gone
            // mouse at 1,4 will run to cheese but fall into hole in 4,5
            mouse = LevelState.UpdateMouse(new Position(1, 4), ref ls);
            Assert.AreEqual(new Position(4, 5), mouse);
        }
        [TestMethod]
        public void TestUpdateMouse2()
        {
            LevelState ls = LoadLevel("test_mouse.txt");
            Assert.IsNotNull(ls);
            // mouse at 0,8 will run to 8,8 eating all the cheese
            // but all the blocks which the cheese was sitting on will fall
            Console.WriteLine("Before:\n" + ls);
            Position mouse = LevelState.UpdateMouse(new Position(0, 8), ref ls);
            Console.WriteLine("After:\n" + ls);
            Assert.AreEqual(new Position(8, 8), mouse);
            for (int i = 1; i < 8; i++)
            {
                Assert.AreEqual(LevelState.EMPTY, ls.GetCell(i, 7));
                Assert.AreEqual('r', ls.GetCell(i, 8)); // note: lower case
            }
            // mouse at 0,10 will run right, fall down the hole,
            // then goto and eat the cheese at 7,11
            //Console.WriteLine("Before:\n" + ls);
            mouse = LevelState.UpdateMouse(new Position(0, 10), ref ls);
            //Console.WriteLine("After:\n" + ls);
            Assert.AreEqual(new Position(7, 11), mouse);
        }
        [TestMethod]
        public void TestMakeMove()
        {
            LevelState ls = LoadLevel("test_mouse.txt");
            Assert.IsNotNull(ls);
            // remove dummmy point & update all mice & gravity
            LevelState ls2 = ls.MakeMove(new Position[] { new Position(1, 0) });
            // check mice positions
            Assert.AreEqual(LevelState.MOUSE, ls2.GetCell(6, 2));
            Assert.AreEqual(LevelState.MOUSE, ls2.GetCell(4, 5));
            Assert.AreEqual(LevelState.MOUSE, ls2.GetCell(8, 8));
            Assert.AreEqual(LevelState.MOUSE, ls2.GetCell(7, 11));
        }
        [TestMethod]
        public void TestBombGetPossibleMoves()
        {
            LevelState ls = LoadLevel("test_bomb.txt");
            Assert.IsNotNull(ls);
            var moves=ls.GetPossibleMoves().ToList();
            var clicks = moves.ConvertAll(t => t.Item1.ToString());
            Console.WriteLine("Click points: " + string.Join(",",clicks));
            Console.WriteLine(moves.Exists(m => (m.Item1.Item1==2 && m.Item1.Item2==1)));
            // bomb at 2,1 can be clicked
            Assert.IsTrue(moves.Exists(m => (m.Item1.Equals(new Position(2, 1)))));
            // bomb at 5,1 cannot be clicked (too near to a mouse)
            Assert.IsFalse(moves.Exists(m => m.Item1.Equals(new Position(5, 1))));
            // tiles 1,2 2,2 3,2 can be clicked
            Assert.IsTrue(moves.Exists(m => m.Item1.Equals(new Position(1, 2))));
            Assert.IsTrue(moves.Exists(m => m.Item1.Equals(new Position(2, 2))));
            Assert.IsTrue(moves.Exists(m => m.Item1.Equals(new Position(3, 2))));
        }
    }
}