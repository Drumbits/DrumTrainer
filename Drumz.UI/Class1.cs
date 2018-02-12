using System;
using SkiaSharp;
using Drumz.Common;
using Drumz.Common.Beats;
using System.Linq;

namespace Drumz.UI
{
    public class Class1
    {
        private static readonly Pattern pattern;
        static Class1()
        {
            var path = @"..\..\..\Samples\RHCP_GiveItAway1.drumz.pat.json";
            pattern = Drumz.Common.Beats.IO.PatternIO.Load(path);
        }
        public static int Tpm
        {
            get
            {
                return pattern.Info.SuggestedBpm * pattern.Info.UnitsPerBeat.Index;
            }
        }
        public static void Draw()
        {
            const int beatsPerBar = 4;
            const int nbBars = 2;
            const int unitPerBeat = 2;
            var allBeats = nbBars * beatsPerBar * unitPerBeat;
            const int blockHeight = 24;
            const int blockWidth = 32;

            const int heightInBlocks = 8;

            const int separator = 1;
            string[] instru = new string[] { "HH", "SN", "T1", "CR", "RD" };
            var upColor = new SKColor(25, 146, 191);
            var downColor = new SKColor(37, 92, 123);
            var gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 200), new[] { upColor, downColor }, null, SKShaderTileMode.Mirror);
            using (var headerPaint = new SKPaint())
            {
                headerPaint.Color = SKColors.White;
                headerPaint.IsAntialias = true;
                headerPaint.FakeBoldText = false;
                headerPaint.IsDither = false;
                headerPaint.StrokeWidth = 1;
                headerPaint.Style = SKPaintStyle.StrokeAndFill;
                headerPaint.Typeface = SKTypeface.FromFamilyName("Arial Black", SKTypefaceStyle.Normal);
                headerPaint.LcdRenderText = true;

                headerPaint.TextAlign = SKTextAlign.Left;
                headerPaint.TextSize = 24;

                var headerWidth = instru.Max(i => headerPaint.MeasureText(i)) + 4;

                using (var surface = SKSurface.Create((allBeats + 1) * blockWidth + (int)Math.Ceiling(headerWidth), (instru.Length + 2) * (blockHeight + separator), SKImageInfo.PlatformColorType, SKAlphaType.Premul))
                {
                    SKCanvas myCanvas = surface.Canvas;

                    using (var backgroundPaint = new SKPaint())
                    {
                        backgroundPaint.Shader = gradient;
                        myCanvas.DrawPaint(backgroundPaint);
                    }

                    var gridTop = blockHeight + separator;
                    var gridBottom = (blockHeight + separator) * (instru.Length) + gridTop;
                    var gridLeft = headerWidth;
                    var gridRight = gridLeft + allBeats * blockWidth;
                    var height = gridTop - 1;

                    var fadeOut = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(headerWidth, 0), new[] { new SKColor(255, 255, 255, 0), SKColors.White }, null, SKShaderTileMode.Clamp);

                    myCanvas.DrawLine(gridLeft, height + 1, gridRight, height + 1, headerPaint);
                    headerPaint.Shader = fadeOut;
                    myCanvas.DrawLine(0, height + 1, headerWidth, height + 1, headerPaint);
                    headerPaint.Shader = null;



                    height += blockHeight + separator;

                    for (var index = 0; index < instru.Length; ++index)
                    {
                        myCanvas.DrawText(instru[index], 0, height - 2, headerPaint);
                        myCanvas.DrawLine(gridLeft, height + 1, gridRight, height + 1, headerPaint);
                        headerPaint.Shader = fadeOut;
                        myCanvas.DrawLine(0, height + 1, headerWidth, height + 1, headerPaint);
                        headerPaint.Shader = null;
                        height += blockHeight + separator;
                    }

                    myCanvas.DrawLine(headerWidth, gridTop, headerWidth, gridBottom, headerPaint);
                    var x = headerWidth + blockWidth;
                    for (var beatIndex = 1; beatIndex <= allBeats; ++beatIndex)
                    {
                        myCanvas.DrawLine(x, gridTop, x, gridBottom, headerPaint);
                        if (beatIndex % unitPerBeat == 1)
                        {
                            var fullBeatIndex = ((beatIndex - 1) / unitPerBeat) % beatsPerBar + 1;
                            var numberSize = headerPaint.MeasureText(fullBeatIndex.ToString());
                            var left = x - blockWidth / 2 - numberSize / 2;
                            myCanvas.DrawText(fullBeatIndex.ToString(), left, gridTop - 4, headerPaint);
                        }
                        x += blockWidth;
                    }
                    // Your drawing code goes here.
                    using (var img = surface.Snapshot())
                    {
                        using (var data = img.Encode(SKEncodedImageFormat.Png, 80))
                        {
                            // save the data to a stream
                            using (var stream = System.IO.File.OpenWrite(@"C:\Temp\img.png"))
                            {
                                data.SaveTo(stream);
                            }
                        }
                    }
                }
            }
        }
        public static void Draw(SKSurface surface, int timeIndex)
        {/*
            var drawer = new PatternDrawer(pattern,
                new GridDrawer.Settings
                {
                    LineHeight = 24,
                    BeatWidth = 64,
                    FontSize = 20
                },
                2);
            if (timeIndex > pattern.Info.LastTime.Index)
                timeIndex = (timeIndex - 1) % pattern.Info.LastTime.Index + 1;
            drawer.Draw(surface, new TimeInUnits(timeIndex));
            int beatsPerBar = pattern.Info.BeatsPerBar;
            int nbBars = pattern.Info.BarsCount;
            int unitPerBeat = pattern.Info.UnitsPerBeat.Index;
            var allBeats = nbBars * beatsPerBar * unitPerBeat;
            const int blockHeight = 24;
            const int blockWidth = 32;

            const int heightInBlocks = 8;

            string[] instru = pattern.Instruments.Select(i => i.Name).ToArray();
            var upColor = new SKColor(25, 146, 191);
            var downColor = new SKColor(37, 92, 123);
            var gradient = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, 200), new[] { upColor, downColor }, null, SKShaderTileMode.Mirror);
            using (var headerPaint = new SKPaint())
                using (var beatMarkPaint = new SKPaint { Style = SKPaintStyle.Fill, Color = new SKColor(255, 255, 255, 125) })
            {

                headerPaint.Color = SKColors.White;
                headerPaint.IsAntialias = true;
                headerPaint.FakeBoldText = false;
                headerPaint.IsDither = false;
                headerPaint.StrokeWidth = 1;
                headerPaint.Style = SKPaintStyle.StrokeAndFill;
                headerPaint.Typeface = SKTypeface.FromFamilyName("Arial Black", SKTypefaceStyle.Normal);
                headerPaint.LcdRenderText = true;

                headerPaint.TextAlign = SKTextAlign.Left;
                headerPaint.TextSize = 20;

                var headerWidth = instru.Max(i => headerPaint.MeasureText(i)) + 4;

                SKCanvas myCanvas = surface.Canvas;

                using (var backgroundPaint = new SKPaint())
                {
                    backgroundPaint.Shader = gradient;
                    myCanvas.DrawPaint(backgroundPaint);
                }

                var gridTop = blockHeight + 1;
                var gridBottom = blockHeight * (instru.Length) + gridTop;
                var gridLeft = headerWidth;
                var gridRight = gridLeft + allBeats * blockWidth;
                var height = gridTop - 1;

                var fadeOut = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(headerWidth, 0), new[] { new SKColor(255, 255, 255, 0), SKColors.White }, null, SKShaderTileMode.Clamp);

                myCanvas.DrawLine(gridLeft, height + 1, gridRight, height + 1, headerPaint);
                headerPaint.Shader = fadeOut;
                myCanvas.DrawLine(0, height + 1, headerWidth, height + 1, headerPaint);
                headerPaint.Shader = null;



                height += blockHeight;
                timeIndex = timeIndex % allBeats + 1;

                for (var index = 0; index < instru.Length; ++index)
                {
                    myCanvas.DrawText(instru[index], 0, height - 2, headerPaint);
                    myCanvas.DrawLine(gridLeft, height + 1, gridRight, height + 1, headerPaint);
                    headerPaint.Shader = fadeOut;
                    myCanvas.DrawLine(0, height + 1, headerWidth, height + 1, headerPaint);
                    headerPaint.Shader = null;
                    height += blockHeight;
                }

                myCanvas.DrawLine(headerWidth, gridTop, headerWidth, gridBottom, headerPaint);
                
                var x = headerWidth + blockWidth;
                for (var beatIndex = 1; beatIndex <= allBeats; ++beatIndex)
                {
                    myCanvas.DrawLine(x, gridTop, x, gridBottom, headerPaint);
                    if (beatIndex % unitPerBeat == 1)
                    {
                        var fullBeatIndex = ((beatIndex - 1) / unitPerBeat) % beatsPerBar + 1;
                        var numberSize = headerPaint.MeasureText(fullBeatIndex.ToString());
                        var left = x - blockWidth / 2 - numberSize / 2;
                        myCanvas.DrawText(fullBeatIndex.ToString(), left, gridTop - 4, headerPaint);
                    }
                    if (beatIndex == timeIndex)
                    {
                        var beatRect = new SKRect(x - blockWidth, gridTop, x, gridBottom);
                        myCanvas.DrawRect(beatRect, beatMarkPaint);
                    }
                    // instruments
                    var beats = pattern.Beats(new TimeInUnits(beatIndex - 1));
                    var markWidth = blockHeight / 2;
                    var markHeight = blockHeight / 2;
                    if (beats != null)
                    {
                        var centerX = x - blockWidth / 2;
                        for (var insIndex = 0; insIndex < beats.Length; ++insIndex)
                        {
                            if (beats[insIndex] == null) continue;
                            var centerY = gridTop + (insIndex + 0.5f) * blockHeight;
                            myCanvas.DrawLine(centerX-markWidth/2, centerY-markHeight/2, centerX+markWidth/2, centerY+markHeight/2, headerPaint);
                            myCanvas.DrawLine(centerX - markWidth / 2, centerY + markHeight / 2, centerX + markWidth / 2, centerY - markHeight / 2, headerPaint);
                        }
                    }
                    x += blockWidth;
                }

            }*/
        }
    }
}
