using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minesweeper
{
    class Cell : INotifyPropertyChanged
    {
        internal enum ViewState
        {
            Hidden,
            Visible
        }

        internal enum FlagState
        {
            None,
            Flagged,
            QuestionMark,
        }

        internal enum MineState
        {
            None,
            Bomb
        }

        public event PropertyChangedEventHandler PropertyChanged;
        internal ViewState View { get; set; }
        internal MineState Mine { get; private set; }
        internal FlagState Flag { get; set; }
        internal int BombNeighbors { get; set; }
        internal int Index { get; }

        internal Cell(int index = 0)
        {
            View = ViewState.Hidden;
            Mine = MineState.None;
            Index = index;
        }

        internal void SetMine()
        {
            Mine = MineState.Bomb;
        }

        internal void SetVisible()
        {
            if (View == ViewState.Hidden && Flag == FlagState.None)
            {
                //Trace.WriteLine($"cell[{Index}] is visible");
                View = ViewState.Visible;
                OnPropertyChanged(nameof(View));
            }
        }

        internal void ClearFlag()
        {
            Flag = FlagState.None;
        }

        internal void ToggleFlag()
        {
            if (View == ViewState.Visible)
            {
                return;
            }

            switch(Flag)
            {
                case FlagState.None:
                    Flag = FlagState.Flagged;
                    break;

                case FlagState.Flagged:
                    Flag = FlagState.QuestionMark;
                    break;

                case FlagState.QuestionMark:
                    Flag = FlagState.None;
                    break;
            }

            OnPropertyChanged(nameof(Flag));
        }

        internal bool Trigger()
        {
            if(Flag == FlagState.None)
            {
                SetVisible();
            }

            return Flag == FlagState.None && Mine == MineState.Bomb;
        }

        public override string ToString()
        {
            //return string.Format($"({Mine}, {BombNeighbors})");

            return string.Format("({0}{1}{2})", View == ViewState.Visible ? 'V' : 'H', Mine == MineState.Bomb ? '*' : ' ', BombNeighbors);
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
