using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SpaceShooter_2021_12_08.Form1;

namespace SpaceShooter_2021_12_08
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            timer.Interval = 30;
            timer.Tick += Timer_Tick;
            timer.Enabled = true;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            Draw();
        }

        Ship ship = new Ship { Position = new(50, 50) };
        Asteroid asteroid = new Asteroid { Position = new(250, 250) };
        private void Draw()
        {
            EnsureImage();
            ClearDisplay();

            using (var g = Graphics.FromImage(display.Image))
            {
                ship.Draw(g, p => p.Rotate(ship.Rotation) + (Vec)ship.Position);
                asteroid.Draw(g);

                if (ship.Intersects(asteroid)) Debugger.Break();
            }

            display.Invalidate();
        }

        private void ClearDisplay()
        {
            using var g = Graphics.FromImage(display.Image);
            g.Clear(Color.Black);
        }

        private void EnsureImage()
        {
            if (display.Image?.Width != display.Width || display.Image?.Height != display.Height)
            {
                display.Image?.Dispose();
                display.Image = new Bitmap(display.Width, display.Height);
            }
        }

        public class Asteroid : ICollidable
        {
            public PointF Position { get; set; }
            public float Radius { get; set; } = 50;

            public void Draw(Graphics g)
            {
                g.DrawArc(Pens.Gray, GetBoundingBox(), 0, 360);
            }

            public RectangleF GetBoundingBox()
            {
                return new RectangleF(Position, new SizeF(Radius, Radius));
            }

            public bool Intersects(ICollidable other)
            {
                return GetBoundingBox().IntersectsWith(other.GetBoundingBox());
            }
        }

        public interface ICollidable
        {
            RectangleF GetBoundingBox();
            bool Intersects(ICollidable other);
        }

        public class Ship : ICollidable
        {
            public Pen ShipPen { get; } = new Pen(Color.White, 1);
            public PointF Position { get; set; }
            public PointF Velocity { get; set; }
            public Angle Rotation { get; set; } = new(0);
            public Angle RotationalVelocity { get; set; } = new(0);
            public Angle RotAccel { get; } = new Angle(0.1f);
            public PointF LinAccel = new PointF(.1f, 0);

            public void Draw(Graphics g, Func<PointF,PointF> localToScreen)
            {
                Update();

                var points = new PointF[] { new(0, -5), new(10, 0), new(0, 5), new(3,0),new(0,-5) };

                var screenPoints = points.Select(localToScreen).ToArray();

                g.DrawLines(ShipPen, screenPoints);
            }

            private void Update()
            {
                Position += new SizeF(Velocity);
                Rotation += RotationalVelocity;
            }

            public void Accel()
            {
                Velocity += new SizeF(LinAccel.Rotate(Rotation));
            }

            public void RotL()
            {
                RotationalVelocity += RotAccel;
            }

            public void RotR()
            {
                RotationalVelocity -= RotAccel;
            }

            public RectangleF GetBoundingBox() => new RectangleF(0, -5, 10, 10);

            public bool Intersects(ICollidable other)
            {
                var bb = GetBoundingBox();
                var obb = other.GetBoundingBox();
                return bb.IntersectsWith(obb);
            }
        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch(e.KeyChar)
            {
                case 'w': ship.Accel(); break;
                case 'a': ship.RotR(); break;
                case 'd': ship.RotL(); break;
            }
        }

    }

    public interface IObject
    {
        PointF Pos { get; }
        Angle Rot { get; }

        void Draw(Graphics g, Func<PointF, PointF> local2screen);
        void Update();
    }

    public interface IRotating : IObject
    {
        Angle RotVel { get; }
    }

    public interface IMoving : IObject
    {
        PointF Vel { get; }
    }

    public interface IShip : IObject, IRotating, IMoving
    {
        void Accel();
        void RotCCW();
        void RotCW();
    }

    public abstract record MovingObject(IObject self) : IMoving
    {
        public PointF Vel { get; set; }

        public PointF Pos { get; set; }

        public Angle Rot { get; set; }

        public void Draw(Graphics g, Func<PointF, PointF> local2screen) => self.Draw(g, local2screen);

        public void Update()
        {
            Pos += (Vec) Vel;
            self.Update();
        }
    }

    public record Vec(PointF p)
    {
        public static explicit operator Vec (PointF p) => new Vec(p);
        public static PointF operator +(PointF p, Vec v) => p + new SizeF(v.p);
    }

    public record Angle(float Radians)
    {
        public static Angle operator +(Angle a1, Angle a2) => new(a1.Radians + a2.Radians);
        public static Angle operator -(Angle a1, Angle a2) => a1 + (-a2);
        public static Angle operator -(Angle a1) => new(-a1.Radians);

        public float AsDegrees => Radians * MathF.PI / 180f;
    }

    public record Vector2(float X, float Y)
    {
        public static Vector2 operator +(Vector2 v1, Vector2 v2) => new(v1.X + v2.X, v1.Y + v2.Y);
        public static Vector2 operator -(Vector2 v1, Vector2 v2) => v1 + (-v2);
        public static Vector2 operator -(Vector2 v) => new(-v.X, -v.Y);

        public Vector2 Rotate(Angle angle)
        {
            var cos = MathF.Cos(angle.Radians);
            var sin = MathF.Sin(angle.Radians);
            return new Vector2(X * cos - Y * sin, X * sin + Y * cos);
        }

        public Vector2 RotateAround(Vector2 origin, Angle angle)
        {
            var offset = this - origin;
            offset.Rotate(angle);
            return offset + origin;
        }
    }

    public record BoundingBox(Vector2 UpperLeft, Vector2 BottomRight)
    {
        public BoundingBox Rotate(Angle angle)
        {
            var ul = UpperLeft.Rotate(angle);
            var br = BottomRight.Rotate(angle);
            return new BoundingBox(ul, br);
        }

        public static BoundingBox operator +(BoundingBox bb, Vector2 v) => new(bb.UpperLeft + v, bb.BottomRight + v);
    }

    public static class Exs
    {
        public static PointF Rotate(this PointF self, Angle angle) 
        {
            var cos = MathF.Cos(angle.Radians);
            var sin = MathF.Sin(angle.Radians);
            return new PointF(self.X * cos - self.Y * sin, self.X * sin + self.Y * cos);
        }

        public static PointF RotateAround(this PointF self, PointF origin, Angle angle)
        {
            SizeF originSize = new SizeF(origin);
            var z = self - originSize;
            var zz = z.Rotate(angle);
            return zz + originSize;
        }
    }
}
