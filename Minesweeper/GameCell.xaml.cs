using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for GameCell.xaml
    /// </summary>
    public partial class GameCell : UserControl
    {
        public event EventHandler OnSelected;

        private bool _Pressed;
        private bool Pressed
        {
            get { return _Pressed; }
            set
            {
                if (value != _Pressed)
                {
                    _Pressed = value;
                    UpdateDisplay();
                }
            }
        }

        private bool Captured = false;
        private Cell _GameElement;
        internal Cell GameElement
        {
            get { return _GameElement; }
            set
            {
                if (_GameElement != null)
                {
                    _GameElement.PropertyChanged -= GameElement_PropertyChanged;
                }

                _GameElement = value;
                if (_GameElement != null)
                {
                    _GameElement.PropertyChanged += GameElement_PropertyChanged;
                }

                UpdateDisplay();
            }
        }

        internal Cell.ViewState? VisState
        {
            get { return _GameElement?.View; }
        }


        public GameCell()
        {
            InitializeComponent();
        }

        private void UpdateDisplay()
        {
            if (GameElement == null)
            {
                Text.Text = "H";
                return;
            }

            if(GameElement.AI == Cell.AiState.Thinking)
            {
                Text.Background = Brushes.Cornsilk;
            }
            else
            {
                Text.Background = Brushes.Black;
            }

            if (Pressed)
            {
                Text.Text = "P";
                return;
            }

            switch (VisState)
            {
                case Cell.ViewState.Hidden:
                    Text.Foreground = Brushes.Red;
                    DisplayHiddenState();
                    break;

                case Cell.ViewState.Visible:
                    DisplayVisibleState();
                    break;

                default:
                    break;
            }
        }

        private void DisplayVisibleState()
        {
            if (GameElement.Mine == Cell.MineState.Bomb)
            {
                Text.Text = "*";
            }
            else if (GameElement.BombNeighbors > 0)
            {
                Text.Foreground = Brushes.LightSkyBlue;
                Text.Text = GameElement.BombNeighbors.ToString();
            }
            else
            {
                Text.Text = "";
            }
        }

        private void DisplayHiddenState()
        {
            var brush = Brushes.LightGreen;
            switch (_GameElement?.Flag)
            {
                case Cell.FlagState.Flagged:
                    Text.Text = "F";
                    break;
                case Cell.FlagState.QuestionMark:
                    Text.Text = "?";
                    break;
                default:
                    Text.Text = "H";
                    brush = Brushes.Red;
                    break;
            }

            Text.Foreground = brush;
        }

        private void GameElement_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(()=> UpdateDisplay()));
        }

        private void GameCell_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Captured = Mouse.Capture(sender as IInputElement);

            if (VisState != Cell.ViewState.Hidden)
            {
                return;
            }

            Pressed = true;
        }

        private void GameCell_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            Mouse.Capture(null);
            Captured = false;

            if (Pressed)
            {
                Pressed = false;
                GameElement.SetVisible();
                OnSelected?.Invoke(this, new EventArgs());
            }
        }

        private void GameCell_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Captured && VisState == Cell.ViewState.Hidden)
            {
                Pressed = true;
            }
        }

        private void GameCell_MouseLeave(object sender, MouseEventArgs e)
        {
            if (Captured)
            {
                Pressed = false;
            }
        }

        private void Grid_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
        }

        private void Grid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            GameElement.ToggleFlag();
        }
    }
}
