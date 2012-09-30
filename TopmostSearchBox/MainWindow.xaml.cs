using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using SharedClasses;
using System.IO;
using System.Threading;

namespace WpfApplication1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		ObservableCollection<DropArea> DropAreaList = new ObservableCollection<DropArea>(DropArea.GetPredefinedDropAreaList());
		private const double visiblyOpacity = 0.98;
		private double initialDragBorderOpacity;

		public MainWindow()
		{
			InitializeComponent();

			DropAreaList.CollectionChanged += (sn, ev) =>
			{
				SetStackPanelItemWidth();
			};
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.Width = SystemParameters.WorkArea.Width;
			this.Left = 0;
			this.Height = SystemParameters.WorkArea.Height;
			this.Top = 0;

			initialDragBorderOpacity = dragBorder.Opacity;

			transparentDropAreas.ItemsSource = DropAreaList;
		}

		private void SetStackPanelItemWidth()
		{
			//transparentDropAreas.UpdateLayout();
			//    var count = DropAreaList.Count;
			//    if (count != 0)
			//    {
			//        double itemwidth = (double)transparentDropAreas.ActualWidth / (double)count;
			//        foreach (var da in DropAreaList)
			//        {
			//            var itemborder = GetMainBorderOfItem(da);
			//            itemborder.Height = transparentDropAreas.ActualHeight - 5;//-5 Just to prevent scrollbars from showing
			//            itemborder.Width = itemwidth - 1;//-1 Just to prevent scrollbars from showing
			//        }
			//    }
		}

		private Border GetMainBorderOfItem(DropArea dropItem)
		{
			var listboxitem = (ListBoxItem)transparentDropAreas.ItemContainerGenerator.ContainerFromItem(dropItem);
			ContentPresenter myContentPresenter = WPFHelper.GetVisualChild<ContentPresenter>(listboxitem);
			DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
			return (Border)myDataTemplate.FindName("itemMainBorder", myContentPresenter);
		}

		private void MenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void Border_DragEnter(object sender, DragEventArgs e)
		{
			MakeDragBorderFullyOpacit();
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
			{
				transparentDropAreas.Opacity = visiblyOpacity;
				e.Effects = DragDropEffects.Copy;
			}
			else
				e.Effects = DragDropEffects.None;
		}

		private void Border_Drop(object sender, DragEventArgs e)
		{
			MakeDragBorderTransparent();
			transparentDropAreas.Opacity = 0;
			string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files != null)
			{
				MessageBox.Show("Dropped on always visible drop area" + Environment.NewLine + string.Join(Environment.NewLine, files));
			}
		}

		private void Border_DragLeave(object sender, DragEventArgs e)
		{
			MakeDragBorderTransparent();
			ThreadingInterop.ActionAfterDelay(delegate
			{
				this.Dispatcher.Invoke((Action)delegate
				{
					if (!TransparentDropAreasContainsMouse())//We exited left of drop area, can now hide the drop areas again
						transparentDropAreas.Opacity = 0;
				});
			},
			TimeSpan.FromSeconds(0.1),
			err => UserMessages.ShowErrorMessage(err));
		}

		private bool TransparentDropAreasContainsMouse()
		{
			transparentDropAreas.UpdateLayout();
			var transparentGridRect = new Rect(transparentDropAreas.PointToScreen(
				new Point(0, 0)),
				new Size(transparentDropAreas.ActualWidth, transparentDropAreas.ActualHeight));
			var mousePos = MouseLocation.GetMousePosition();
			return transparentGridRect.Contains(mousePos);
		}

		private void transparentGrid_PreviewDragLeave(object sender, DragEventArgs e)
		{
			if (!TransparentDropAreasContainsMouse())//We exited the drop areas, can hide them again (if entered the dragBorder it will make visible again on DragEnter event)
				transparentDropAreas.Opacity = 0;
		}

		private void stackPanelLoaded(object sender, RoutedEventArgs e)
		{
			SetStackPanelItemWidth();
		}

		private void HighLightBorder(Border border)
		{
			border.BorderBrush = Brushes.Red;
		}

		private void RemoveHighLightBorder(Border border)
		{
			border.BorderBrush = Brushes.Transparent;
		}

		private void itemMainBorder_DragEnter(object sender, DragEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe is Border)
				HighLightBorder((Border)fe);

			DropArea da = fe.DataContext as DropArea;
			if (da == null) return;

			if (e.Data.GetDataPresent(da.DragEventArgsObjectType))
			{
				e.Effects = DragDropEffects.Copy;
			}
			else
				e.Effects = DragDropEffects.None;
		}

		private void itemMainBorder_Drop(object sender, DragEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			DropArea da = fe.DataContext as DropArea;
			if (da == null) return;
			transparentDropAreas.Opacity = 0;
			da.PerformOnDrop(e);
		}

		private void itemMainBorder_DragLeave(object sender, DragEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe is Border)
				RemoveHighLightBorder((Border)fe);
		}

		private void exitRightClickMenuItemClicked(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void dragBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			transparentDropAreas.Opacity = transparentDropAreas.Opacity == visiblyOpacity ? 0 : visiblyOpacity;
		}

		private void itemMainBorder_MouseEnter(object sender, MouseEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe is Border)
				HighLightBorder((Border)fe);
		}

		private void itemMainBorder_MouseLeave(object sender, MouseEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe is Border)
				RemoveHighLightBorder((Border)fe);
		}

		private void MakeDragBorderFullyOpacit()
		{
			dragBorder.Opacity = 1;
		}
		private void MakeDragBorderTransparent()
		{
			dragBorder.Opacity = initialDragBorderOpacity;
		}
		private void dragBorder_MouseEnter(object sender, MouseEventArgs e)
		{
			MakeDragBorderFullyOpacit();
		}

		private void dragBorder_MouseLeave(object sender, MouseEventArgs e)
		{
			MakeDragBorderTransparent();
		}

		private void labelAboutMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			e.Handled = true;
			AboutWindow2.ShowAboutWindow(new ObservableCollection<DisplayItem>()
			{
				new DisplayItem("Author", "Francois Hill"),
				new DisplayItem("Hex icon", "http://www.iconarchive.com", "http://www.iconarchive.com/show/oxygen-icons-by-oxygen-icons.org/Mimetypes-text-x-hex-icon.html")
			},
			true,
			this);
		}
	}

	public class DropArea
	{
		public ImageSource IconImage { get; private set; }
		public string DisplayText { get; private set; }
		public /*Brush*/ Color BackColor { get; private set; }
		public string DragEventArgsObjectType { get; private set; }
		private Func<object, object, bool, bool> OnDrop;//Middle boolean to say to add to recent list or not
		private string RecentListFilename_NullForNoRecents;
		private Func<object, object, string[]> FuncToGetFileLinesFromDragEventArgs;//DragDrop
		private Func<string[], object> FuncToGetDragEventArgsFromRecentList;
		private object Tag;
		public ObservableCollection<MenuItem> CurrentMenuItems { get; private set; }
		public DropArea(ImageSource IconImage, string DisplayText, /*Brush*/Color BackColor, string DragEventArgsObjectType, Func<object, object, bool> OnDrop, string RecentListFilename_NullForNoRecents, Func<object, object, string[]> FuncToGetFileLinesFromDragEventArgs, Func<string[], object> FuncToGetDragEventArgsFromRecentList, object Tag)
		{
			this.IconImage = IconImage;
			this.DisplayText = DisplayText;
			this.BackColor = BackColor;
			this.DragEventArgsObjectType = DragEventArgsObjectType;
			this.OnDrop = (dragev, tagObj, addtoRecent) => OnDrop(dragev, tagObj);
			this.RecentListFilename_NullForNoRecents = RecentListFilename_NullForNoRecents;
			if (this.RecentListFilename_NullForNoRecents != null && !this.RecentListFilename_NullForNoRecents.Contains("."))
				this.RecentListFilename_NullForNoRecents += ".txt";//Only add .txt if has no other extension
			this.FuncToGetFileLinesFromDragEventArgs = FuncToGetFileLinesFromDragEventArgs;
			this.FuncToGetDragEventArgsFromRecentList = FuncToGetDragEventArgsFromRecentList;
			this.Tag = Tag;
			LoadRecentList();
		}
		private void LoadRecentList()
		{
			if (CurrentMenuItems != null)
				CurrentMenuItems.Clear();
			var recentFilepath = GetRecentSearchesFilepath();
			if (File.Exists(recentFilepath))
			{
				var recentlines = File.ReadAllLines(recentFilepath);
				if (FuncToGetDragEventArgsFromRecentList != null)
				{
					if (CurrentMenuItems == null)
						CurrentMenuItems =
							new ObservableCollection<MenuItem>(
								recentlines.Select(line =>
								{
									return GetMenuItemFromFileLine(line);
								}));
					else
						foreach (var line in recentlines)
							CurrentMenuItems.Add(GetMenuItemFromFileLine(line));
				}
			}
		}

		private ContextMenu _menuItemRightClickMenu = null;
		private ContextMenu GetMenuItemRightClickMenu()
		{
			if (_menuItemRightClickMenu != null)
				return _menuItemRightClickMenu;

			_menuItemRightClickMenu = new ContextMenu();

			MenuItem removeItem = new MenuItem() { Header = "Remove" };
			removeItem.Click += (sn, ev) =>
			{
				MenuItem thisMenuitem = (MenuItem)sn;
				object dragObject = thisMenuitem.DataContext;//DataContext should be same as its parent ContextMenu, which gives us the DragObject
				var filelinesFromObject = FuncToGetFileLinesFromDragEventArgs(dragObject, Tag);

				try
				{
					var filelines = File.ReadAllLines(GetRecentSearchesFilepath()).ToList();
					filelines.RemoveAll(line => filelinesFromObject.Contains(line));
					File.WriteAllLines(GetRecentSearchesFilepath(), filelines);
					LoadRecentList();
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage("Unable to remove recent item: " + exc.Message);
				}
			};
			_menuItemRightClickMenu.Items.Add(removeItem);
			return _menuItemRightClickMenu;
		}

		private MenuItem GetMenuItemFromFileLine(string line)
		{
			var menuItem = new MenuItem();
			menuItem.Header = line;
			//We generate a string array with one element, so the operation will be performed only with one element, not an array
			//The code seems confusing, but it is correct
			menuItem.Tag = FuncToGetDragEventArgsFromRecentList(new string[] { line });
			menuItem.PreviewMouseRightButtonUp += (sn, ev) =>
			{
				MenuItem thisMenuitem = (MenuItem)sn;
				thisMenuitem.ContextMenu.DataContext = thisMenuitem.Tag;//Pass the DragObject to it
				thisMenuitem.ContextMenu.IsOpen = true;
				ev.Handled = true;//So the recent item is not performed too
			};
			menuItem.ContextMenu = GetMenuItemRightClickMenu();
			menuItem.Click += (sn, ev) => OnDrop(((MenuItem)sn).Tag, Tag, false);//False sais do not add to recents (AGAIN, because this MenuItem was obtained from the recents)
			return menuItem;
		}
		public void PerformOnDrop(DragEventArgs dragEventArgs)
		{
			var dragEventData = dragEventArgs.Data.GetData(DragEventArgsObjectType);
			if (OnDrop(dragEventData, Tag, true))//True is to say we must add it to the recent list
				AddToRecentList(dragEventData);
		}

		private void AddToRecentList(object dragEventData)
		{
			if (RecentListFilename_NullForNoRecents != null)
			{
				try
				{
					var newRecentItems = FuncToGetFileLinesFromDragEventArgs(dragEventData, Tag);
					if (newRecentItems != null)
					{
						var currentLines = File.Exists(GetRecentSearchesFilepath())
							? File.ReadAllLines(GetRecentSearchesFilepath()).ToList()
							: new List<string>();
						foreach (var linetoadd in newRecentItems)
							if (!currentLines.Contains(linetoadd))
								currentLines.Add(linetoadd);
						File.WriteAllLines(GetRecentSearchesFilepath(), currentLines);
						//File.AppendAllLines(GetRecentSearchesFilepath(), fileLinesToAdd);
						LoadRecentList();
						//foreach (var line in newRecentItems)
						//    CurrentMenuItems.Add(GetMenuItemFromFileLine(line));
					}
				}
				catch (Exception exc)
				{
					UserMessages.ShowErrorMessage(exc.Message);
				}
			}
		}

		private string GetRecentSearchesFilepath()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata(RecentListFilename_NullForNoRecents, "TopmostSearchBox", "Recent");
		}

		private static BitmapImage GetBitmapImageFromUri(string relativeUri)
		{
			BitmapImage img = new BitmapImage();
			img.BeginInit();
			img.UriSource = new Uri("pack://application:,,,/TopmostSearchBox;component/" + relativeUri.TrimStart('/'));
			img.EndInit();
			return img;
		}

		public static List<DropArea> GetPredefinedDropAreaList()
		{
			List<DropArea> tmplist = new List<DropArea>();

			foreach (FileOperations fileoperation in Enum.GetValues(typeof(FileOperations)))
			{
				ImageSource outIconImage;
				string outDisplayText;
				Color outBackColor;
				if (!GetDetailsForFileOperation(fileoperation, out outIconImage, out outDisplayText, out outBackColor))
					continue;

				tmplist.Add(new DropArea(
					outIconImage,
					outDisplayText,
					outBackColor,
					DataFormats.FileDrop,
					(dragObject, tagObj) =>
					{
						string[] files = dragObject as string[];//Because out format was DataFormats.FileDrop, we know it should be a string[]
						if (files != null)
						{
							DoFileOperationSearch(files, (FileOperations)tagObj);
							return true;
						}
						else
							return false;
					},
					fileoperation.ToString(),
					(dragObject, tagObj) =>
					{
						return dragObject as string[];
					},
					recentfilelines => recentfilelines,//Just return the string[] as the drag-drop object
					fileoperation));
			}

			//tmplist.Add(new DropArea(
			//    null,
			//    "Drop files here to convert to hex...",
			//    Colors.Blue,//Brushes.Blue,
			//    DataFormats.FileDrop,
			//    dragObject =>
			//    {
			//        string[] files = dragObject as string[];//Because out format was DataFormats.FileDrop, we know it should be a string[]
			//        if (files != null)
			//        {
			//            DoFileOperationSearch(files, FileOperations.ToHex);
			//            return true;
			//        }
			//        else
			//            return false;
			//    },
			//    "ToHex",
			//    dragObject => dragObject as string[],
			//    recentfilelines => recentfilelines));

			return tmplist;
		}

		private static string fileoperationsExepath = null;
		enum FileOperations
		{
			SearchTextInFiles, ToHex, FromHex, GetMetaData, CompareToCachedMetadata,
			CreateDownloadLinkTextFile, ResizeImage,
			RotatePdf90degrees, RotatePdf180degrees, RotatePdf270degrees,
			HighlightToHtml
		};
		private static bool GetDetailsForFileOperation(FileOperations operation, out ImageSource outIconImage, out string outDisplayText, out Color outBackColor)
		{
			bool returnVal = true;//Will be set false only in the "default" case of the switch statement

			switch (operation)
			{
				case FileOperations.SearchTextInFiles:
					outIconImage = GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop files here to search in all file paths and contents for text...";
					outBackColor = Colors.Green;
					break;
				case FileOperations.ToHex:
					outIconImage = GetBitmapImageFromUri("DisplayIcons/tohex.jpg");
					outDisplayText = "Drop files here to convert them to hex...";
					outBackColor = Colors.Blue;
					break;
				case FileOperations.FromHex:
					outIconImage = GetBitmapImageFromUri("DisplayIcons/fromhex.jpg");
					outDisplayText = "Drop files here to convert the from hex...";
					outBackColor = Colors.Blue;
					break;
				case FileOperations.GetMetaData:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to generate (and cache) their metadata...";
					outBackColor = Colors.Orange;
					break;
				case FileOperations.CompareToCachedMetadata:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to compare to their metadata, to get changes (added, removed, changed)...";
					outBackColor = Colors.Orange;
					break;
				case FileOperations.CreateDownloadLinkTextFile:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to generate a DownloadLink text file (will prompt you to paste the link)...";
					outBackColor = Colors.BlueViolet;
					break;
				case FileOperations.ResizeImage:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop images here to resize them...";
					outBackColor = Colors.White;
					break;
				case FileOperations.RotatePdf90degrees:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 90degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.RotatePdf180degrees:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 180degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.RotatePdf270degrees:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 270degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.HighlightToHtml:
					outIconImage = null;//GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop source code files here to generate their syntax-highlighted HTML...";
					outBackColor = Colors.Maroon;
					break;
				default:
					outIconImage = null;
					outDisplayText = null;//"UNKNOW file oparetion, drop here...";
					outBackColor = Colors.Transparent;
					returnVal = false;
					break;
			}
			return returnVal;
		}
		private static void DoFileOperationSearch(string[] filepaths, FileOperations operation)
		{
			foreach (var file in filepaths)
				DoFileOperationSearch(file, operation);
		}
		private static void DoFileOperationSearch(string filepathIn, FileOperations operation)
		{
			if (fileoperationsExepath == null)
			{
				fileoperationsExepath = RegistryInterop.GetAppPathFromRegistry("FileOperations");
				if (fileoperationsExepath == null)
				{
					UserMessages.ShowErrorMessage("Please install FileOperations, cannot find exe path from registry. Unable to process drag-drop operation.");
					//Application.Exit();
				}
			}

			Thread searchThread = ThreadingInterop.PerformOneArgFunctionSeperateThread<string>(
			(filepath) =>
			{
				List<string> outputs, errors;
				int exitcode;

				bool? runresult = ProcessesInterop.RunProcessCatchOutput(
					new System.Diagnostics.ProcessStartInfo(fileoperationsExepath,
						string.Format("\"{0}\" \"{1}\"", operation.ToString().ToLower(), filepath)),
						out outputs,
						out errors,
						out exitcode);

				if (runresult != true)
				{
					UserMessages.ShowErrorMessage("Could not search using FileOperations:" + Environment.NewLine
						+ (outputs.Count > 0 ? "Outputs: " + Environment.NewLine + string.Join(Environment.NewLine, outputs) + Environment.NewLine : "")
						+ (errors.Count > 0 ? "Errors: " + Environment.NewLine + string.Join(Environment.NewLine, errors) : ""));
				}
			},
			filepathIn,
			false);
			//searchingThreads.Add(searchThread);
		}
	}

	public static class MouseLocation
	{
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetCursorPos(ref Win32Point pt);

		[StructLayout(LayoutKind.Sequential)]
		private struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		};
		public static Point GetMousePosition()
		{
			Win32Point w32Mouse = new Win32Point();
			GetCursorPos(ref w32Mouse);
			return new Point(w32Mouse.X, w32Mouse.Y);
		}
	}
}
