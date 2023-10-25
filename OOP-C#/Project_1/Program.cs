using System;
using System.Collections.Generic;
using System.Threading;

public class Vector
{
    public float X { get; set; }
    public float Y { get; set; }

    public Vector(float x, float y)
    {
        X = x;
        Y = y;
    }
}

public class Image
{
    public string[,] Pixels { get; set; }
    public int Width => Pixels.GetLength(1);
    public int Height => Pixels.GetLength(0);

    public Image(string[,] pixels)
    {
        Pixels = pixels;
    }
}

public class Collider
{
    public Vector Position { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    public float LeftEdge => Position.X;
    public float RightEdge => Position.X + Width;
    public float TopEdge => Position.Y;
    public float BottomEdge => Position.Y + Height;

    public Collider(Vector position, int width, int height)
    {
        Position = position;
        Width = width;
        Height = height;
    }

    public bool CollideWith(Collider other)
    {
        return
            RightEdge > other.LeftEdge &&
            LeftEdge < other.RightEdge &&
            BottomEdge > other.TopEdge &&
            TopEdge < other.BottomEdge;
    }
}

public class RectCollider : Collider
{
    public RectCollider(Vector position, int width, int height) : base(position, width, height) { }
}

public class CircleCollider : Collider
{
    public CircleCollider(Vector position, int radius) : base(position, radius * 2, radius * 2) { }
}

public class GameObject
{
    public Vector Position { get; set; }
    public Image? Image { get; set; }
    public Collider? Collider { get; set; }
    public Vector? Speed { get; set; }
    public Vector? MinBounds { get; set; }
    public Vector? MaxBounds { get; set; }
    public Action? OnDestroy { get; set; }

    public GameObject(Vector position)
    {
        Position = position;
    }

    public void Move()
    {
        if (Speed != null)
        {
            Position.X += Speed.X;
            Position.Y += Speed.Y;
        }

        if (MinBounds != null && Image != null)
        {
            if (Position.X + Image.Width < MinBounds.X)
            {
                OnDestroy?.Invoke();
            }
        }
    }

    public bool CollideWith(GameObject other)
    {
        return Collider != null && other.Collider != null && Collider.CollideWith(other.Collider);
    }
}

public class Scene
{
    private int _width;
    private int _height;

    public Scene(int width, int height)
    {
        _width = width;
        _height = height;
    }

    public void Draw(params GameObject[] objects)
    {
        Console.Clear();

        foreach (var obj in objects)
        {
            if (obj.Position.X + (obj.Image?.Width ?? 0) >= 0 && obj.Position.X < _width &&
                obj.Position.Y + (obj.Image?.Height ?? 0) >= 0 && obj.Position.Y < _height)
            {
                for (int y = 0; y < obj.Image.Height; y++)
                {
                    if (obj.Position.Y + y >= 0 && obj.Position.Y + y < _height)
                    {
                        for (int x = 0; x < obj.Image.Width; x++)
                        {
                            if (obj.Position.X + x >= 0 && obj.Position.X + x < _width)
                            {
                                char pixel = obj.Image.Pixels[y, x][0];
                                Console.SetCursorPosition((int)obj.Position.X + x, (int)obj.Position.Y + y);
                                Console.Write(pixel);
                            }
                        }
                    }
                }
            }
        }
    }
}

public class Player : GameObject
{
    private Image? _crouchingImage;
    private Image? _standingImage;
    private Collider? _crouchingCollider;
    private Collider? _standingCollider;
    private bool _crouching;
    private bool _grounded = false;

    // Add timer flag for crouching stand-up
    private bool _isCrouchingTimer = false;

    public Player(Vector position) : base(position)
    {
        _standingImage = new Image(new string[,]
        {
            {" ", " ", " ", " ", "# ", "# ", " ", " "},
            {" ", " ", " ", " ", "# ", "# ", "# ", "# "},
            {" ", " ", " ", " ", "# ", "# ", "# ", "# "},
            {" ", " ", " ", " ", "# ", "# ", " ", " "},
            {" ", " ", " ", "# ", "# ", "# ", " ", " "},
            {" ", " ", "# ", "# ", "# ", "# ", "# ", " "},
            {" ", "# ", "# ", "# ", "# ", "# ", " ", " "},
            {"# ", "# ", "# ", "# ", "# ", "# ", " ", " "},
            {" ", " ", "# ", " ", " ", "# ", " ", " "},
            {" ", " ", "# ", "# ", " ", "# ", "# ", " "}
        });

        _crouchingImage = new Image(new string[,]
        {
            {" ", " ", " ", " ", "# ", "# ", " ", " "},
            {" ", " ", " ", " ", "# ", "# ", "# ", "# "},
            {" ", " ", " ", " ", "# ", "# ", "# ", "# "},
            {" ", " ", " ", "# ", "# ", "# ", " ", " "},
            {" ", " ", "# ", "# ", "# ", "# ", "# ", " "},
            {"# ", "# ", "# ", "# ", "# ", "# ", " ", " "},
            {" ", " ", "# ", "# ", " ", "# ", "# ", " "}
        });

        _standingCollider = new RectCollider(position, _standingImage.Width, _standingImage.Height);
        _crouchingCollider = new RectCollider(position, _crouchingImage.Width, _crouchingImage.Height);

        Image = _standingImage;
        Collider = _standingCollider;
    }

    // Add properties for crouching and crouching timer
    public bool IsCrouching => _crouching;
    public bool IsCrouchingTimer => _isCrouchingTimer;

    public void StandUp()
    {
        if (_crouching)
        {
            Image = _standingImage;
            Collider = _standingCollider;
            _crouching = false;
            Position.Y += (_standingCollider?.BottomEdge ?? 0) - (_crouchingCollider?.BottomEdge ?? 0);
        }
    }

    public void Jump()
    {
        if (_grounded)
        {
            StandUp();
            Speed = new Vector(0, -3);
        }
    }

    public void Crouch()
    {
        if (!_crouching && _grounded)
        {
            Image = _crouchingImage;
            Collider = _crouchingCollider;
            Position.Y += (_standingCollider?.BottomEdge ?? 0) - (_crouchingCollider?.BottomEdge ?? 0);
            _crouching = true;
        }
    }

    public new void Move()
    {
        if (Speed != null)
        {
            Speed.Y += 0.3f;
            base.Move();
        }

        if (Collider != null)
        {
            if (Collider.BottomEdge > (MaxBounds?.Y ?? 0))
            {
                _grounded = true;
                Collider.Position.Y = (MaxBounds?.Y ?? 0) - Collider.Height;
                Speed = new Vector(0, 0);
            }
            else
            {
                _grounded = false;
            }
        }
    }
}

public class Meteor : GameObject
{
    public Meteor(Vector position) : base(position)
    {
        Image = new Image(new string[,]
        {
            {" ", "# ", "# ", "# ", "# ", " ", "# "},
            {"# ", " ", " ", "# ", " ", "# ", " "},
            {"# ", "# ", "# ", " ", " ", "# ", "# "},
            {"# ", " ", " ", "# ", " ", "# ", " "},
            {"# ", " ", " ", " ", "# ", "# ", " "},
            {" ", "# ", "# ", "# ", "# ", " ", "# "}
        });

        Collider = new CircleCollider(position, Image.Width / 2);
        Speed = new Vector(-1.4f, 0);
    }

    public new void Move()
    {
        base.Move();
    }
}

public class Cactus : GameObject
{
    public Cactus(Vector position) : base(position)
    {
        Image = new Image(new string[,]
        {
            { "# ", "# ", " ", "# ", "# " },
            { "# ", "# ", " ", "# ", "# " },
            { "# ", "# ", "# ", "# ", "# " },
            { "# ", "# ", "# ", "# ", " " },
            { " ", "# ", "# ", " ", " " },
            { " ", "# ", "# ", " ", " " }
        });

        Collider = new RectCollider(position, Image.Width, Image.Height);
        Speed = new Vector(-1.4f, 0);
    }

    public new void Move()
    {
        base.Move();
    }
}

public class Program
{
    private static int _sceneWidth = 50;
    private static int _sceneHeight = 30;
    private static Random _random = new Random();

    public static void ProcessInput(Player player)
    {
        if (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true).Key;

            if (key == ConsoleKey.W)
            {
                player.Jump();
            }
            else if (key == ConsoleKey.S)
            {
                player.Crouch();
            }
        }
    }

    private static GameObject RandomObstacle(Vector minBounds, Vector maxBounds, int x)
    {
        if (_random.NextDouble() > 0.5)
        {
            var meteor = new Meteor(new Vector(x, _sceneHeight - 15));
            meteor.MinBounds = minBounds;
            meteor.MaxBounds = maxBounds;
            Action? onObstacleDestroy = null;
            onObstacleDestroy = () =>
            {
                meteor = new Meteor(new Vector(x, _sceneHeight - 15));
                meteor.OnDestroy = onObstacleDestroy;
            };
            meteor.OnDestroy = onObstacleDestroy;
            return meteor;
        }
        else
        {
            var cactus = new Cactus(new Vector(x, _sceneHeight - 6));
            cactus.MinBounds = minBounds;
            cactus.MaxBounds = maxBounds;
            Action? onObstacleDestroy = null;
            onObstacleDestroy = () =>
            {
                cactus = new Cactus(new Vector(x, _sceneHeight - 6));
                cactus.OnDestroy = onObstacleDestroy;
            };
            cactus.OnDestroy = onObstacleDestroy;
            return cactus;
        }
    }

    public static void Main()
    {
        while (true)
        {
            Vector minBounds = new Vector(0, 0);
            Vector maxBounds = new Vector(_sceneWidth, _sceneHeight);
            Scene scene = new Scene(_sceneWidth, _sceneHeight);
            Player player = new Player(new Vector(3, _sceneHeight - 7));
            player.MinBounds = minBounds;
            player.MaxBounds = maxBounds;

            GameObject obstacle1 = RandomObstacle(minBounds, maxBounds, _sceneWidth);
            Action? onObstacle1Destroy = null;
            onObstacle1Destroy = () =>
            {
                obstacle1 = RandomObstacle(minBounds, maxBounds, _sceneWidth);
                obstacle1.OnDestroy = onObstacle1Destroy;
            };
            obstacle1.OnDestroy = onObstacle1Destroy;

            GameObject obstacle2 = RandomObstacle(minBounds, maxBounds, (int)(_sceneWidth * 1.5));
            Action? onObstacle2Destroy = null;
            onObstacle2Destroy = () =>
            {
                obstacle2 = RandomObstacle(minBounds, maxBounds, (int)(_sceneWidth * 1.5));
                obstacle2.OnDestroy = onObstacle2Destroy;
            };
            obstacle2.OnDestroy = onObstacle2Destroy;

            int score = 0;

            Timer crouchingStandUpTimer = null;

            while (true)
            {
                ProcessInput(player);

                if (player.CollideWith(obstacle1) || player.CollideWith(obstacle2))
                {
                    Console.SetCursorPosition(0, _sceneHeight);
                    Console.Write("You lost! Press 'R' to restart.");
                    while (true)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            if (key == ConsoleKey.R)
                            {
                                break; // Restart the game
                            }
                        }
                    }
                    break; // Exit the game over screen
                }

                player.Move();
                obstacle1.Move();
                obstacle2.Move();

                scene.Draw(player, obstacle1, obstacle2);

                score++;
                Console.SetCursorPosition(0, 0);
                Console.Write("Score: " + score);

                crouchingStandUpTimer?.Dispose();

                if (player.IsCrouching && !player.IsCrouchingTimer)
                {
                    crouchingStandUpTimer = new Timer(_ =>
                    {
                        player.StandUp();
                    }, null, 500, Timeout.Infinite);
                }

                Thread.Sleep(100);
            }
        }
    }
}