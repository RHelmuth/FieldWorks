﻿// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Grammar.Tools.GrammarSketch
{
	/// <summary>
	/// ITool implementation for the "grammarSketch" tool in the "grammar" area.
	/// </summary>
	internal sealed class GrammarSketchTool : ITool
	{
		private GrammarSketchHtmlViewer _grammarSketchHtmlViewer;
		private bool _refreshOriginalValue;
		private ToolStripItem _refreshMenu;
		private ToolStripItem _refreshToolBarBtn;
		private ToolStripItem _fileExportMenu;

		private bool _fileExportVisibleOriginalValue;
		private bool _fileExportEnabledOriginalValue;

		void FileExportMenu_Click(object sender, EventArgs e)
		{
			using (var dlg = new ExportDialog())
			{
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.ShowDialog();
			}
		}

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

		#region Implementation of IMajorFlexComponent

		/// <summary>
		/// Deactivate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the outgoing component, when the user switches to a component.
		/// </remarks>
		public void Deactivate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
			mainCollapsingSplitContainer.SecondControl.Controls.Remove(_grammarSketchHtmlViewer);
			((IDisposable)_grammarSketchHtmlViewer).Dispose();
			_grammarSketchHtmlViewer = null;

			_refreshMenu.Enabled = _refreshOriginalValue;
			_refreshMenu = null;
			_refreshToolBarBtn.Enabled = _refreshOriginalValue;
			_refreshToolBarBtn = null;

			_fileExportMenu.Click -= FileExportMenu_Click;
			_fileExportMenu.Visible = _fileExportVisibleOriginalValue;
			_fileExportMenu.Enabled = _fileExportEnabledOriginalValue;
			_fileExportMenu = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "See TODO-Linux comment")]
		public void Activate(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer,
			StatusBar statusbar)
		{
			_grammarSketchHtmlViewer = new GrammarSketchHtmlViewer
			{
				Dock = DockStyle.Fill
			};
			_grammarSketchHtmlViewer.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
			mainCollapsingSplitContainer.SecondControl.Controls.Add(_grammarSketchHtmlViewer);

			// F5 refresh is disabled in this tool.
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			_refreshMenu = menuStrip.Items.Find("refreshToolStripMenuItem", true)[0];
			_refreshOriginalValue = _refreshMenu.Enabled;
			_refreshMenu.Enabled = false;
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			var ts = (ToolStrip)toolStripContainer.TopToolStripPanel.Controls.Find("toolStripStandard", false)[0];
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			_refreshToolBarBtn = ts.Items.Find("toolStripButton_Refresh", true)[0];
			_refreshToolBarBtn.Enabled = false;

			// File->Export menu is visible and enabled in this tool.
			// TODO-Linux: boolean 'searchAllChildren' parameter is marked with "MonoTODO".
			_fileExportMenu = menuStrip.Items.Find("exportToolStripMenuItem", true)[0];
			_fileExportVisibleOriginalValue = _fileExportMenu.Visible;
			_fileExportEnabledOriginalValue = _fileExportMenu.Enabled;
			_fileExportMenu.Visible = true;
			_fileExportMenu.Enabled = true;
			_fileExportMenu.Click += FileExportMenu_Click;
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
#if RANDYTODO
			// TODO: If tool uses a SDA decorator (IRefreshable), then call its "Refresh" method.
			// TODO: Call "ReloadIfNeeded" on Record clerk(s).
#endif
		}

		/// <summary>
		/// The properties are about to be saved, so make sure they are all current.
		/// Add new ones, as needed.
		/// </summary>
		public void EnsurePropertiesAreCurrent()
		{
		}

		#endregion

		#region Implementation of IMajorFlexUiComponent

		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		public string MachineName
		{
			get { return "grammarSketch"; }
		}

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName
		{
			get { return "Grammar Sketch"; }
		}

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName
		{
			get { return "grammar"; }
		}

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon
		{
			get
			{
				var image = Images.DocumentView;
				image.MakeTransparent(Color.Magenta);
				return image;
			}
		}

		#endregion
	}
}