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

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for Playfield.xaml
    /// </summary>
    public partial class Playfield : UserControl
    {
        List<Cell> cells = new List<Cell>();
        private Minefield game;
        private int bombCount = 10;


        public Playfield()
        {
            InitializeComponent();

            CreateMineField();
        }

        private void CreateMineField()
        {
            FinalScreen.Visibility = Visibility.Hidden;

            ClearMinefield();
            int index = 0;

            cells = new List<Cell>();
            foreach (GameCell child in Field.Children)
            {
                var cell = new Cell(index++);
                child.GameElement = cell;
                cells.Add(cell);
                child.OnSelected += Child_OnSelected;
            }
        }

        private void ClearMinefield()
        {
            game = null;
            foreach (GameCell child in Field.Children)
            {
                child.OnSelected -= Child_OnSelected;
                child.GameElement = null;
            }
        }

        private void Child_OnSelected(object sender, EventArgs e)
        {
            var cell = sender as GameCell;

            if (game == null)
            {
                game = new Minefield(cells, Field.Columns, cell.GameElement.Index, bombCount);
                game.PropertyChanged += Game_PropertyChanged;
            }

            game.SelectIndex(cell.GameElement.Index);
        }

        private void Game_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Debug.Assert(sender == game);
            Debug.Assert(e.PropertyName == nameof(Minefield.State));

            // Show all the cells
            foreach (GameCell child in Field.Children)
            {
                child.GameElement.SetVisible();
            }

            string result = $"Game {(sender as Minefield).State}";
            Result.Text = result;

            FinalScreen.Visibility = Visibility.Visible;

            Debug.WriteLine(result);
        }

        private void FinalScreen_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                CreateMineField();
            }
        }
    }
}
