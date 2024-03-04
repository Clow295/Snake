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

namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly int msGameLoop = 250;
        private readonly int rows = 15, cols = 15;
        private readonly Image[,] gridImages;
        private GameState gameState;

        private readonly Dictionary<GridValue, ImageSource> gridValToImage = new()
        {
            {GridValue.Empty, Images.Empty },
            {GridValue.Snake, Images.Body },
            {GridValue.Food, Images.Food },
        };

        private readonly Dictionary<Direction, int> dirToRotation = new()
        {
            {Direction.up, 0 },
            {Direction.right, 90 },
            {Direction.down, 180 },
            {Direction.left, 270 }
        };

        public MainWindow()
        {
            InitializeComponent();
            gridImages = SetupGrid();
            gameState = new GameState(rows, cols);
        }

        private async Task RunGame()
        {
            Draw();
            await ShowCountDown();
            Overlay.Visibility = Visibility.Hidden;
            await GameLoop();
            await ShowGameOver();
            gameState = new GameState(rows, cols);
        }

        private async void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Overlay.Visibility == Visibility.Visible && gameState.Mode != GameMode.Paused)
            {
                e.Handled = true;
            }
            if(gameState.Mode == GameMode.NotStarted)
            {
                gameState.Mode = GameMode.Started;
                await RunGame();
                gameState.Mode = GameMode.NotStarted;
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if(gameState.Mode == GameMode.NotStarted)
            {
                return;
            }

            if(gameState.Mode == GameMode.Over)
            {
                return;
            }

            if(gameState.Mode == GameMode.Started && e.Key == Key.Space)
            {
                gameState.Mode = GameMode.Paused;
                return;
            }

            if(gameState.Mode == GameMode.Paused && e.Key == Key.Space)
            {
                gameState.Mode = GameMode.Resuming;
                return;
            }

            switch (e.Key)
            {
                case Key.A:
                case Key.Left:
                    gameState.ChangeDirection(Direction.left);
                    break;
                case Key.D:
                case Key.Right:
                    gameState.ChangeDirection(Direction.right);
                    break;
                case Key.W:
                case Key.Up:
                    gameState.ChangeDirection(Direction.up);
                    break;
                case Key.S:
                case Key.Down:
                    gameState.ChangeDirection(Direction.down);
                    break;
            }
        }

        private async Task GameLoop()
        {
            while (gameState.Mode != GameMode.NotStarted && gameState.Mode != GameMode.Over)
            {
                await Task.Delay(msGameLoop);
                if(gameState.Mode == GameMode.Started)
                {
                    gameState.Move();
                    Draw();
                }
                if(gameState.Mode == GameMode.Paused)
                {
                    Overlay.Visibility = Visibility.Visible;
                    OverlayText.Text = "Pause";
                }
                else if(gameState.Mode == GameMode.Resuming)
                {
                    Overlay.Visibility= Visibility.Visible;
                    for (int i = 5; i >= 1; i--)
                    {
                        OverlayText.Text = $"Resuming in {i}";
                        await Task.Delay(1000);
                    }
                    Overlay.Visibility = Visibility.Hidden;
                    gameState.Mode = GameMode.Started;
                }
            }
        }
        private Image[,] SetupGrid()
        {
            Image[,] images = new Image[rows, cols];
            GameGrid.Rows = rows;
            GameGrid.Columns = cols;
            GameGrid.Width = GameGrid.Height * (cols / (double)rows);

            for(int r = 0; r < rows; r++)
            {
                for(int c = 0; c < cols; c++)
                {
                    Image image = new Image
                    {
                        Source = Images.Empty,
                        RenderTransformOrigin = new Point(0.5, 0.5)
                    };

                    images[r, c] = image;
                    GameGrid.Children.Add(image);
                }
            }
            return images;
        }

        private void Draw()
        {
            DrawGrid();
            DrawSnakeHead();
            ScoreText.Text = $"SCORE {gameState.Score}";
        }

        private void DrawGrid()
        {
            for (int r = 0;r < rows; r++)
            {
                for( int c = 0; c < cols; c++)
                {
                    GridValue gridVal = gameState.Grid[r, c];
                    gridImages[r, c].Source = gridValToImage[gridVal];
                    gridImages[r, c].RenderTransform = Transform.Identity;
                }
            }
        }

        private void DrawSnakeHead()
        {
            Position headPos = gameState.HeadPosition();
            Image image = gridImages[headPos.Row, headPos.Col];
            image.Source = Images.Head;

            int rotation = dirToRotation[gameState.Dir];
            image.RenderTransform = new RotateTransform(rotation);
        }

        private async Task DrawDeadSnake()
        {
            List<Position> positions = new List<Position>(gameState.SnakePosition());
            for (int i = 0; i < positions.Count; i++)
            {
                Position pos = positions[i];
                ImageSource source = (i == 0) ? Images.DeadHead : Images.DeadBody;
                gridImages[pos.Row, pos.Col].Source = source;
                await Task.Delay(100);
            }
        }

        private async Task ShowCountDown()
        {
            for (int i = 5; i >= 1; i--)
            {
                OverlayText.Text = $"{i}";
                await Task.Delay(1000);
            }
        }

        private async Task ShowGameOver()
        {
            await DrawDeadSnake();
            await Task.Delay(1000);
            Overlay.Visibility = Visibility.Visible;
            OverlayText.Text = "Press any key to start";
            gameState.Mode = GameMode.NotStarted;
        }
    }
}