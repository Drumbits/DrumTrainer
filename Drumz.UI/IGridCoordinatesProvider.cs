using SkiaSharp;
using Drumz.Common.Beats;
using Drumz.Common;

namespace Drumz.UI
{
    public interface IGridCoordinatesProvider
    {
        SKPoint Coordinates(IInstrumentId instrument, TimeInUnits t);
        SKPoint Coordinates(IInstrumentId instrument, float timeInBeats);
    }
}
