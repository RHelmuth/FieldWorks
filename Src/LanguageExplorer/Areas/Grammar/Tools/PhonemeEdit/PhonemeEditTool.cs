﻿// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.PaneBar;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using LanguageExplorer.Works;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Grammar.Tools.PhonemeEdit
{
	/// <summary>
	/// ITool implementation for the "phonemeEdit" tool in the "grammar" area.
	/// </summary>
	internal sealed class PhonemeEditTool : ITool
	{
		private MultiPane _multiPane;
		private RecordBrowseView _recordBrowseView;
		private RecordClerk _recordClerk;

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
			MultiPaneFactory.RemoveFromParentAndDispose(majorFlexComponentParameters.MainCollapsingSplitContainer, ref _multiPane);
			_recordBrowseView = null;
		}

		/// <summary>
		/// Activate the component.
		/// </summary>
		/// <remarks>
		/// This is called on the component that is becoming active.
		/// </remarks>
		public void Activate(MajorFlexComponentParameters majorFlexComponentParameters)
		{
			if (majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Count == 0)
			{
				// Pathological...this helps the memory-only backend mainly, but makes others self-repairing.
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(majorFlexComponentParameters.LcmCache.ActionHandlerAccessor, () =>
				{
					majorFlexComponentParameters.LcmCache.LanguageProject.PhonologicalDataOA.PhonemeSetsOS.Add(majorFlexComponentParameters.LcmCache.ServiceLocator.GetInstance<IPhPhonemeSetFactory>().Create());
				});
			}
			if (_recordClerk == null)
			{
				_recordClerk = majorFlexComponentParameters.RecordClerkRepositoryForTools.GetRecordClerk(GrammarArea.Phonemes, majorFlexComponentParameters.Statusbar, GrammarArea.PhonemesFactoryMethod);
			}

			var root = XDocument.Parse(GrammarResources.PhonemeEditToolParameters).Root;
			_recordBrowseView = new RecordBrowseView(root.Element("browseview").Element("parameters"), majorFlexComponentParameters.LcmCache, _recordClerk);
#if RANDYTODO
			// TODO: Set up 'dataTreeMenuHandler' to handle menu events.
			// TODO: Install menus and connect them to event handlers. (See "CreateContextMenuStrip" method for where the menus are.)
#endif
			var recordEditView = new RecordEditView(root.Element("recordview").Element("parameters"), XDocument.Parse(AreaResources.VisibilityFilter_All), majorFlexComponentParameters.LcmCache, _recordClerk);
			var mainMultiPaneParameters = new MultiPaneParameters
			{
				Orientation = Orientation.Vertical,
				AreaMachineName = AreaMachineName,
				Id = "PhonemeItemsAndDetailMultiPane",
				ToolMachineName = MachineName
			};

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
				_recordBrowseView, "Browse", new PaneBar(),
				recordEditView, "Details", recordEditViewPaneBar);

			panelButton.DatTree = recordEditView.DatTree;
			// Too early before now.
			recordEditView.FinishInitialization();
			RecordClerkServices.SetClerk(majorFlexComponentParameters, _recordClerk);
		}

		/// <summary>
		/// Do whatever might be needed to get ready for a refresh.
		/// </summary>
		public void PrepareToRefresh()
		{
			_recordBrowseView.BrowseViewer.BrowseView.PrepareToRefresh();
		}

		/// <summary>
		/// Finish the refresh.
		/// </summary>
		public void FinishRefresh()
		{
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
		public string MachineName => "phonemeEdit";

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		public string UiName => "Phonemes";

		#endregion

		#region Implementation of ITool

		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		public string AreaMachineName => "grammar";

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		public Image Icon => Images.SideBySideView.SetBackgroundColor(Color.Magenta);

		#endregion
	}
}