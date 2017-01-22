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

namespace Minesweeper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Minefield Mines;

        public MainWindow()
        {
            InitializeComponent();
            Game.OnNewGame += Game_OnNewGame;
            ShowMenu();
        }

        private void CreatePlayfield(int rows, int cols, int numBombs)
        {
            Mines = new Minefield(cols, rows, numBombs);
            Game.Initialize(Mines);
            Game.Visibility = Visibility.Visible;
            Menu.Visibility = Visibility.Hidden;
        }

        private void ShowMenu()
        {
            Game.Visibility = Visibility.Hidden;
            Menu.Visibility = Visibility.Visible;
        }

        private void Game_OnNewGame(object sender, EventArgs e)
        {
            ShowMenu();
        }

        private void Button_Click9x9(object sender, RoutedEventArgs e)
        {
            CreatePlayfield(9, 9, 10);
        }

        private void Button_Click_16x16(object sender, RoutedEventArgs e)
        {
            CreatePlayfield(16, 16, 40);
        }

        private void Button_Click_30x16(object sender, RoutedEventArgs e)
        {
            CreatePlayfield(30, 16, 99);
        }
    }
}
