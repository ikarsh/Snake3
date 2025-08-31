namespace Snake3;

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using Microsoft.VisualBasic.Logging;
using System.Diagnostics;
using System.CodeDom;
using System.Runtime.InteropServices.Swift;

static class Config
{
    public const int SEG_SIZE = 20;
    public const int GRID_WID = 40;
    public const int GRID_HEI = 30;
    public const int TIMER_INTERVAL = 100;
}


enum Direction { Right, Left, Up, Down }

static class DirectionEx
{
    public static Direction Opposite(this Direction dir)
    {
        return dir switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new UnreachableException()
        };
    }

    public static Point OffsetPoint(this Direction dir, Point point)
    {
        switch (dir)
        {
            case Direction.Right:
                point.Offset(Config.SEG_SIZE, 0);
                break;
            case Direction.Left:
                point.Offset(-Config.SEG_SIZE, 0);
                break;
            case Direction.Up:
                point.Offset(0, -Config.SEG_SIZE);
                break;
            case Direction.Down:
                point.Offset(0, Config.SEG_SIZE);
                break;
        }
        return point;
    }
}

static class Rand
{
    private static Random rand = new Random();
    public static Point RandomPoint()
    {
        return new Point(Config.SEG_SIZE * rand.Next(0, Config.GRID_WID),
        Config.SEG_SIZE * rand.Next(Config.GRID_HEI));
    }

    public static Direction RandomDirection()
    {
        switch (rand.Next(0, 4))
        {
            case 0:
                return Direction.Down;
            case 1:
                return Direction.Left;
            case 2:
                return Direction.Right;
            case 3:
                return Direction.Up;
            default:
                throw new UnreachableException();
        }
    }
}

abstract class Artifact
{
    public abstract void Paint(Graphics g);
}

class Snake : Artifact
{
    private List<Point> points;
    private Direction direction;
    private bool justAte = false;
    private bool justChangedDirection = false;
    public Color color;

    public Snake(Color c, Point p, Direction d)
    {
        points = new List<Point> { p };
        direction = d;
        color = c;
    }

    public void ChangeDirection(Direction dir)
    {
        if (justChangedDirection || direction == dir || direction == dir.Opposite()) return;

        justChangedDirection = true;
        direction = dir;
    }

    public void Update(Game g)
    {
        // Point head = new Point(points[0].X, points[0].Y);
        justChangedDirection = false;
        Point head = direction.OffsetPoint(points[0]);

        if (
            head.X < 0 || head.X > Config.SEG_SIZE * Config.GRID_WID ||
            head.Y < 0 || head.Y > Config.SEG_SIZE * Config.GRID_HEI
        )
        {
            g.Death();
            return;
        }

        Artifact? a = g.ArtifactAt(head);
        if (a is Food f && f.color == color)
        {
            points.Insert(0, head);
            f.Regenerate(g);
            justAte = true;
        }
        else if (a is not null)
        {
            g.Death();
            return;
        }
        else
        {
            points.Insert(0, head);
        }

        if (!justAte)
        {
            points.RemoveAt(points.Count() - 1);
        }
        justAte = false;
    }

    public bool CollidingWalls()
    {
        Point head = points[0];
        return (
            head.X < 0 || head.X > Config.SEG_SIZE * Config.GRID_WID ||
            head.Y < 0 || head.Y > Config.SEG_SIZE * Config.GRID_HEI
        );
    }

    public bool CollidingSnake(Snake other)
    {
        Point head = points[0];
        return other.ContainsPoint(head);
    }

    public bool ContainsPoint(Point p)
    {
        return points.Contains(p);
    }

    public override void Paint(Graphics g)
    {
        foreach (Point segment in points)
        {
            g.FillRectangle(new SolidBrush(color), new Rectangle(segment, new Size(Config.SEG_SIZE, Config.SEG_SIZE)));
        }
    }

    public int Length()
    {
        return points.Count();
    }
}

class Food : Artifact
{
    public Color color;
    public Point position;
    public Food(Color c, Point p)
    {
        color = c;
        position = p;
    }

    public override void Paint(Graphics g)
    {
        g.FillRectangle(new SolidBrush(color), new Rectangle(position + new Size(Config.SEG_SIZE / 4, Config.SEG_SIZE / 4), new Size(Config.SEG_SIZE / 4, Config.SEG_SIZE / 4)));
    }

    public void Regenerate(Game g)
    {
        position = g.EmptyPoint();
    }
}
class Game : Form
{
    private Timer timer;
    private List<Snake> snakes;
    private List<Food> foods;
    private bool isGameOver;

    public Game()
    {
        Text = "Snake game";
        ClientSize = new Size(
            Config.SEG_SIZE * (Config.GRID_WID + 1),
            Config.SEG_SIZE * (Config.GRID_HEI + 1)
        );
        timer = new Timer();
        timer.Interval = Config.TIMER_INTERVAL;
        timer.Tick += (_, _) =>
        {
            GameUpdate();
            Invalidate();
        };

        snakes = new List<Snake>();
        foods = new List<Food>();
        SetHighscore(0);
        StartGame();
        timer.Start();

        KeyPreview = true;
        KeyDown += (object? sender, KeyEventArgs e) =>
        {
            if (e.KeyCode == Keys.Q)
            {
                Application.Exit();
            }
        };
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        SetHighscore(0);
    }

    private void StartGame()
    {

        snakes = new List<Snake>
        {
            new Snake(Color.Red, new Point(10 * Config.SEG_SIZE, 10 * Config.SEG_SIZE), Direction.Right),
            new Snake(Color.Blue, new Point((Config.GRID_WID - 10) * Config.SEG_SIZE, (Config.GRID_HEI - 10) * Config.SEG_SIZE), Direction.Left)
        };
        foods = new List<Food>
        {
            new Food(Color.Red, new Point((Config.GRID_WID - 10) * Config.SEG_SIZE, 10 * Config.SEG_SIZE)),
            new Food(Color.Blue, new Point(10 * Config.SEG_SIZE, (Config.GRID_HEI - 10) * Config.SEG_SIZE)),
        };
        isGameOver = false;

        RegisterKeys(snakes[1], new Tuple<Keys, Keys, Keys, Keys>(Keys.W, Keys.S, Keys.D, Keys.A));
        RegisterKeys(snakes[0], new Tuple<Keys, Keys, Keys, Keys>(Keys.Up, Keys.Down, Keys.Right, Keys.Left));
    }

    public void Death()
    {
        isGameOver = true;
        int score = snakes[0].Length() * snakes[1].Length();
        int highscore = ReadHighscore();

        if (score > highscore)
        {
            SetHighscore(score);
            highscore = score;
        }
        
        MessageBox.Show($"You Lost! \nScore: {score}\nHighscore: {highscore}");
        StartGame();
    }

    private int ReadHighscore()
    {
        return int.Parse(File.ReadAllText("highscore.txt"));
    }

    private void SetHighscore(int score)
    {
        File.WriteAllText("highscore.txt", score.ToString());
    }

    public Artifact? ArtifactAt(Point p)
    {
        foreach (Snake snake in snakes)
        {
            if (snake.ContainsPoint(p)) return snake;
        }
        foreach (Food food in foods)
        {
            if (food.position == p) return food;
        }
        return null;
    }

    public bool IsEmpty(Point p)
    {
        return ArtifactAt(p) == null;
    }

    public Point EmptyPoint() {
        Point p;
        do
        {
            p = Rand.RandomPoint();
        } while (!IsEmpty(p));
        return p;
    }

    private void GameUpdate()
    {
        if (isGameOver) return;

        foreach (Snake snake in snakes)
        {
            snake.Update(this);
        }
    }

    private void RegisterKeys(Snake snake, Tuple<Keys, Keys, Keys, Keys> controls) {
        KeyDown += (object? sender, KeyEventArgs e) =>
        {
            if (e.KeyCode == controls.Item1)
            {
                snake.ChangeDirection(Direction.Up);
            }
            else if (e.KeyCode == controls.Item2)
            {
                snake.ChangeDirection(Direction.Down);
            }
            else if (e.KeyCode == controls.Item3)
            {
                snake.ChangeDirection(Direction.Right);
            }
            else if (e.KeyCode == controls.Item4)
            {
                snake.ChangeDirection(Direction.Left);
            }
        };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        foreach (Artifact a in foods.Cast<Artifact>().Concat(snakes.Cast<Artifact>()))
        {
            a.Paint(e.Graphics);
        }
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







