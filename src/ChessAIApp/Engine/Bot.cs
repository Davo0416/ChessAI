using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ChessDotNet;

// Abstract Bot class - Base for every other bot class
namespace ChessAIApp
{
  public abstract class Bot
  {
    public Board? board { get; set; }
    public abstract Task MakeMove();
    public abstract void Reset();
    public abstract void SetColor(Player? color);
  }
}