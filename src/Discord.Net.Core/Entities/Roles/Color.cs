using System;
using System.Diagnostics;

namespace Discord
{
    [DebuggerDisplay(@"{DebuggerDisplay,nq}")]
    public struct Color
    { 
        /// <summary> Gets the encoded value for this color. </summary>
        public uint RawValue { get; }

        /// <summary> Gets the red component for this color. </summary>
        public byte R => (byte)(RawValue >> 16);
        /// <summary> Gets the green component for this color. </summary>
        public byte G => (byte)(RawValue >> 8);
        /// <summary> Gets the blue component for this color. </summary>
        public byte B => (byte)(RawValue);

        public Color(uint rawValue)
        {
            RawValue = rawValue;
        }
        public Color(byte r, byte g, byte b)
        {
            RawValue =
                ((uint)r << 16) |
                ((uint)g << 8) |
                (uint)b;
        }
        public Color(int r, int g, int b)
        {
            if (r < 0 || r > 255)
                throw new ArgumentOutOfRangeException(nameof(r), "Value must be within [0,255]");
            if (g < 0 || g > 255)
                throw new ArgumentOutOfRangeException(nameof(g), "Value must be within [0,255]");
            if (b < 0 || b > 255)
                throw new ArgumentOutOfRangeException(nameof(b), "Value must be within [0,255]");
            RawValue =
                ((uint)r << 16) |
                ((uint)g << 8) |
                (uint)b;
        }
        public Color(float r, float g, float b)
        {
            if (r < 0.0f || r > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(r), "Value must be within [0,1]");
            if (g < 0.0f || g > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(g), "Value must be within [0,1]");
            if (b < 0.0f || b > 1.0f)
                throw new ArgumentOutOfRangeException(nameof(b), "Value must be within [0,1]");
            RawValue =
                ((uint)(r * 255.0f) << 16) |
                ((uint)(g * 255.0f) << 8) |
                (uint)(b * 255.0f);
        }
        
        public override string ToString() =>
            $"#{Convert.ToString(RawValue, 16)}";
        private string DebuggerDisplay =>
            $"#{Convert.ToString(RawValue, 16)} ({RawValue})";
    }
}
