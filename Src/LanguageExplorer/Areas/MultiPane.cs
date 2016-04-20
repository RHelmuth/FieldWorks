// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas
{
#if RANDYTODO
	// TODO: Why do we need "CollapsingSplitContainer" & "MultiPane"? Can they be collapsed into one?
	// TODO: "CollapsingSplitContainer" is used by the main window as its top level splitter,
	// TODO: and it is the right pane of main CollapsingSplitContainer instance for tools with a RecordBar.
	// TODO: "MultiPane" is then used by numerous tools in the right half of that main or second "CollapsingSplitContainer".
#endif
	/// <summary>
	/// A MultiPane (actually currently more a DualPane) displays two child controls,
	/// either side by side or one above the other, with a splitter between them.
	///
	/// The vertical parameter causes the two controls to be one above the other, if true,
	/// or side by side, if false. (Default, if omitted, is true.)
	/// The id parameter gives the MultiPane a name (which should be unique across the whole
	/// containing application) to use in storing state, such as the position of the splitter,
	/// persistently.
	/// It is mandatory to specify the area that the control is part of, but I (JT) don't know why.
	///
	/// If the mediator has a property called id_ShowFirstPane (e.g., LexEntryAndEditor_ShowFirstPane),
	/// it will control the visibility of the first pane (visible if the property is true).
	/// </summary>
	internal class MultiPane : CollapsingSplitContainer, IMainContentControl
	{
		/// <summary />
		internal event EventHandler ShowFirstPaneChanged;

		private readonly string m_areaMachineName;
		private readonly string m_id;
		// When its superclass gets switched to the new SplitContainer class. it has to implement IMainUserControl itself.
		private IContainer components;
		private Size m_parentSizeHint;
		private bool m_showingFirstPane;
		private string m_propertyControllingVisibilityOfFirstPane;
		private string m_defaultPrintPaneId;
		private string m_defaultFocusControl;
		//the name of the tool which this MultiPane is a part of.
		private string m_toolName;
		private string m_defaultFixedPaneSizePoints;
		private string m_persistContext;
		private string m_label;

		/// <summary>
		/// Constructor
		/// </summary>
		internal MultiPane()
		{
			ResetSplitterEventHandler(false); // Get rid of the handler until we have a parent.

			m_parentSizeHint.Width = m_parentSizeHint.Height = 0;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		internal MultiPane(MultiPaneParameters parameters)
			: this()
		{
			m_toolName = parameters.ToolMachineName ?? string.Empty;
			m_areaMachineName = parameters.AreaMachineName;
			m_id = parameters.Id ?? "NOID";
			m_defaultFixedPaneSizePoints = parameters.DefaultFixedPaneSizePoints ?? "50%";
			m_defaultPrintPaneId = parameters.DefaultPrintPane ?? string.Empty;
			m_defaultFocusControl = parameters.DefaultFocusControl ?? string.Empty;
			m_persistContext = parameters.PersistContext;
			m_label = parameters.Label;
			m_propertyControllingVisibilityOfFirstPane = string.Format("Show_{0}", m_id);

			SecondCollapseZone = parameters.SecondCollapseZone;
			Orientation = parameters.Orientation;
			Dock = DockStyle.Fill;
			SplitterWidth = 5;

			FirstControl = parameters.FirstControlParameters.Control;
			FirstLabel = parameters.FirstControlParameters.Label;
			FirstControl.Dock = DockStyle.Fill;

			SecondControl = parameters.SecondControlParameters.Control;
			SecondLabel = parameters.SecondControlParameters.Label;
			SecondControl.Dock = DockStyle.Fill;
		}


		/// <summary />
		internal string PropertyControllingVisibilityOfFirstPane
		{
			get { return m_propertyControllingVisibilityOfFirstPane; }
			set
			{
				if (!string.IsNullOrEmpty(m_propertyControllingVisibilityOfFirstPane))
				{
					Subscriber.Unsubscribe(m_propertyControllingVisibilityOfFirstPane, PropertyControllingVisibilityOfFirstPane_Changed);
				}
				m_propertyControllingVisibilityOfFirstPane = value;
				m_showingFirstPane = string.IsNullOrEmpty(value);
				Subscriber.Subscribe(m_propertyControllingVisibilityOfFirstPane, PropertyControllingVisibilityOfFirstPane_Changed);
			}
		}

		private void PropertyControllingVisibilityOfFirstPane_Changed(object newValue)
		{
			var showIt = (bool) newValue;
			if (m_showingFirstPane == showIt)
			{
				return; // No change.
			}
			m_showingFirstPane = string.IsNullOrEmpty(m_propertyControllingVisibilityOfFirstPane) && showIt;
			Panel1Collapsed = !m_showingFirstPane;
			if (ShowFirstPaneChanged != null)
				ShowFirstPaneChanged(this, new EventArgs());
		}

		/// <summary />
		internal string PrintPane
		{
			get
			{
				CheckDisposed();
				return m_defaultPrintPaneId;
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;
			if (disposing)
			{
				if(components != null)
					components.Dispose();
				if (!string.IsNullOrWhiteSpace(m_propertyControllingVisibilityOfFirstPane))
				{
					Subscriber.Unsubscribe(m_propertyControllingVisibilityOfFirstPane, PropertyControllingVisibilityOfFirstPane_Changed);
				}
			}

			base.Dispose(disposing);
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
//			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(MultiPane));
			//
			// MultiPane
			//
			this.Name = "MultiPane";

		}
		#endregion

		#region IMainUserControl implementation

		/// <summary>
		/// This is the property that return the name to be used by the accessibility object.
		/// </summary>
		public string AccName
		{
			get
			{
				CheckDisposed();

				var name = m_persistContext;
				if (string.IsNullOrEmpty(name))
				{
					name = m_id;
				}
				if (string.IsNullOrEmpty(name))
				{
					name = m_label;
				}
				if (string.IsNullOrEmpty(name))
				{
					name = "MultiPane";
				}

				return name;
			}
		}

		/// <summary>
		/// Get/set string that will trigger a message box to show.
		/// </summary>
		/// <remarks>Set to null or string.Empty to not show the message box.</remarks>
		public string MessageBoxTrigger { get; set; }

		#endregion IMainUserControl implementation

		#region IMainContentControl implementation

		/// <summary />
		public bool PrepareToGoAway()
		{
			CheckDisposed();

			//we are ready to go away if our two controls are ready to go away
			bool firstControlReady = true;
			if (FirstControl != null)
				firstControlReady = ((IMainContentControl)FirstControl).PrepareToGoAway();
			bool secondControlReady = true;
			if (SecondControl != null)
				secondControlReady = ((IMainContentControl)SecondControl).PrepareToGoAway();

			return firstControlReady && secondControlReady;
		}

		/// <summary />
		public string AreaName
		{
			get
			{
				CheckDisposed();

				return m_areaMachineName;
			}
		}

		#endregion // IMainContentControl implementation

		#region ICtrlTabProvider implementation

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "result is a reference")]
		public Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("'targetCandidates' is null.");

			int sizeOfSharedDimensionPanel1 = Orientation == Orientation.Vertical ? Panel1.Width : Panel1.Height;
			Control result = null;
			if (sizeOfSharedDimensionPanel1 != CollapsingSplitContainer.kCollapsedSize && !Panel1Collapsed)
			{
				// Panel1 is visible and wide.
				result = (FirstControl as ICtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
				if (!FirstControl.ContainsFocus)
					result = null;
			}
			int sizeOfSharedDimensionPanel2 = Orientation == Orientation.Vertical ? Panel2.Width : Panel2.Height;
			if (sizeOfSharedDimensionPanel2 != CollapsingSplitContainer.kCollapsedSize && !Panel2Collapsed)
			{
				// Panel2 is visible and wide.
				Control otherResult = (SecondControl as ICtrlTabProvider).PopulateCtrlTabTargetCandidateList(targetCandidates);
				if (SecondControl.ContainsFocus)
				{
					Debug.Assert(result == null, "result is unexpectedly not null.");
					Debug.Assert(otherResult != null, "otherResult is unexpectedly null.");
					result = otherResult;
				}
			}

			return result;
		}

		#endregion  ICtrlTabProvider implementation

		/// <summary>
		/// Used to give us an idea of what our boundaries will be before we are initialized
		/// enough to determine them ourselves.
		/// </summary>
		/// <remarks> at the moment, the top-level multipane is able to figure out its eventual
		/// size without help from this. However, multipanes inside of other ones rely on this.
		/// </remarks>
		internal Size ParentSizeHint
		{
			get { return m_parentSizeHint; }
			set { m_parentSizeHint = value; }
		}

		/// <summary>
		/// </summary>
		internal String DefaultPrintPaneId
		{
			get { return m_defaultPrintPaneId; }
			set { m_defaultPrintPaneId = value; }
		}

		/// <summary>
		/// Size is overridden so that until the pane is sized properly,
		/// typically docked in some parent, it will be big enough not to interfere with
		/// the splitter position set in its Init method.
		/// </summary>
		protected override Size DefaultSize
		{
			get
			{
				return new Size(2000,2000);
			}
		}

		/// <summary />
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);

			Control parent = Parent;
			if (parent != null && PropertyTable != null)
			{
				ResetSplitterEventHandler(true);
				SetSplitterDistance();
			}
		}

		private string SplitterDistancePropertyName
		{
			get
			{
				return string.Format("MultiPaneSplitterDistance_{0}_{1}_{2}",
					m_areaMachineName,
					PropertyTable.GetValue("currentContentControl", string.Empty),
					m_id);
			}
		}

		// Set to true when sufficiently initialized that it makes sense to persist changes to split position.
		private bool m_fOkToPersistSplit = false;

		/// <summary />
		protected override void OnSplitterMoved(object sender, SplitterEventArgs e)
		{
			if (InSplitterMovedMethod)
				return;

			base.OnSplitterMoved(sender, e);

			// Persist new position.
			if (PropertyTable != null && m_fOkToPersistSplit)
			{
				PropertyTable.SetProperty(SplitterDistancePropertyName, SplitterDistance, true, false);
			}
		}

		private void SetSplitterDistance()
		{
			int sizeOfSharedDimension = Orientation == Orientation.Vertical ? Width : Height;
			int defaultLocation;

			// Find 'total', which will be the height or width,
			// depending on the orientation of the multi pane.
			bool proportional = m_defaultFixedPaneSizePoints.EndsWith("%");
			int total;
			Size size = Size;
			if (m_parentSizeHint.Width != 0 && !proportional)
				size = m_parentSizeHint;
			if (Orientation == Orientation.Vertical)
				total = size.Width;
			else
				total = size.Height;

			if (proportional)
			{
				string percentStr = m_defaultFixedPaneSizePoints.Substring(0, m_defaultFixedPaneSizePoints.Length - 1);
				int percent = Int32.Parse(percentStr);
				float loc = (total * (((float)percent) / 100));
				double locD = Math.Round(loc);
				defaultLocation = (int)locD;
			}
			else
			{
				defaultLocation = Int32.Parse(m_defaultFixedPaneSizePoints);
			}

			if (PropertyTable != null)
			{
				// NB GetIntProperty RECORDS the default as if it had really been set by the user.
				// This behavior is disastrous here, where if we haven't truly persisted something,
				// we want to stick to computing the percent whenever the parent resizes.
				// So, first see whether there is a value in the property table at all.
				defaultLocation = PropertyTable.GetValue(SplitterDistancePropertyName, defaultLocation);
			}
			if (defaultLocation < kCollapsedSize)
				defaultLocation = kCollapsedSize;

			if (SplitterDistance != defaultLocation)
			{
				int originalSD = SplitterDistance;
				try
				{
					// Msg: SplitterDistance (aka: defaultLocation) must be between Panel1MinSize and Width - Panel2MinSize.
					if (defaultLocation >= Panel1MinSize && defaultLocation <= (sizeOfSharedDimension - Panel2MinSize))
					{
						// We do NOT want to persist this computed position!
						bool old = m_fOkToPersistSplit;
						m_fOkToPersistSplit = false;
						SplitterDistance = defaultLocation;
						m_fOkToPersistSplit = old;
					}
				}
				catch (Exception err)
				{
					Debug.WriteLine(err.Message);
					string msg = string.Format("Orientation: {0} Width: {1} Height: {2} Original SD: {3} New SD: {4} Panel1MinSize: {5} Panel2MinSize: {6} ID: {7} Panel1Collapsed: {8} Panel2Collapsed: {9}",
						Orientation, Width, Height, originalSD, defaultLocation,
						Panel1MinSize, Panel2MinSize,
						m_id,
						Panel1Collapsed, Panel2Collapsed);
					throw new ArgumentOutOfRangeException(msg, err);
				}
			}
		}


#if RANDYTO
		/// summary>
		/// Receives the broadcast message "PropertyChanged." If it is the ShowFirstPane
		/// property, adjust.
		/// /summary>
		public void OnPropertyChanged(string name)
		{
			CheckDisposed();
			if (PropertyTable.GetValue("ToolForAreaNamed_lexicon", "") != m_toolName)
			{
				return;
			}
			if (name == "ActiveClerkSelectedObject" || name == "ToolForAreaNamed_lexicon")
			{
				SetFocusInDefaultControl();
			}
			if (name == m_propertyControllingVisibilityOfFirstPane)
			{
				bool fShowFirstPane = PropertyTable.GetValue(m_propertyControllingVisibilityOfFirstPane, true);
				if (fShowFirstPane == m_showingFirstPane)
					return; // just in case it didn't really change

				m_showingFirstPane = fShowFirstPane;

				Panel1Collapsed = !fShowFirstPane;

				if (ShowFirstPaneChanged != null)
					ShowFirstPaneChanged(this, new EventArgs());
			}

		}
#endif

		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			SetFocusInDefaultControl();
		}

		/// <summary>
		/// The focus will only be set in the default control if it implements IFocusablePanePortion.
		/// Note that it may BE our First or SecondPane, or it may be a child of one of those.
		/// </summary>
		private void SetFocusInDefaultControl()
		{
			if (String.IsNullOrEmpty(m_defaultFocusControl))
				return;
			var defaultFocusControl = (FwUtils.FindControl(FirstControl, m_defaultFocusControl) ??
				FwUtils.FindControl(SecondControl, m_defaultFocusControl)) as IFocusablePanePortion;
			Debug.Assert(defaultFocusControl != null,
				"Failed to find focusable subcontrol.",
				"This MultiPane was configured to focus {0} as a default control. But it either was not found or was not an IFocuablePanePortion",
				m_defaultFocusControl);
			// LT-14222...can't do BeginInvoke until our handle is created...we attempt this multiple times since it is hard
			// to find the right time to do it. If we can't do it yet hope we can do it later.
			if (defaultFocusControl != null && IsHandleCreated)
			{
				defaultFocusControl.IsFocusedPane = true; // Lets it know it can do any special behavior (e.g., DataPane) when it is the focused child.
				BeginInvoke((MethodInvoker) (() => defaultFocusControl.Focus()));
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

			IsInitializing = true;
			if (m_propertyControllingVisibilityOfFirstPane == null)
			{
				m_showingFirstPane = false;
			}
			else
			{
				m_showingFirstPane = true; // default
				// NOTE: we don't actually want to create and persist this property if it's not already loaded.
				bool showingFirstPane;
				if (PropertyTable.TryGetValue(m_propertyControllingVisibilityOfFirstPane, SettingsGroup.LocalSettings, out showingFirstPane))
				{
					m_showingFirstPane = showingFirstPane;
				}
			}
			Panel1Collapsed = !m_showingFirstPane;
			IsInitializing = false;
			m_fOkToPersistSplit = true;
		}

		#endregion

		#region Implementation of IMainUserControl

		/// <summary>
		/// Get or set the name to be used by the accessibility object.
		/// </summary>
		string IMainUserControl.AccName { get; set; }

		#endregion
	}
}