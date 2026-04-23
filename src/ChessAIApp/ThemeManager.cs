using System;
using System.Windows;

namespace ChessAIApp
{
  public static class ThemeManager
  {
    public static void LoadTheme(string themeName)
    {
      var dict = new ResourceDictionary
      {
        Source = new Uri($"Themes/{themeName}.xaml", UriKind.Relative)
      };

      Application.Current.Resources.MergedDictionaries.Clear();
      Application.Current.Resources.MergedDictionaries.Add(dict);
    }
  }
}