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
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace TrueTypeSharp.Demo
{
    class Program
    {
        static void SaveBitmap(byte[] data, int x0, int y0, int x1, int y1,
            int stride, string filename)
        {
            var bitmap = new Bitmap(x1 - x0, y1 - y0);
            for (int y = y0; y < y1; y++)
            {
                for (int x = x0; x < x1; x++)
                {
                    byte opacity = data[y * stride + x];
                    bitmap.SetPixel(x - x0, y - y0, Color.FromArgb(opacity, 0x00, 0x7f, 0xff));
                }
            }
            bitmap.Save(filename);
        }

        static void Main(string[] args)
        {
            var font = new TrueTypeFont(@"Anonymous\Anonymous Pro.ttf");

            // Render some characters...
            for (char ch = 'A'; ch <= 'Z'; ch++)
            {
                int width, height, xOffset, yOffset;
                float scale = font.GetScaleForPixelHeight(80);
                byte[] data = font.GetCodepointBitmap(ch, scale, scale,
                    out width, out height, out xOffset, out yOffset);

                SaveBitmap(data, 0, 0, width, height, width, "Char-" + ch.ToString() + ".png");
            }

            // Let's try baking. Tasty tasty.
            BakedCharCollection characters; float pixelHeight = 18;
            var bitmap = font.BakeFontBitmap(pixelHeight, out characters, true);

            SaveBitmap(bitmap.Buffer, 0, 0, bitmap.Width, bitmap.Height, bitmap.Width, "BakeResult1.png");
            
            // Now, let's give serialization a go.
            using (var file = File.OpenWrite("BakeResult2.temp"))
            {
                var bitmapSaver = new BinaryFormatter();
                bitmapSaver.Serialize(file, bitmap);
                bitmapSaver.Serialize(file, characters);

                int ascent, descent, lineGap;
                float scale = font.GetScaleForPixelHeight(pixelHeight);
                font.GetFontVMetrics(out ascent, out descent, out lineGap);
                bitmapSaver.Serialize(file, (float)ascent * scale);
                bitmapSaver.Serialize(file, (float)descent * scale);
                bitmapSaver.Serialize(file, (float)lineGap * scale);
            }

            using (var file = File.OpenRead("BakeResult2.temp"))
            {
                var bitmapLoader = new BinaryFormatter();
                var bitmapAgain = (FontBitmap)bitmapLoader.Deserialize(file);
                var charactersAgain = (BakedCharCollection)bitmapLoader.Deserialize(file);

                SaveBitmap(bitmapAgain.Buffer, 0, 0, bitmapAgain.Width, bitmapAgain.Height, bitmap.Width, "BakeResult2.png");
                for (char ch = 'A'; ch <= 'Z'; ch++)
                {
                    BakedChar bakedChar = charactersAgain[ch];
                    if (bakedChar.IsEmpty) { continue; }
                    SaveBitmap(bitmapAgain.Buffer,
                        bakedChar.X0, bakedChar.Y0, bakedChar.X1, bakedChar.Y1,
                        bitmapAgain.Stride, "SmallChar-" + ch.ToString() + ".png");
                }
            }
        }
    }
}
