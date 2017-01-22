using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Windows.Threading;

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for Playfield.xaml
    /// </summary>
    public partial class Playfield : UserControl
    {
        private Minefield game;
        public event EventHandler OnNewGame;

        public Playfield()
        {
            InitializeComponent();
        }

        private void ClearMinefield()
        {
            FinalScreen.Visibility = Visibility.Hidden;
            Field.Visibility = Visibility.Visible;

            if (game != null)
            {
                game.PropertyChanged -= Game_PropertyChanged;
                game = null;
            }

            foreach (GameCell child in Field.Children)
            {
                child.OnSelected -= Child_OnSelected;
                child.GameElement = null;
            }
        }

        internal void Initialize(Minefield mines)
        {
            ClearMinefield();

            Field.Rows = mines.Height;
            Field.Columns = mines.Width;
            game = mines;
            game.PropertyChanged += Game_PropertyChanged;
            int cellCount = mines.Height * mines.Width;
            int i = 0;
            for (; i < cellCount && i < Field.Children.Count; ++i)
            {
                GameCell cell = Field.Children[i] as GameCell;
                cell.GameElement = game.Playfield[i];
                cell.OnSelected += Child_OnSelected;
            }

            for (; i < cellCount; ++i)
            {
                GameCell cell = new GameCell()
                {
                    GameElement = game.Playfield[i]
                };
                Field.Children.Add(cell);
                cell.OnSelected += Child_OnSelected;
            }
        }

        private void Child_OnSelected(object sender, EventArgs e)
        {
            var cell = sender as GameCell;

            game.SelectIndex(cell.GameElement.Index);
        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.Assert(sender == game);
            Debug.Assert(e.PropertyName == nameof(Minefield.State));

            // Show all the cells
            foreach (GameCell child in Field.Children)
            {
                child.GameElement?.SetVisible();
            }

            string result = $"Game {(sender as Minefield).State}";
            Result.Text = result;

            FinalScreen.Visibility = Visibility.Visible;

            Debug.WriteLine(result);
        }

        private void Button_NewGame(object sender, RoutedEventArgs e)
        {
            OnNewGame?.Invoke(this, new EventArgs());
            ClearMinefield();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.Dispatcher.InvokeAsync(() =>
            {
                for (int i = 0; i < 500; ++i)
                {
                    GameCell cell = new GameCell();
                    Field.Children.Add(cell);
                }
            }, DispatcherPriority.Loaded);
        }
    }
}
