using System;
using System.Collections.Generic;
using System.Text;

namespace Drumz.Common.Beats
{
    public sealed class Velocity
    {
        public static Velocity Softest = new Velocity(0.1);
        public static Velocity Soft = new Velocity(0.25);
        public static Velocity Medium = new Velocity(0.5);
        public static Velocity Loud = new Velocity(0.75);
        public static Velocity Loudest = new Velocity(1.0);
        public readonly double Value;
        public Velocity(double value)
        {
            this.Value = Math.Round(value, 3);
        }
        public override bool Equals(object obj)
        {
            var other = obj as Velocity;
            return other != null && other.Value == Value;
        }
        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
        public static bool operator ==(Velocity v1, Velocity v2)
        {
            return Equals(v1, v2);
        }
        public static bool operator !=(Velocity v1, Velocity v2)
        {
            return !Equals(v1, v2);
        }
        public override string ToString()
        {
            return "v:" + Value;
        }
    }
}
