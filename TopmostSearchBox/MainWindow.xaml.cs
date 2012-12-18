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
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using SharedClasses;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace WpfApplication1
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private static Action<bool> ActionAfterOperationPerformed;
		ObservableCollection<DropAreaGroup> DropAreaGroupList;
		private const double visiblyOpacity = 0.99;
		private double initialDragBorderOpacity;

		UserActivityHook mouseHook;
		//MouseGestures.MouseGestures gesturesMonitor;
		//TODO: Add items for upload/download (FTP, PHP, Http, etc)
		public MainWindow()
		{
			InitializeComponent();

			mouseHook = new UserActivityHook(true, false, false, false, true, true);
			mouseHook.OnGesture += new UserActivityHook.GestureHandler(gestures_Gesture);

			//gesturesMonitor = new MouseGestures.MouseGestures(true, true);
			//gesturesMonitor.Gesture += new MouseGestures.MouseGestures.GestureHandler(gestures_Gesture);
			//gesturesMonitor.Enabled = true;

			ActionAfterOperationPerformed = delegate
			{
				//if (!TransparentDropAreasContainsMouse())
				transparentDropAreas.Opacity = 0;
			};

			DropAreaGroupList = new ObservableCollection<DropAreaGroup>(DropAreaGroup.GetPredefinedDropAreaList(ActionAfterOperationPerformed, OnMessage));

			DropAreaGroupList.CollectionChanged += (sn, ev) =>
			{
				SetStackPanelItemWidth();
			};

			//var fonts = new List<string>()
			//{
			//    "BatangChe",
			//    "Calibri",
			//    "Century Gothic",
			//    "DotumChe",
			//    "Freestyle Script",
			//    "Gisha",
			//    "Gulim",
			//    "Kristen ITC",
			//    "Microsoft Yi Baiti",
			//    "Miriam Fixed",
			//    "MV Boli",
			//    "Papyrus",
			//    "Segoe Print",
			//    "Tempus Sans ITC",
			//    "Utsaah",
			//    "Verdana"
			//};
			//foreach (var f in fonts)
			//    tmpcombo.Items.Add(f);
		}

		private void gestures_Gesture(object sender, UserActivityHook.MouseGestureEventArgs e)
		{
			if (e.Gesture == null || e.Gesture.Motions == null)
				return;

			string gesture = e.Gesture.Motions.ToUpper();
			ThreadingInterop.DoAction(delegate
			{
				if (gesture.Equals("DURLDRL"))
				{
					UserMessages.ShowInfoMessage("You spelled Francois.");
				}
				else if (gesture.Equals("LDRULR"))
				{
					var path = RegistryInterop.GetAppPathFromRegistry("GoogleEarth").Trim('"');
					if (!File.Exists(path))
						UserMessages.ShowWarningMessage("Please install Google Earth to use this gesture, cannot find file: " + path);
					else
						Process.Start(path);
				}
				else
					UserMessages.ShowInfoMessage("Gesture: " + e.Gesture.Motions);
			},
			false);
		}

		private void OnMessage(string errorMessage, FeedbackMessageTypes feedbackType)
		{
			var par = new Paragraph();
			par.Inlines.Add(new Run(errorMessage) { Foreground = GetForegroundFromFeedbackType(feedbackType) });
			richTextBox1.Document.Blocks.Add(par);
		}

		private Brush GetForegroundFromFeedbackType(FeedbackMessageTypes feedbackType)
		{
			switch (feedbackType)
			{
				case FeedbackMessageTypes.Success:
					return Brushes.Green;
				case FeedbackMessageTypes.Error:
					return Brushes.Red;
				case FeedbackMessageTypes.Warning:
					return Brushes.Orange;
				case FeedbackMessageTypes.Status:
					return Brushes.Gray;
				default:
					return Brushes.Yellow;
			}
		}

		Timer timerToKeepAlwaysOnTop;
		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			this.Width = SystemParameters.WorkArea.Width;
			this.Left = 0;
			//int uncomment;
			//this.Height = 400;
			this.Height = SystemParameters.WorkArea.Height;
			this.Top = 0;

			initialDragBorderOpacity = dragBorder.Opacity;

			listboxDropAreas.ItemsSource = DropAreaGroupList;

			timerToKeepAlwaysOnTop = new Timer(
				delegate { this.Dispatcher.Invoke((Action)delegate { this.Topmost = false; this.Topmost = true; }); },
				null,
				TimeSpan.FromSeconds(5),
				TimeSpan.FromSeconds(2));

			OnMessage("App started, watch this space for important messages.", FeedbackMessageTypes.Status);

			richTextBox1.Document.Blocks.AddRange(
				InlineItem.GetParagraphsFromInlineItems(
					true,//Own paragraph
					new InlineButton(new Button()
						{
							Content = "Double-click this button to exit application",
							Background = Brushes.Transparent,
							Padding = new Thickness(5)
						},
						BaselineAlignment.Center,
						new MouseDoubleClickEvent(
							false,
							(snder, evtargs) =>
							{
								//UserMessages.ShowInfoMessage("Double clicked");
								evtargs.Handled = true;
								Application.Current.Shutdown();
							}))));
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
			var groupListboxItem = (ListBoxItem)listboxDropAreas.ItemContainerGenerator.ContainerFromItem(GetDropAreaGroupOfDropAreaItem(dropItem));
			ContentPresenter myContentPresenter = WPFHelper.FindVisualChild<ContentPresenter>(groupListboxItem);
			DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
			ListBox groupitemListbox = (ListBox)myDataTemplate.FindName("groupItemListbox", myContentPresenter);
			for (int i = 0; i < groupitemListbox.Items.Count; i++)
			//foreach (ListBoxItem dropareaListboxItem in groupitemListbox.Items)
			{
				var dropareaListboxItem = (ListBoxItem)groupitemListbox.ItemContainerGenerator.ContainerFromItem(groupitemListbox.Items[i]);
				if (dropareaListboxItem.DataContext == dropItem)
				{
					ContentPresenter myContentPresenter2 = WPFHelper.FindVisualChild<ContentPresenter>(dropareaListboxItem);
					DataTemplate myDataTemplate2 = myContentPresenter2.ContentTemplate;
					return (Border)myDataTemplate2.FindName("itemMainBorder", myContentPresenter2);
				}
			}
			return null;
		}

		private DropAreaGroup GetDropAreaGroupOfDropAreaItem(DropArea item)
		{
			foreach (var group in DropAreaGroupList)
				if (group.DropAreas.Contains(item))
					return group;
			return null;//Nothing found
		}

		private void MenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void Border_DragEnter(object sender, DragEventArgs e)
		{
			MakeDragBorderFullyOpacit();
		}

		private void dragBorder_DragOver(object sender, DragEventArgs e)
		{
			OnTargetDragOver(sender, e);
			//Allow all types at this stage?
			transparentDropAreas.Opacity = visiblyOpacity;
			e.Effects = DragDropEffects.Copy;
			if (e.Data.GetDataPresent(DataFormats.FileDrop))
				e.Effects = DragDropEffects.Link;
			e.Handled = true;

			//if (e.Data.GetDataPresent(DataFormats.FileDrop)
			//    || e.Data.GetDataPresent(DataFormats.Text))
			//{
			//transparentDropAreas.Opacity = visiblyOpacity;
			//e.Effects = DragDropEffects.Copy;
			//}
			//else
			//    e.Effects = DragDropEffects.None;
			//e.Handled = true;
		}

		private void Border_Drop(object sender, DragEventArgs e)
		{
			MakeDragBorderTransparent();
			transparentDropAreas.Opacity = 0;
			string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files != null)
			{
				UserMessages.ShowInfoMessage("Dropped on always visible drop area" + Environment.NewLine + string.Join(Environment.NewLine, files));
			}
		}

		private void Border_DragLeave(object sender, DragEventArgs e)
		{
			//DropBorderDragLeave(sender);
			OnTargetDragLeave(sender, e);
		}

		private void DropBorderDragLeave(object sender)
		{
			try
			{
				//We make the 'ignoreBorderWidth' parameter 1 because DragLeave is fired sometimes while on the border edge
				if (WPFHelper.DoesFrameworkElementContainMouse(((Border)sender), 1))
					return;
			}
			catch { }
			MakeDragBorderTransparent();
			ThreadingInterop.ActionAfterDelay(delegate
			{
				this.Dispatcher.Invoke((Action)delegate
				{
					bool containsMouse = TransparentDropAreasContainsMouse();
					if (!containsMouse)//We exited left of drop area, can now hide the drop areas again
						transparentDropAreas.Opacity = 0;
				});
			},
			TimeSpan.FromSeconds(0.1),
			err => OnMessage(err, FeedbackMessageTypes.Error));
		}

		private bool _dragInProgress;
		private void OnTargetDragLeave(object sender, DragEventArgs e)
		{
			_dragInProgress = false;

			// It appears there's a quirk in the drag/drop system.  While the user is dragging the object
			// over our control it appears the system will send us (quite frequently) DragLeave followed 
			// immediately by DragEnter events.  So when we get DragLeave, we can't be sure that the 
			// drag/drop operation was actually terminated.  Therefore, instead of doing cleanup
			// immediately, we schedule the cleanup to execute later and if during that time we receive
			// another DragEnter or DragOver event, then we don't do the cleanup.
			dragBorder.Dispatcher.BeginInvoke(new Action(() =>
			{
				if (_dragInProgress == false) DropBorderDragLeave(sender);//, e);
			}));
		}

		private void OnTargetDragOver(object sender, DragEventArgs e)
		{
			_dragInProgress = true;

			//OnQueryDragDataValid(sender, e);
		}

		private void WriteToConsole(string msg)
		{
			Console.WriteLine(string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss"), msg));
		}

		private bool TransparentDropAreasContainsMouse()
		{
			try
			{
				bool containsIt = WPFHelper.DoesFrameworkElementContainMouse(transparentDropAreas);
				return containsIt;
			}
			catch { return false; }
			//transparentDropAreas.UpdateLayout();
			//var transparentGridRect = new Rect(transparentDropAreas.PointToScreen(
			//    new Point(0, 0)),
			//    new Size(transparentDropAreas.ActualWidth, transparentDropAreas.ActualHeight));
			//var mousePos = MouseLocation.GetMousePosition();
			//return transparentGridRect.Contains(mousePos);
		}

		private void transparentGrid_PreviewDragLeave(object sender, DragEventArgs e)
		{
			if (!TransparentDropAreasContainsMouse())//We exited the drop areas, can hide them again (if entered the dragBorder it will make visible again on DragEnter event)
				transparentDropAreas.Opacity = 0;
		}

		private void transparentGrid_MouseLeave(object sender, MouseEventArgs e)
		{
			if (!TransparentDropAreasContainsMouse())//We exited the drop areas, can hide them again (if entered the dragBorder it will make visible again on DragEnter event)
				transparentDropAreas.Opacity = 0;
		}

		private void stackPanelLoaded(object sender, RoutedEventArgs e)
		{
			SetStackPanelItemWidth();
		}

		Dictionary<Border, Brush> previousBorderBrush = new Dictionary<Border, Brush>();
		private void HighLightBorder(Border border)
		{
			//border.BorderBrush = Brushes.Red;
			
			//border.Background = (Brush)Resources.MergedDictionaries[1]["DropItemHighlightBackground"];			
			////Must now use MergedDictionaries[0] because we remove the ExpressionDark Theme
			if (previousBorderBrush.ContainsKey(border))
				previousBorderBrush.Remove(border);
			previousBorderBrush.Add(border, border.Background);
			border.Background = (Brush)Resources.MergedDictionaries[0]["DropItemHighlightBackground"];
		}

		private void RemoveHighLightBorder(Border border)
		{
			//border.BorderBrush = Brushes.Transparent;
			if (previousBorderBrush.ContainsKey(border))
				border.Background = previousBorderBrush[border];
			//border.Background = Brushes.Transparent;
		}

		private void itemMainBorder_DragEnter(object sender, DragEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe is Border)
				HighLightBorder((Border)fe);
		}

		private void itemMainBorder_DragOver(object sender, DragEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			DropArea da = fe.DataContext as DropArea;
			if (da == null) return;

			if (e.Data.GetDataPresent(da.DragEventArgsFormatType)
				&& (da.OptionalCheckToAllowDragDrop == null || da.OptionalCheckToAllowDragDrop(da, e.Data.GetData(da.DragEventArgsFormatType))))
				e.Effects = DragDropEffects.Copy;
			else
				e.Effects = DragDropEffects.None;
			e.Handled = true;
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
				if (!fe.ContextMenu.IsOpen)
					RemoveHighLightBorder((Border)fe);
		}

		private void itemMainBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			DropArea da = fe.DataContext as DropArea;
			if (da == null) return;
			if (fe is Border)
			{
				e.Handled = true;
				MouseSimulator.ClickRightMouseButton();
				//fe.ContextMenu.DataContext = da;
				//fe.ContextMenu
				//var binding = fe.ContextMenu.GetBindingExpression(ContextMenu.ItemsSourceProperty);
				//binding.Status();
				//fe.ContextMenu.IsOpen = true;
				//System.Windows.Input.MouseEventArgs e1 = new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, DateTime.Now.Millisecond);
				//e1.RoutedEvent = System.Windows.Input.Mouse.;
				//fe.RaiseEvent(e1);

				//System.Windows.Input.MouseEventArgs e1 = new System.Windows.Input.MouseEventArgs(System.Windows.Input.Mouse.PrimaryDevice, DateTime.Now.Millisecond);
				//e1.RoutedEvent = System.Windows.Input.Mouse.MouseUpEvent;
				//fe.RaiseEvent(e1);
			}
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

		private void itemMainBorder_ContextMenuClosed(object sender, RoutedEventArgs e)
		{
			FrameworkElement fe = sender as FrameworkElement;
			if (fe == null) return;
			if (fe.DataContext is DropArea)
			{
				var itemBorder = GetMainBorderOfItem(fe.DataContext as DropArea);
				try
				{
					//if (!WPFHelper.DoesFrameworkElementContainMouse(itemBorder))
					RemoveHighLightBorder(itemBorder);//Juse always close it for now, otherwise it will keep the box highlighted sometimes
				}
				catch { }
			}
		}

		private void NotificationAreaIcon_MouseClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton == MouseButton.Left)
			{
				if (this.IsVisible)
					this.Hide();
				else
					this.Show();
			}
		}

		//private void tmpcombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
		//{
		//    if (tmpcombo.SelectedItem == null) return;
		//    label1.FontFamily = new System.Windows.Media.FontFamily(tmpcombo.SelectedItem.ToString());
		//    label2.FontFamily = new System.Windows.Media.FontFamily(tmpcombo.SelectedItem.ToString());
		//}
	}

	public class DropAreaGroup
	{
		private static Action<string, FeedbackMessageTypes> _onerror;
		private static Action<string, FeedbackMessageTypes> OnMessage
		{
			get { if (_onerror == null) _onerror = delegate { }; return _onerror; }
			set { _onerror = value; DropArea.OnMessage = value; }
		}

		public string GroupName { get; private set; }
		public ObservableCollection<DropArea> DropAreas { get; private set; }
		private DropAreaGroup(string GroupName, IEnumerable<DropArea> DropAreas)
		{
			this.GroupName = GroupName;
			this.DropAreas = new ObservableCollection<DropArea>(DropAreas);
		}

		public static List<DropAreaGroup> GetPredefinedDropAreaList(Action<bool> ActionAfterOperationPerformed, Action<String, FeedbackMessageTypes> onMessage)
		{
			OnMessage = onMessage;

			List<DropAreaGroup> tmpGroupList = new List<DropAreaGroup>();
			tmpGroupList.Add(new DropAreaGroup("CMD", new List<DropArea>()
			{
				new DropArea(
					DropArea.GetBitmapImageFromUri("DisplayIconssearch.ico"),
					"Command prompt",
					DataFormats.FileDrop,
					(dragobj, tagobj) => { Process.Start("cmd", "/k pushd \"" + (dragobj as string[])[0] + "\""); return true; },
					"CommandPrompt",
					(dragobj, tagobj) => dragobj as string[],
					recentlines => recentlines,
					ActionAfterOperationPerformed,
					null,
					(da, dragobj) => { return Directory.Exists((dragobj as string[])[0]); })
			}));

			foreach (FileOperations[] fileoperationsInGroup in fileOperationGroups)
			{
				List<DropArea> tmplist = new List<DropArea>();

				foreach (FileOperations fileoperation in fileoperationsInGroup)
				{
					ImageSource outIconImage;
					string outDisplayText;
					Color outBackColor;
					Func<DropArea, object, bool> outOptionalCheckToAllowDragDrop;
					if (!GetDetailsForFileOperation(fileoperation, out outIconImage, out outDisplayText, out outBackColor, out outOptionalCheckToAllowDragDrop))
						continue;

					tmplist.Add(
						new DropArea(
							outIconImage,
							outDisplayText,
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
							ActionAfterOperationPerformed,
							fileoperation,
							outOptionalCheckToAllowDragDrop));
				}
				tmpGroupList.Add(new DropAreaGroup("Group" + (tmpGroupList.Count + 1), tmplist));
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

			return tmpGroupList;
		}

		private static string fileoperationsExepath = null;
		enum FileOperations
		{
			SearchTextInFiles, ToHex, FromHex, GetMetaData, CompareToCachedMetadata,
			CreateDownloadLinkTextFile, ResizeImage,
			RotatePdf90degrees, RotatePdf180degrees, RotatePdf270degrees,
			HighlightToHtml
		};
		private static List<FileOperations[]> fileOperationGroups = new List<FileOperations[]>()
		{
			new FileOperations[] { FileOperations.SearchTextInFiles },
			new FileOperations[] { FileOperations.ToHex, FileOperations.FromHex },
			new FileOperations[] { FileOperations.GetMetaData, FileOperations.CompareToCachedMetadata },
			new FileOperations[] { FileOperations.CreateDownloadLinkTextFile },
			new FileOperations[] { FileOperations.ResizeImage },
			new FileOperations[] { FileOperations.RotatePdf90degrees, FileOperations.RotatePdf180degrees, FileOperations.RotatePdf270degrees },
			new FileOperations[] { FileOperations.HighlightToHtml }
		};
		private static bool GetDetailsForFileOperation(FileOperations operation, out ImageSource outIconImage, out string outDisplayText, out Color outBackColor, out Func<DropArea, object, bool> outOptionalCheckToAllowDragDrop)
		{
			bool returnVal = true;//Will be set false only in the "default" case of the switch statement
			outOptionalCheckToAllowDragDrop = null;

			switch (operation)
			{
				case FileOperations.SearchTextInFiles:
					outIconImage = DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop files here to search in all file paths and contents for text...";
					outBackColor = Colors.Green;
					//outOptionalCheckToAllowDragDrop = (dropareSender, dropObj) =>
					//{
					//    return dropObj is string[] && Directory.Exists(((string[])dropObj)[0]);
					//};
					break;
				case FileOperations.ToHex:
					outIconImage = DropArea.GetBitmapImageFromUri("DisplayIcons/tohex.jpg");
					outDisplayText = "Drop files here to convert them to hex...";
					outBackColor = Colors.Blue;
					break;
				case FileOperations.FromHex:
					outIconImage = DropArea.GetBitmapImageFromUri("DisplayIcons/fromhex.jpg");
					outDisplayText = "Drop files here to convert the from hex...";
					outBackColor = Colors.Blue;
					break;
				case FileOperations.GetMetaData:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to generate (and cache) their metadata...";
					outBackColor = Colors.Orange;
					break;
				case FileOperations.CompareToCachedMetadata:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to compare to their metadata, to get changes (added, removed, changed)...";
					outBackColor = Colors.Orange;
					break;
				case FileOperations.CreateDownloadLinkTextFile:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop folder(s) here to generate a DownloadLink text file (will prompt you to paste the link)...";
					outBackColor = Colors.BlueViolet;
					break;
				case FileOperations.ResizeImage:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop images here to resize them...";
					outBackColor = Colors.White;
					break;
				case FileOperations.RotatePdf90degrees:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 90degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.RotatePdf180degrees:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 180degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.RotatePdf270degrees:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
					outDisplayText = "Drop PDF files here to rotate them 270degrees...";
					outBackColor = Colors.Magenta;
					break;
				case FileOperations.HighlightToHtml:
					outIconImage = null;//DropArea.GetBitmapImageFromUri("DisplayIcons/search.ico");
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
					OnMessage("Please install FileOperations, cannot find exe path from registry. Unable to process drag-drop operation.", FeedbackMessageTypes.Error);
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
					OnMessage("Could not search using FileOperations:" + Environment.NewLine
						+ (outputs.Count > 0 ? "Outputs: " + Environment.NewLine + string.Join(Environment.NewLine, outputs) + Environment.NewLine : "")
						+ (errors.Count > 0 ? "Errors: " + Environment.NewLine + string.Join(Environment.NewLine, errors) : ""),
						FeedbackMessageTypes.Error);
				}
			},
			filepathIn,
			false);
			//searchingThreads.Add(searchThread);
		}
	}
	public class DropArea
	{
		private static Action<string, FeedbackMessageTypes> _onmessage;
		internal static Action<string, FeedbackMessageTypes> OnMessage
		{
			get { if (_onmessage == null) _onmessage = delegate { }; return _onmessage; }
			set { _onmessage = value; }
		}

		public ImageSource IconImage { get; private set; }
		public string DisplayText { get; private set; }
		public string DragEventArgsFormatType { get; private set; }
		private Func<object, object, bool, bool> ActionToPerform;//Middle boolean to say to add to recent list or not
		private string RecentListFilename_NullForNoRecents;
		private Func<object, object, string[]> FuncToGetRecentFileLinesFromDragObject;//DragDrop
		private Func<string[], object> FuncToGetDragObjectFromRecentFileLines;
		private Action<bool> ActionAfterOperationPerformed;
		private object Tag;
		public Func<DropArea, object, bool> OptionalCheckToAllowDragDrop;
		public ObservableCollection<MenuItem> CurrentMenuItems { get; private set; }
		/// <summary>
		/// Creates a new DropArea item.
		/// </summary>
		/// <param name="IconImage">The icon to display.</param>
		/// <param name="DisplayText">The text to display.</param>
		/// <param name="DragEventArgsFormatType">The format type which will be used to obtain the data from the DragEventArgs, passed as parameter to function "dragEventArgs.Data.GetData(...)".</param>
		/// <param name="ActionToPerform">The action to be performed when an item is dropped or clicked from the recent list.</param>
		/// <param name="RecentListFilename_NullForNoRecents">The filename of the recent list, if null no history will be kept.</param>
		/// <param name="FuncToGetRecentFileLinesFromDragObject">The function which will be used to convert the lines of the recent file to the DragObject.</param>
		/// <param name="FuncToGetDragObjectFromRecentFileLines">The function which will be used to convert the DragObject to the lines in the recent file.</param>
		/// <param name="ActionAfterOperationPerformed">Action to be taken after the operation is performed (either via drag-drop or menuitem click), the bool parameter is whether the perform succeeded or failed.</param>
		/// <param name="Tag">Any additional data to be associated with this object.</param>
		/// <param name="OptionalCheckToAllowDragDrop">Additional function which will be used (after using the DragEventArgsFormatType) to check whether a drag-drop operation may be allowed.</param>
		public DropArea(ImageSource IconImage, string DisplayText, string DragEventArgsFormatType, Func<object, object, bool> ActionToPerform, string RecentListFilename_NullForNoRecents, Func<object, object, string[]> FuncToGetRecentFileLinesFromDragObject, Func<string[], object> FuncToGetDragObjectFromRecentFileLines, Action<bool> ActionAfterOperationPerformed, object Tag, Func<DropArea, object, bool> OptionalCheckToAllowDragDrop = null)
		{
			this.IconImage = IconImage;
			this.DisplayText = DisplayText;
			this.DragEventArgsFormatType = DragEventArgsFormatType;
			this.ActionToPerform = (dragobj, tagObj, addtoRecent) => ActionToPerform(dragobj, tagObj);
			this.RecentListFilename_NullForNoRecents = RecentListFilename_NullForNoRecents;
			if (this.RecentListFilename_NullForNoRecents != null && !this.RecentListFilename_NullForNoRecents.Contains("."))
				this.RecentListFilename_NullForNoRecents += ".txt";//Only add .txt if has no other extension
			this.FuncToGetRecentFileLinesFromDragObject = FuncToGetRecentFileLinesFromDragObject;
			this.FuncToGetDragObjectFromRecentFileLines = FuncToGetDragObjectFromRecentFileLines;
			this.ActionAfterOperationPerformed = ActionAfterOperationPerformed;
			if (this.ActionAfterOperationPerformed == null) this.ActionAfterOperationPerformed = delegate { };
			this.Tag = Tag;
			this.OptionalCheckToAllowDragDrop = OptionalCheckToAllowDragDrop;
			LoadRecentList();
		}
		private void LoadRecentList()
		{
			if (CurrentMenuItems == null)
				CurrentMenuItems = new ObservableCollection<MenuItem>();
			CurrentMenuItems.Clear();
			var recentFilepath = GetRecentSearchesFilepath();
			if (File.Exists(recentFilepath))
			{
				var recentlines = File.ReadAllLines(recentFilepath);
				if (FuncToGetDragObjectFromRecentFileLines != null)
				{
					CurrentMenuItems = null;
					CurrentMenuItems =
						new ObservableCollection<MenuItem>(
							recentlines.Select(line =>
							{
								return GetMenuItemFromFileLine(line);
							}));
				}
			}
			if (CurrentMenuItems.Count == 0)
				CurrentMenuItems.Add(new MenuItem() { Header = "No recent items yet.", IsEnabled = true });
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
				var filelinesFromObject = FuncToGetRecentFileLinesFromDragObject(dragObject, Tag);

				try
				{
					var filelines = File.ReadAllLines(GetRecentSearchesFilepath()).ToList();
					filelines.RemoveAll(line => filelinesFromObject.Contains(line));
					File.WriteAllLines(GetRecentSearchesFilepath(), filelines);
					LoadRecentList();
				}
				catch (Exception exc)
				{
					OnMessage("Unable to remove recent item: " + exc.Message, FeedbackMessageTypes.Error);
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
			menuItem.Tag = FuncToGetDragObjectFromRecentFileLines(new string[] { line });
			menuItem.PreviewMouseRightButtonUp += (sn, ev) =>
			{
				MenuItem thisMenuitem = (MenuItem)sn;
				thisMenuitem.ContextMenu.DataContext = thisMenuitem.Tag;//Pass the DragObject to it
				thisMenuitem.ContextMenu.IsOpen = true;
				ev.Handled = true;//So the recent item is not performed too
			};
			menuItem.ContextMenu = GetMenuItemRightClickMenu();
			menuItem.Click += (sn, ev) =>
			{
				PerformOperation(((MenuItem)sn).Tag, Tag, false);//False sais do not add to recents (AGAIN, because this MenuItem was obtained from the recents)
			};
			return menuItem;
		}
		public void PerformOnDrop(DragEventArgs dragEventArgs)
		{
			var dragEventData = dragEventArgs.Data.GetData(DragEventArgsFormatType);
			PerformOperation(dragEventData, Tag, true);
		}

		private bool PerformOperation(object dragObject, object tagObject, bool addToRecentList)
		{
			OnMessage("Performing " + this.GetType().Name, FeedbackMessageTypes.Status);
			bool operationSuccess = ActionToPerform(dragObject, tagObject, addToRecentList);
			if (addToRecentList && operationSuccess)//True is to say we must add it to the recent list
				AddToRecentList(dragObject);
			ActionAfterOperationPerformed(operationSuccess);
			return operationSuccess;
		}

		private void AddToRecentList(object dragEventData)
		{
			if (RecentListFilename_NullForNoRecents != null)
			{
				try
				{
					var newRecentItems = FuncToGetRecentFileLinesFromDragObject(dragEventData, Tag);
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
					OnMessage(exc.Message, FeedbackMessageTypes.Error);
				}
			}
		}

		private string GetRecentSearchesFilepath()
		{
			return SettingsInterop.GetFullFilePathInLocalAppdata(RecentListFilename_NullForNoRecents, "TopmostSearchBox", "Recent");
		}

		public static BitmapImage GetBitmapImageFromUri(string relativeUri)
		{
			try
			{
				BitmapImage img = new BitmapImage();
				img.BeginInit();
				img.UriSource = new Uri("pack://application:,,,/TopmostSearchBox;component/" + relativeUri.TrimStart('/'));
				img.EndInit();
				return img;
			}
			catch { return null; }
		}
	}
}
