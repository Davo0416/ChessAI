using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ChessAIApp
{
  public static class Overlays
  {
    // Highlights ===================================================================================
    public static void HighlightSquare(Panel square, Color color, string tag, bool doUnhighlight)
    {
      if (square == null) return;

      bool alreadyHighlighted =
          square.Children
          .OfType<Grid>()
          .Any(child => (string?)child.Tag == tag);

      // Remove previous highlights of the same category
      if (doUnhighlight)
        UnhighlightSquare(square, tag);

      if (!alreadyHighlighted)
      {
        var highlight = new Grid
        {
          Background = new SolidColorBrush(color),
          IsHitTestVisible = false,
          Tag = tag
        };

        Panel.SetZIndex(highlight, -1);
        square.Children.Add(highlight);
      }
    }

    public static void UnhighlightSquare(Panel square, string tag)
    {
      if (square == null) return;

      var toRemove =
          square.Children
          .OfType<Grid>()
          .Where(child =>
              (child.Tag is string t) &&
              t.Contains(tag))
          .ToList();

      foreach (var h in toRemove)
        square.Children.Remove(h);
    }

    public static void UnhighlightAll(Grid chessBoardGrid)
    {
      var overlaysToRemove = chessBoardGrid.Children
          .OfType<Grid>()
          .SelectMany(square => square.Children
              .OfType<FrameworkElement>()
              .Where(fe => fe.Tag is string t && t == "Highlight"))
          .ToList();

      foreach (var overlay in overlaysToRemove)
      {
        ((Panel)overlay.Parent).Children.Remove(overlay);
      }
    }

    public static void UnhighlightAllUserHighlights(Panel chessBoardGrid)
    {
      var overlaysToRemove = chessBoardGrid.Children
          .OfType<Grid>()
          .SelectMany(square => square.Children
              .OfType<FrameworkElement>()
              .Where(fe => fe.Tag is string t && t.Contains("UserHighlight")))
          .ToList();

      foreach (var overlay in overlaysToRemove)
      {
        ((Panel)overlay.Parent).Children.Remove(overlay);
      }
    }

    // Arrows =========================================================================
    public static void DrawArrow(Panel ArrowLayer, Point start, Point end, Color color)
    {
      string id = start.ToString() + end.ToString();
      var arrows = ArrowLayer.Children
      .OfType<FrameworkElement>()
      .Where(e => (string?)e.Tag == id)
      .ToList();

      if (arrows.Any())
      {
        foreach (var arrow in arrows)
          ArrowLayer.Children.Remove(arrow);
      }
      else
      {
        double thickness = 15;
        Vector direction = end - start;
        direction.Normalize();
        end -= direction * 20;
        start += direction * 30;

        var line = new System.Windows.Shapes.Line
        {
          X1 = start.X,
          Y1 = start.Y,
          X2 = end.X,
          Y2 = end.Y,
          Stroke = new SolidColorBrush(color),
          StrokeThickness = thickness,
          StrokeStartLineCap = PenLineCap.Flat,
          StrokeEndLineCap = PenLineCap.Flat,
          Tag = id
        };

        ArrowLayer.Children.Add(line);
        end -= direction * 10;
        Vector perpendicular = new Vector(-direction.Y, direction.X);

        double arrowHeadLength = 20;
        double arrowHeadWidth = 35;
        end += direction * 30;
        Point p1 = end;
        Point p2 = end - direction * arrowHeadLength + perpendicular * arrowHeadWidth / 2;
        Point p3 = end - direction * arrowHeadLength - perpendicular * arrowHeadWidth / 2;

        var arrowHead = new System.Windows.Shapes.Polygon
        {
          Points = new PointCollection { p1, p2, p3 },
          Fill = new SolidColorBrush(color),
          Tag = id
        };

        ArrowLayer.Children.Add(arrowHead);
      }
    }

    public static void ClearArrows(Panel ArrowLayer)
    {
      ArrowLayer.Children.Clear();
    }
  }
}
