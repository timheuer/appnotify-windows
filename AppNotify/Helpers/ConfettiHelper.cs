using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;

namespace AppNotify.Helpers;

public sealed class ConfettiHelper
{
    private readonly Canvas _canvas;
    private readonly DispatcherTimer _timer;
    private readonly List<ConfettiPiece> _pieces = new();
    private readonly Random _rng = new();
    private int _tickCount;

    public event Action? Completed;

    private static readonly Windows.UI.Color[] Colors =
    [
        Windows.UI.Color.FromArgb(255, 255, 45, 85),   // pink
        Windows.UI.Color.FromArgb(255, 88, 86, 214),   // purple
        Windows.UI.Color.FromArgb(255, 0, 199, 190),   // teal
        Windows.UI.Color.FromArgb(255, 255, 204, 0),   // yellow
        Windows.UI.Color.FromArgb(255, 255, 149, 0),   // orange
        Windows.UI.Color.FromArgb(255, 52, 199, 89),   // green
        Windows.UI.Color.FromArgb(255, 0, 122, 255),   // blue
        Windows.UI.Color.FromArgb(255, 255, 59, 48),   // red
    ];

    public ConfettiHelper(Canvas canvas)
    {
        _canvas = canvas;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) }; // ~60fps
        _timer.Tick += OnTick;
    }

    public void Fire()
    {
        _canvas.Visibility = Visibility.Visible;
        _tickCount = 0;

        double w = _canvas.ActualWidth > 0 ? _canvas.ActualWidth : 400;
        double h = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 520;

        // Scale particle count based on screen size
        int count = w > 800 ? 200 : 80;

        for (int i = 0; i < count; i++)
        {
            var color = Colors[_rng.Next(Colors.Length)];
            bool isRect = _rng.Next(2) == 0;
            var shape = isRect
                ? (FrameworkElement)new Rectangle
                {
                    Width = _rng.Next(6, 12),
                    Height = _rng.Next(4, 8),
                    Fill = new SolidColorBrush(color),
                    RadiusX = 1,
                    RadiusY = 1,
                }
                : new Ellipse
                {
                    Width = _rng.Next(5, 10),
                    Height = _rng.Next(5, 10),
                    Fill = new SolidColorBrush(color),
                };

            var piece = new ConfettiPiece
            {
                Element = shape,
                X = _rng.NextDouble() * w,
                Y = -_rng.NextDouble() * h * 0.3 - 10,
                VelocityX = (_rng.NextDouble() - 0.5) * 4,
                VelocityY = _rng.NextDouble() * 2 + 1.5,
                RotationSpeed = (_rng.NextDouble() - 0.5) * 8,
                Rotation = _rng.NextDouble() * 360,
                Opacity = 1.0,
            };

            Canvas.SetLeft(shape, piece.X);
            Canvas.SetTop(shape, piece.Y);
            _canvas.Children.Add(shape);
            _pieces.Add(piece);
        }

        _timer.Start();
    }

    private void OnTick(object? sender, object e)
    {
        _tickCount++;
        double h = _canvas.ActualHeight > 0 ? _canvas.ActualHeight : 520;
        bool allDone = true;

        foreach (var p in _pieces)
        {
            p.VelocityY += 0.08; // gravity
            p.X += p.VelocityX;
            p.Y += p.VelocityY;
            p.Rotation += p.RotationSpeed;
            p.VelocityX *= 0.99; // air resistance

            // Fade out near bottom
            if (p.Y > h * 0.7)
                p.Opacity = Math.Max(0, 1.0 - (p.Y - h * 0.7) / (h * 0.3));

            if (p.Opacity > 0 && p.Y < h + 20)
            {
                allDone = false;
                Canvas.SetLeft(p.Element, p.X);
                Canvas.SetTop(p.Element, p.Y);
                p.Element.Opacity = p.Opacity;
                p.Element.RenderTransform = new RotateTransform { Angle = p.Rotation };
            }
            else
            {
                p.Element.Opacity = 0;
            }
        }

        if (allDone || _tickCount > 300) // ~5 seconds max
        {
            _timer.Stop();
            _canvas.Children.Clear();
            _pieces.Clear();
            _canvas.Visibility = Visibility.Collapsed;
            Completed?.Invoke();
        }
    }

    private class ConfettiPiece
    {
        public FrameworkElement Element { get; set; } = null!;
        public double X { get; set; }
        public double Y { get; set; }
        public double VelocityX { get; set; }
        public double VelocityY { get; set; }
        public double Rotation { get; set; }
        public double RotationSpeed { get; set; }
        public double Opacity { get; set; }
    }
}
