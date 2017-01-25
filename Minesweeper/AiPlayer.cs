using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    class AiPlayer
    {
        private Minefield Field { get; set; }
        private List<int> NextMoves { get; set; } = new List<int>();
        private List<int> NextFlags { get; set; } = new List<int>();
        private List<Cell> KnownBombs { get; set; } = new List<Cell>();
        private bool Initialized { get; set; } = false;

        internal AiPlayer(Minefield field)
        {
            Field = field;
        }

        internal bool Move()
        {
            var visibleCells = from cell in Field.Playfield where cell.View == Cell.ViewState.Visible select cell;

            if (!Initialized && visibleCells.Count() == 0)
            {
                Initialized = true;
                PickFirstPoint();
                return true;
            }

            Initialized = true;

            if (TakeMove())
            {
                return true;
            }

            FindNextMove();

            if (!TakeMove())
            {
                Trace.WriteLine($"AI Found no moves! {KnownBombs.Count} found");
                return false;
            }

            return true;
        }

        private bool TakeMove()
        {
            if (NextMoves.Count == 0)
            {
                return PlaceFlag();
            }

            // Check to make sure we aren't moving to an existing cell
            if (Field.Playfield[NextMoves[0]].View == Cell.ViewState.Visible)
            {
                Trace.Write($"Eliminating already visible elements was {NextMoves.Count} ");
                NextMoves = (from move in NextMoves
                             where Field.Playfield[move].View == Cell.ViewState.Hidden
                             select move).ToList();

                Trace.WriteLine($"now {NextMoves.Count}");
                return TakeMove();
            }

            Field.SelectIndex(NextMoves[0]);
            NextMoves.RemoveAt(0);

            return true;
        }

        private bool PlaceFlag()
        {
            if (NextFlags.Count == 0)
            {
                return false;
            }


            var nextFlag = Field.Playfield[NextFlags[0]];

            //Debug.Assert(nextFlag.Flag == Cell.FlagState.None);
            if (nextFlag.Flag != Cell.FlagState.None)
            {
                Trace.WriteLine($"BUGBUG: duplicate flag - {nextFlag.Index}");
                nextFlag.ClearFlag();
            }
            nextFlag.ToggleFlag();
            NextFlags.RemoveAt(0);

            return true;
        }

        private void PickFirstPoint()
        {
            Random random = new Random();
            int width = Field.Width;
            int height = Field.Height;

            Minefield.Point p;
            p.Row = random.Next(height / 3) + height / 3;
            p.Col = random.Next(width / 3) + width / 3;

            Field.SelectPoint(p);

            FindNextMove();
        }

        private void FindNextMove()
        {
            IEnumerable<Cell> neighbors = FindNeighbors();

            List<Cell> bombs = GetKnownBombs(neighbors);

            CalucateSafeMoves(neighbors, bombs);

            if (NextMoves.Count == 0)
            {
                FindDeepMove(neighbors);
            }

            Trace.WriteLine($"There were {NextMoves.Count} moves added");
        }

        private void CalucateSafeMoves(IEnumerable<Cell> neighbors, List<Cell> bombs)
        {
            // A safe space is all of the spaces next to a cleared space
            // where the number of bombs is equal to the number of neighbor bombs
            var safe = (from cell in neighbors
                        let hidden = GetHiddenNeighbors(cell.Index)
                        let likeClear = (from h in hidden
                                         where !bombs.Contains(h)
                                         select h)
                        where hidden.Count() - likeClear.Count() == cell.BombNeighbors
                        from clear in likeClear
                        select clear.Index).Distinct();

            NextMoves.AddRange(from s in safe where !NextMoves.Contains(s) select s);
        }

        private List<Cell> GetKnownBombs(IEnumerable<Cell> neighbors)
        {
            // Bombs are in all of the locations where the number of hidden spaces
            // is the same as the number of bomb neighbors
            var bombs = (from cell in neighbors
                         let hidden = GetHiddenNeighbors(cell.Index)
                         where cell.BombNeighbors == hidden.Count()
                         from bomb in hidden
                         select bomb).Distinct().ToList();

            var newbombs = from bomb in bombs
                           where !KnownBombs.Contains(bomb)
                           select bomb.Index;

            NextFlags.AddRange(newbombs);

            bombs.AddRange(KnownBombs);
            KnownBombs = bombs.Distinct().ToList();
            bombs = KnownBombs;
            return bombs;
        }

        private bool FindDeepMove(IEnumerable<Cell> neighbors)
        {
            bool found = false;
            neighbors = RemoveSatifiedSensors(neighbors);

            foreach (var testCell in neighbors)
            {
                found |= FindCellMove(neighbors, testCell);
            }

            return found;
        }

        private bool FindCellMove(IEnumerable<Cell> neighbors, Cell testCell)
        {
            var hiddenNeighbors = GetHiddenNeighbors(testCell.Index);
            var hiddenNonBombNeighbors = from bomb in hiddenNeighbors
                                         where bomb.Flag != Cell.FlagState.Flagged
                                         select bomb;

            var testNeeds = testCell.BombNeighbors - hiddenNeighbors.Count() + hiddenNonBombNeighbors.Count();

            bool found = false;

            // From the visible neighbors - choose those with overlaps 
            foreach (var v in GetUseableNeighbors(neighbors, testCell))
            {
                var vHiddenNeighbors = GetHiddenNeighbors(v.Index);
                var vHiddenNoFlags = from cell in vHiddenNeighbors where cell.Flag == Cell.FlagState.None select cell;
                var vCommonNeighbors = from h in vHiddenNeighbors
                                       where hiddenNeighbors.Contains(h)
                                       select h;
                var vCommonHiddenNoFlag = from nf in vCommonNeighbors
                                          where nf.Flag == Cell.FlagState.None
                                          select nf;

                if (vCommonHiddenNoFlag.Count() == 0)
                {
                    continue;
                }

                // Now look at this neighbor and compare how many bombs it says, and how many are known
                var vHas = (from bomb in vHiddenNeighbors
                            where bomb.Flag == Cell.FlagState.Flagged
                            select bomb).Count();
                var vNeeds = v.BombNeighbors - vHas;

                // if both cells have the same needs and the testCell overlaps with the v cell
                // then all of the non-overlapping cells are safe
                if (vNeeds == testNeeds && vCommonHiddenNoFlag.Count() == hiddenNonBombNeighbors.Count())
                {
                    var safe = from cell in vHiddenNeighbors
                               where cell.Flag == Cell.FlagState.None && !vCommonNeighbors.Contains(cell)
                               select cell.Index;

                    foreach (var s in safe)
                    {
                        Debug.Assert(Field.Playfield[s].Mine == Cell.MineState.None);
                    }

                    NextMoves.AddRange(safe);
                }

                // Only consider cases where the testCell needs fewer than the new cell
                if (vNeeds <= testNeeds)
                {
                    continue;
                }

                // How many cells are left?
                var fillCells = (from cell in vHiddenNoFlags
                                 where !vCommonNeighbors.Contains(cell)
                                 select cell).ToList();

                if (fillCells.Count != (vNeeds - testNeeds))
                {
                    continue;
                }

                foreach (var cell in fillCells)
                {
                    NextFlags.Add(cell.Index);
                    Debug.Assert(cell.Mine == Cell.MineState.Bomb);
                    KnownBombs.Add(cell);
                }

                Trace.WriteLine($"Added {fillCells.Count} bomb(s)");
                found = true;
            }

            return found;
        }

        private IEnumerable<Cell> GetUseableNeighbors(IEnumerable<Cell> neighbors, Cell testCell)
        {
            return from cell in GetVisibleNeighbors(testCell.Index)
                   where neighbors.Contains(cell)
                   select cell;
        }

        private IEnumerable<Cell> RemoveSatifiedSensors(IEnumerable<Cell> neighbors)
        {
            neighbors = from neigbor in neighbors
                        where GetHiddenNeighbors(neigbor.Index).Count() > neigbor.BombNeighbors
                        select neigbor;
            return neighbors;
        }

        private IEnumerable<Cell> GetVisibleNeighbors(int index)
        {
            return from c in Field.GetNeighbors(index)
                   let cell = Field.Playfield[c]
                   where cell.View == Cell.ViewState.Visible && cell.BombNeighbors != 0
                   select cell;
        }

        private IEnumerable<Cell> GetHiddenNeighbors(int index)
        {
            return from c in Field.GetNeighbors(index)
                   where Field.Playfield[c].View != Cell.ViewState.Visible
                   select Field.Playfield[c];
        }

        private IEnumerable<Cell> FindNeighbors()
        {
            var cellList = Field.Playfield;
            var bombNeighbors = from cell in Field.Playfield
                                where cell.BombNeighbors > 0 && cell.View == Cell.ViewState.Visible
                                select cell;

            return bombNeighbors;
        }
    }
}
