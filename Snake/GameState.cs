using System.IO;
using System.Text;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;


namespace Snake
{
    public class GameState
    {
        public int Rows { get; }
        public int Cols { get; }
        public GridValue[,] Grid { get; }
        public Direction Dir { get; private set; }
        public int Score { get; private set; }
        private int gameTimeInSeconds;
        private DispatcherTimer timer;
        public int Lives { get; private set; }
        private bool isImmune;
        private readonly int immuneTime = 200;
        private int immuneTimer;
        private int delay = 45;

        //public int HighestScore { get; set; }

        public GameMode Mode { get; set; }

        private readonly LinkedList<Direction> dirChanges = new();
        private readonly LinkedList<Position> snakePositions = new();
        private readonly Random random = new Random();

        public GameState(int rows, int cols, int lives, int gameTimeInSeconds)
        {
            Rows = rows;
            Cols = cols;
            Grid = new GridValue[Rows, Cols];
            Dir = Direction.right;
            Lives = lives;
            
            this.gameTimeInSeconds = gameTimeInSeconds;
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            timer.Start();

            AddSnake();
            AddFood();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            // Giảm thời gian đi 1 giây
            gameTimeInSeconds--;

            // Kiểm tra nếu thời gian đã hết, kết thúc trò chơi
            if (gameTimeInSeconds == 0)
            {
                Mode = GameMode.Over;
            }
        }

        private void AddSnake()
        {
            int r = random.Next(0, Rows);
            int temp = random.Next(3, Cols - 3);
            for (int c  = temp; c <= temp + 2; c++)
            {
                Grid[r, c] = GridValue.Snake;
                snakePositions.AddFirst(new Position(r, c));
            }
        }

        private IEnumerable<Position> EmptyPositions()
        {
            for (int r = 0; r < Rows; r++)
            {
                for(int c = 0; c < Cols; c++)
                {
                    if (Grid[r,c] == GridValue.Empty)
                    {
                        yield return new Position(r, c);
                    }
                }
            }
        }

        private void AddFood()
        {
            List<Position> empty = new List<Position>(EmptyPositions());
            if (empty.Count == 0)
            {
                return;
            }
            Position pos = empty[random.Next(empty.Count)];
            Grid[pos.Row, pos.Col] = GridValue.Food;
        }

        public Position HeadPosition()
        {
            return snakePositions.First.Value;
        }

        public Position TailPosition() 
        {
            return snakePositions.Last.Value;
        }

        public IEnumerable<Position> SnakePosition()
        {
            return snakePositions;
        }

        private void AddHead(Position pos)
        {
            snakePositions.AddFirst(pos);
            Grid[pos.Row, pos.Col] = GridValue.Snake;
        }

        private void RemoveTail()
        {
            Position tail = snakePositions.Last.Value;
            Grid[tail.Row, tail.Col] = GridValue.Empty;
            snakePositions.RemoveLast();
        }

        private Direction GetLastDirection()
        {
            if (dirChanges.Count == 0)
            {
                return Dir;
            }
            return dirChanges.Last.Value;
        }

        private bool CanChangeDirection(Direction newDir)
        {
            if (dirChanges.Count == 2)
            {
                return false;
            }
            Direction lastDir = GetLastDirection();
            return newDir != lastDir && newDir != lastDir.Opposite();
        }
        public void ChangeDirection(Direction dir)
        {
            if (CanChangeDirection(dir))
            {
                dirChanges.AddLast(dir);
            }
        }

        private bool OutsideGrid(Position pos)
        {
            return pos.Row < 0 || pos.Row >= Rows || pos.Col < 0 || pos.Col >= Cols;
        }

        private GridValue WillHit(Position newHeadPos)
        {
            if (OutsideGrid(newHeadPos))
            {
                return GridValue.Outside;
            }

            if (newHeadPos == TailPosition()){
                return GridValue.Empty;
            }
            
            return Grid[newHeadPos.Row, newHeadPos.Col];
        }

        public void Move()
        {

            if (dirChanges.Count > 0)
            {
                Dir = dirChanges.First.Value;
                dirChanges.RemoveFirst();
            }

            Position newHeadPos = HeadPosition().Translate(Dir);
            GridValue hit = WillHit(newHeadPos);

            if (isImmune)
            {
                immuneTimer -= delay;
                if (immuneTimer <= 0)
                {
                    isImmune = false;
                }
            }

            if (hit == GridValue.Outside || hit == GridValue.Snake)
            {
                if (!isImmune)
                {
                    Lives--;
                    isImmune = true;
                    immuneTimer = immuneTime;
                    if (Lives <= 0)
                    {
                        Mode = GameMode.Over;
                    }
                }
            }
            else if(hit == GridValue.Empty)
            {
                RemoveTail();
                AddHead(newHeadPos);
            }
            else if(hit == GridValue.Food)
            {
                AddHead(newHeadPos);
                Score++;
                AddFood();
            }
        }
    }
}
