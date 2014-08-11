#region License
/* TrueTypeSharp
   Copyright (c) 2010 Illusory Studios LLC

   TrueTypeSharp is available at zer7.com. It is a C# port of Sean Barrett's
   C library stb_truetype, which was placed in the public domain and is
   available at nothings.org.

   Permission to use, copy, modify, and/or distribute this software for any
   purpose with or without fee is hereby granted, provided that the above
   copyright notice and this permission notice appear in all copies.

   THE SOFTWARE IS PROVIDED "AS IS" AND THE AUTHOR DISCLAIMS ALL WARRANTIES
   WITH REGARD TO THIS SOFTWARE INCLUDING ALL IMPLIED WARRANTIES OF
   MERCHANTABILITY AND FITNESS. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR
   ANY SPECIAL, DIRECT, INDIRECT, OR CONSEQUENTIAL DAMAGES OR ANY DAMAGES
   WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER IN AN
   ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT OF
   OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
*/
#endregion

using System;
using System.Runtime.Serialization;

namespace TrueTypeSharp
{
    [Serializable]
    public struct FontBitmap : ISerializable
    {
        public delegate T PixelConversionFunc<T>(byte opacity);

        public byte[] Buffer; public int BufferOffset;
        public int XOffset, YOffset, Width, Height, Stride;

        public FontBitmap(int width, int height) : this()
        {
            if (width < 0 || height < 0 || width * height < 0)
                { throw new ArgumentOutOfRangeException(); }

            Buffer = new byte[width * height];
            Stride = Width = width; Height = height;
        }

        FontBitmap(SerializationInfo info, StreamingContext context) : this()
        {
            Buffer = (byte[])info.GetValue("Buffer", typeof(byte[]));
            BufferOffset = info.GetInt32("BufferOffset");
            XOffset = info.GetInt32("XOffset");
            YOffset = info.GetInt32("YOffset");
            Width = info.GetInt32("Width");
            Height = info.GetInt32("Height");
            Stride = info.GetInt32("Stride");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Buffer", Buffer);
            info.AddValue("BufferOffset", BufferOffset);
            info.AddValue("XOffset", XOffset);
            info.AddValue("YOffset", YOffset);
            info.AddValue("Width", Width);
            info.AddValue("Height", Height);
            info.AddValue("Stride", Stride);
        }

        public FontBitmap GetResizedBitmap(int width, int height)
        {
            var bitmap = new FontBitmap(width, height);
            int w = Math.Min(width, Width), h = Math.Min(height, Height);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++) { bitmap[x, y] = this[x, y]; }
            }
            return bitmap;
        }

        public FontBitmap GetResizedBitmap(int width, int height, BakedCharCollection bakedChars)
        {
            var bitmap = GetResizedBitmap(width, height);
            if (bakedChars != null) { bakedChars.BakeWidth = bitmap.Width; bakedChars.BakeHeight = bitmap.Height; }
            return bitmap;
        }

        static int RoundUpToPow2(int value)
        {
            int rounded = 1;
            while (rounded < value) { rounded <<= 1; }
            return rounded;
        }

        public FontBitmap GetResizedBitmapPow2()
        {
            return GetResizedBitmap(RoundUpToPow2(Width), RoundUpToPow2(Height));
        }

        public FontBitmap GetResizedBitmapPow2(BakedCharCollection bakedChars)
        {
            return GetResizedBitmap(RoundUpToPow2(Width), RoundUpToPow2(Height), bakedChars);
        }

        public FontBitmap GetTrimmedBitmap()
        {
            return GetResizedBitmap(Width, Height);
        }

        public byte[,] To2DBitmap()
        {
            return To2DBitmap(false);
        }

        public byte[,] To2DBitmap(bool yMajor)
        {
            return To2DBitmap(yMajor, x => x);
        }

        public T[,] To2DBitmap<T>(bool yMajor, PixelConversionFunc<T> pixelConversionFunc)
        {
            if (pixelConversionFunc == null) { throw new ArgumentNullException("pixelConversionFunc"); }

            if (yMajor)
            {
                var bitmap = new T[Height, Width];
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        bitmap[y, x] = pixelConversionFunc(this[x, y]);
                    }
                }
                return bitmap;
            }
            else
            {
                var bitmap = new T[Width, Height];
                for (int x = 0; x < Width; x++)
                {
                    for (int y = 0; y < Height; y++)
                    {
                        bitmap[x, y] = pixelConversionFunc(this[x, y]);
                    }
                }
                return bitmap;
            }
        }

        public int StartOffset
        {
            get { return YOffset * Stride + XOffset + BufferOffset; }
        }

        public bool IsValid
        {
            get
            {
                return Buffer != null && Width >= 0 && Height >= 0 && StartOffset >= 0
                    && Width * Height >= 0 && Width * Height <= Buffer.Length
                    && StartOffset + Height * Stride >= 0
                    && StartOffset + Height * Stride <= Buffer.Length;
            }
        }

        internal FakePtr<byte> StartPointer
        {
            get { return new FakePtr<byte>() { Array = Buffer, Offset = StartOffset }; }
        }

        public byte this[int x, int y]
        {
            get
            {
                try { return Buffer[StartOffset + y * Stride + x]; }
                catch (NullReferenceException) { throw new InvalidOperationException(); }
            }
            set
            {
                try { Buffer[StartOffset + y * Stride + x] = value; }
                catch (NullReferenceException) { throw new InvalidOperationException(); }
            }
        }
    }
}
