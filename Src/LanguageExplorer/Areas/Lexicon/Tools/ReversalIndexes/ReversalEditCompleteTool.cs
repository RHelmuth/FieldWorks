﻿// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.PaneBar;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.ReversalIndexes
{
	/// <summary>
	/// ITool implementation for the "reversalEditComplete" tool in the "lexicon" area.
	/// </summary>
	internal sealed class ReversalEditCompleteTool : ITool
	{
		private const string panelMenuId = "left";
		private LcmCache _cache;
		private MultiPane _multiPane;
		private RecordClerk _recordClerk;
		private XhtmlDocView _xhtmlDocView;
		private IReversalIndexRepository _reversalIndexRepository;
		private IReversalIndex _currentReversalIndex;
		private SliceContextMenuFactory _sliceContextMenuFactory;
		private DataTree _dataTree;

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
		public void Deactivate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory.Dispose();

			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);

			_cache = null;
			_reversalIndexRepository = null;
			_currentReversalIndex = null;
			_xhtmlDocView = null;
			_sliceContextMenuFactory = null;
			_dataTree = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			_sliceContextMenuFactory = new SliceContextMenuFactory();
			_sliceContextMenuFactory.RegisterPanelMenuCreatorMethod(panelMenuId, CreateMainPanelContextMenuStrip);
			_cache = majorFlexComponentParameters.LcmCache;
			var currentGuid = ReversalIndexEntryUi.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
			if (currentGuid != Guid.Empty)
			{
				_currentReversalIndex = (IReversalIndex)majorFlexComponentParameters.LcmCache.ServiceLocator.GetObject(currentGuid);
			}
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(LexiconArea.AllReversalEntries, majorFlexComponentParameters.Statusbar, LexiconArea.AllReversalEntriesFactoryMethod);
			}

			var root = XDocument.Parse(LexiconResources.ReversalEditCompleteToolParameters).Root;
			_xhtmlDocView = new XhtmlDocView(root.Element("docview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk);
			_dataTree = new DataTree(_sliceContextMenuFactory);
#if RANDYTODO
			// TODO: See LexiconEditTool for how to set up all manner of menus and toolbars.
#endif
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.HideAdvancedListItemFields), majorFlexComponentParameters.LcmCache, _recordClerk, _dataTree);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "ReversalIndexItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};
			var docViewPaneBar = new PaneBar();
			var img = LanguageExplorerResources.MenuWidget;
			img.MakeTransparent(Color.Magenta);
			var panelMenu = new PanelMenu(_sliceContextMenuFactory, panelMenuId)
			{
				Dock = DockStyle.Left,
				BackgroundImage = img,
				BackgroundImageLayout = ImageLayout.Center
			};
			docViewPaneBar.AddControls(new List<Control> { panelMenu });
			var recordEditViewPaneBar = new PaneBar();
			var panelButton = new PanelButton(PropertyTable, null, PaneBarContainerFactory.CreateShowHiddenFieldsPropertyName(MachineName), LanguageExplorerResources.ksHideFields, LanguageExplorerResources.ksShowHiddenFields)
			{
				Dock = DockStyle.Right
			};
			recordEditViewPaneBar.AddControls(new List<Control> { panelButton });

			_multiPane = MultiPaneFactory.CreateMultiPaneWithTwoPaneBarContainersInMainCollapsingSplitContainer(
				majorFlexComponentParameters.FlexComponentParameters,
				majorFlexComponentParameters.MainCollapsingSplitContainer,
				mainMultiPaneParameters,
				_xhtmlDocView, "Doc Reversals", docViewPaneBar, // XhtmlDocView
				recordEditView, "Browse Entries", recordEditViewPaneBar); // RecordEditView

			_xhtmlDocView.FinishInitialization();
			panelButton.DatTree = recordEditView.DatTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			_xhtmlDocView.OnPropertyChanged("ReversalIndexPublicationLayout");
			((IPostLayoutInit)_multiPane).PostLayoutInit();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
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
			_xhtmlDocView.PublicationDecorator.Refresh();
			_recordClerk.ReloadIfNeeded();
			((DomainDataByFlidDecoratorBase)_recordClerk.VirtualListPublisher).Refresh();
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
		public string MachineName => "reversalEditComplete";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "BUGGY: Reversal Indexes";
		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "lexicon";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion

		private Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>> CreateMainPanelContextMenuStrip(string panelMenuId)
		{
			var contextMenuStrip = new ContextMenuStrip();
			contextMenuStrip.Opening += MainPanelContextMenuStrip_Opening;

			var menuItems = new List<Tuple<ToolStripMenuItem, EventHandler>>();
			var retVal = new Tuple<ContextMenuStrip, CancelEventHandler, List<Tuple<ToolStripMenuItem, EventHandler>>>(contextMenuStrip, MainPanelContextMenuStrip_Opening, menuItems);

			if (_reversalIndexRepository == null)
			{
				_reversalIndexRepository = _cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			}
			var allInstancesinRepository = _reversalIndexRepository.AllInstances().ToDictionary(rei => rei.Guid);
			foreach (var rei in allInstancesinRepository.Values)
			{
				var newMenuItem = PaneBarContextMenuFactory.CreateToolStripMenuItem(contextMenuStrip, rei.ChooserNameTS.Text, null, ReversalIndex_Menu_Clicked, null);
				newMenuItem.Tag = rei;
				menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(newMenuItem, ReversalIndex_Menu_Clicked));
			}

			return retVal;
		}

		private void MainPanelContextMenuStrip_Opening(object sender, CancelEventArgs e)
		{
		}

		private void ReversalIndex_Menu_Clicked(object sender, EventArgs e)
		{
			var contextMenuItem = (ToolStripMenuItem)sender;
			_currentReversalIndex = (IReversalIndex)contextMenuItem.Tag;
			PropertyTable.SetProperty("ReversalIndexGuid", _currentReversalIndex.Guid.ToString(), SettingsGroup.LocalSettings, true, false);
			((ReversalClerk)_recordClerk).ChangeOwningObjectIfPossible();
			SetCheckedState(contextMenuItem);
		}

		private void SetCheckedState(ToolStripMenuItem reversalToolStripMenuItem)
		{
			var currentTag = (IReversalIndex)reversalToolStripMenuItem.Tag;
			reversalToolStripMenuItem.Checked = (currentTag.Guid.ToString() == PropertyTable.GetValue<string>("ReversalIndexGuid"));
		}
	}
}