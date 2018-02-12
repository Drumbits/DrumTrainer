using SkiaSharp;
using Drumz.Common.Beats;

namespace Drumz.UI
{
    public interface IGridCoordinatesProvider
    {
        SKPoint Coordinates(int instrumentIndex, TimeInUnits t);
        SKPoint Coordinates(int instrumentIndex, float timeInBeats);
    }
}
