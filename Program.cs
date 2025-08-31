namespace Snake3;

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

enum Direction {Right, Left, Up, Down}

static class GameConstants
{
    public const int SEG_SIZE = 20;
    public const int GRID_WID = 40;
    public const int GRID_HEI = 30;
    public const int TIMER_INTERVAL = 100;
}


class Game : Form
{
    private Timer timer;
    private List<Point> snake;
    private Point food;
    private Direction direction;
    private bool isGameOver;

    private bool movedThisUpdate = false;

    public Game()
    {
        Text = "Snake game";
        ClientSize = new Size(
            GameConstants.SEG_SIZE * GameConstants.GRID_WID,
            GameConstants.SEG_SIZE * GameConstants.GRID_HEI
        );
        timer = new Timer();
        timer.Interval = GameConstants.TIMER_INTERVAL;
        timer.Tick += (_, _) =>
        {
            update();
            Invalidate();
        };
        snake = new List<Point>();
        startGame();
        timer.Start();

        KeyPreview = true;
        KeyDown += OnKeyDown;
    }

    private void startGame()
    {
        snake.Clear();
        snake.Add(new Point(100, 100));
        generateFood();
        direction = Direction.Right;
        movedThisUpdate = false;
        isGameOver = false;
    }

    private void generateFood()
    {
        Random rand = new Random();
        do
        {
            food = new Point(
                GameConstants.SEG_SIZE * rand.Next(0, GameConstants.GRID_WID),
                GameConstants.SEG_SIZE * rand.Next(0, GameConstants.GRID_HEI)
            );
        } while (snake.Contains(food));
    }

    private void update()
    {
        if (isGameOver) return;
        movedThisUpdate = false;

        Point head = new Point(snake[0].X, snake[0].Y);
        switch (direction)
        {
            case Direction.Right:
                head.Offset(GameConstants.SEG_SIZE, 0);
                break;
            case Direction.Left:
                head.Offset(-GameConstants.SEG_SIZE, 0);
                break;
            case Direction.Up:
                head.Offset(0, -GameConstants.SEG_SIZE);
                break;
            case Direction.Down:
                head.Offset(0, GameConstants.SEG_SIZE);
                break;
        }

        if (head.X < 0 || head.X > GameConstants.SEG_SIZE * GameConstants.GRID_WID ||
        head.Y < 0 || head.Y > GameConstants.SEG_SIZE * GameConstants.GRID_HEI ||
        snake.Contains(head)
        )
        {
            isGameOver = true;
            MessageBox.Show($"You Lost! Length: {snake.Count()}");
            startGame();
            return;
        }

        snake.Insert(0, head);

        if (head == food)
        {
            generateFood();
        }
        else
        {
            snake.RemoveAt(snake.Count() - 1);
        }
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (movedThisUpdate) return;
        movedThisUpdate = true;
        switch (e.KeyCode)
        {
            case Keys.Up:
                if (direction != Direction.Down) direction = Direction.Up;
                break;
            case Keys.Down:
                if (direction != Direction.Up) direction = Direction.Down;
                break;
            case Keys.Left:
                if (direction != Direction.Right) direction = Direction.Left;
                break;
            case Keys.Right:
                if (direction != Direction.Left) direction = Direction.Right;
                break;

        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        foreach (Point segment in snake)
        {
            e.Graphics.FillRectangle(Brushes.Green, new Rectangle(segment, new Size(GameConstants.SEG_SIZE, GameConstants.SEG_SIZE)));
        }

        e.Graphics.FillRectangle(Brushes.Red, new Rectangle(food, new Size(GameConstants.SEG_SIZE, GameConstants.SEG_SIZE)));

    }
}


static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Game());
    }
}







