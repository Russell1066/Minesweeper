using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    class Minefield : INotifyPropertyChanged
    {
        private List<Cell> Playfield;
        private static List<Point> Neighbors;
        private int Width;
        private int Height;
        private int NumBombs;
        private Random random = new Random();

        private GameState _State;
        public GameState State
        {
            get { return _State; }
            private set
            {
                if (_State != value)
                {
                    _State = value;
                    OnPropertyChanged(nameof(State));
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public enum GameState
        {
            Playing,
            Won,
            Lost
        };

        public struct Point
        {
            public int Row;
            public int Col;

            public Point(int row, int column)
            {
                Row = row;
                Col = column;
            }

            static public Point operator +(Point lhs, Point rhs)
            {
                Point retv;
                retv.Row = lhs.Row + rhs.Row;
                retv.Col = lhs.Col + rhs.Col;

                return retv;
            }

            public override string ToString()
            {
                return string.Format($"({Row}, {Col})");
            }
        };

        static Minefield()
        {
            Neighbors = new List<Point>();
            for (int row = -1; row < 2; ++row)
            {
                for (int col = -1; col < 2; ++col)
                {
                    if (row != 0 || col != 0)
                    {
                        Neighbors.Add(new Point() { Row = row, Col = col });
                    }
                }
            }
        }

        public Minefield(ICollection<Cell> cells, int width, int start, int numBombs)
        {
            State = GameState.Playing;
            int count = cells.Count;
            Debug.Assert((count % width) == 0);

            Width = width;
            Height = count / width;
            NumBombs = numBombs;

            Debug.Assert(Width > 0 && Height > 0 && numBombs > 0 && numBombs < (Width * Height - 9));
            Debug.Assert(start >= 0 && start < count);

            Playfield = cells.ToList();

            GenerateField(IndexToPoint(start), numBombs);
        }

        private List<int> GetNeighbors(Point p)
        {
            ValidatePoint(p);

            List<int> retv = new List<int>();
            foreach (var cell in Neighbors)
            {
                Point neighbor = p + cell;
                if (InField(neighbor))
                {
                    retv.Add(PointToIndex(neighbor));
                }
            }

            return retv;
        }

        public bool SelectIndex(int index)
        {
            return SelectPoint(IndexToPoint(index));
        }

        public bool SelectPoint(Point p)
        {
            if (GetCell(p).Mine == Cell.MineState.Bomb)
            {
                State = GameState.Lost;

                return true;
            }

            ClearCells(p);

            var hiddenCells = from cell in Playfield
                              where cell.View == Cell.ViewState.Hidden
                              select cell;

            if (hiddenCells.Count() == NumBombs)
            {
                State = GameState.Won;
            }

            return false;
        }

        private void ClearCells(Point p)
        {
            HashSet<int> known = new HashSet<int>();
            List<int> neighbors = new List<int>();
            int index = PointToIndex(p);
            known.Add(index);
            neighbors.Add(index);

            while (neighbors.Count > 0)
            {
                //Trace.Write($"({IndexToPoint(neighbors[0])} ({Playfield[neighbors[0]]})) - ");
                index = neighbors[0];
                neighbors.RemoveAt(0);

                var cell = Playfield[index];
                cell.SetVisible();
                if (cell.BombNeighbors > 0)
                {
                    continue;
                }

                foreach (var v in GetNeighbors(IndexToPoint(index)))
                {
                    // We know we've touched this - don't do it again
                    if (known.Contains(v))
                    {
                        continue;
                    }

                    // Get the cell and mark it as one we won't touch again
                    cell = Playfield[v];
                    known.Add(v);

                    // If we've exposed this one at some point (ever) and we are seeing it again
                    // it cannot add to our fill
                    if (cell.View == Cell.ViewState.Visible)
                    {
                        continue;
                    }

                    // If a cell is a bomb, don't uncover it for god's sake!
                    if (cell.Mine == Cell.MineState.Bomb)
                    {
                        //Trace.Write($"({IndexToPoint(v)} bomb) ");
                        continue;
                    }

                    // All our conditions are met, show this one
                    //Trace.Write($"({IndexToPoint(v)} {cell}) ");
                    cell.SetVisible();

                    neighbors.Add(v);
                }

                //Trace.WriteLine("");
            }
        }

        private void DisplayField()
        {
            for (int i = 0; i < Playfield.Count; ++i)
            {
                if (i % Width == 0)
                {
                    Trace.WriteLine("");
                }

                Trace.Write($"{Playfield[i]}");
            }

            Trace.WriteLine("");
            var v = from cell in Playfield
                    where cell.View == Cell.ViewState.Hidden
                    select cell;

            Trace.WriteLine($"{v.Count()} Hidden - with {NumBombs} bombs");
        }

        private int PointToIndex(Point p)
        {
            ValidatePoint(p);
            return p.Row * Width + p.Col;
        }

        private Point IndexToPoint(int index)
        {
            var p = new Point(index / Width, index % Width); ;
            ValidatePoint(p);
            return p;
        }

        private bool InField(Point p)
        {
            return InField(p.Row, p.Col);
        }

        private bool InField(int row, int column)
        {
            return row >= 0 && row < Height && column >= 0 && column < Width;
        }

        private Cell GetCell(Point p)
        {
            ValidatePoint(p);
            return Playfield[p.Row * Width + p.Col];
        }

        private void ValidatePoint(Point p)
        {
            Debug.Assert(p.Col >= 0 && p.Col < Width);
            Debug.Assert(p.Row >= 0 && p.Row < Height);
        }

        // This is one way - but we could add the cells after if the number of
        // mines is small compared to the field size
        private void GenerateField(Point start, int numBombs)
        {
            List<Point> bombLocations = PlaceBombs(start, numBombs);

            // See all of the locations around the bombs that are not already bombs with the counts
            foreach (var bomb in bombLocations)
            {
                foreach (var index in GetNeighbors(bomb))
                {
                    var cell = Playfield[index];
                    if (cell.Mine == Cell.MineState.None)
                    {
                        ++cell.BombNeighbors;
                    }
                }
            }
        }

        private List<Point> PlaceBombs(Point start, int numBombs)
        {
            List<Point> bombLocations = new List<Point>();

            List<int> exempt = GetNeighbors(start);
            exempt.Add(PointToIndex(start));
            exempt.Sort();

            int fieldSize = Playfield.Count;
            int remainingCells = fieldSize - exempt.Count;

            for (int i = 0; i < fieldSize; ++i)
            {
                Cell cell = Playfield[i];

                // Don't place any bombs immidately around the start
                if (exempt.Contains(i))
                {
                    continue;
                }

                if (random.Next(remainingCells) < numBombs)
                {
                    bombLocations.Add(IndexToPoint(i));
                    cell.SetMine();
                    --numBombs;
                }

                --remainingCells;

            }

            return bombLocations;
        }
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
