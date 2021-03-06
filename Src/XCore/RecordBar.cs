// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace XCore
{
	/// <summary>
	/// Summary description for RecordBar.
	/// </summary>
	public class RecordBar : UserControl
	{
		protected TreeView m_treeView;
		protected ListView m_listView;
		private ColumnHeader m_columnHeader1;
		private Control m_optionalHeaderControl;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public RecordBar()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			TreeView.HideSelection = false;
			m_listView.HideSelection = false;

			TreeView.Dock = DockStyle.Fill;
			m_listView.Dock = DockStyle.Fill;

			IsFlatList = true;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		public TreeView TreeView
		{
			get
			{
				CheckDisposed();

				return m_treeView;
			}
		}

		public ListView ListView
		{
			get
			{
				CheckDisposed();

				return m_listView;
			}
		}

		public bool IsFlatList
		{
			set
			{
				CheckDisposed();

				if (value)
				{
					TreeView.Visible = false;
					m_listView.Visible = true;
				}
				else
				{
					TreeView.Visible = true;
					m_listView.Visible = false;
				}
			}
		}

		public bool HasHeaderControl { get { return m_optionalHeaderControl != null; } }

		public void AddHeaderControl(Control c)
		{
			CheckDisposed();

			if (c == null || HasHeaderControl)
				return;

			m_optionalHeaderControl = c;
			Controls.Add(c);
			c.Dock = DockStyle.Top;
		}

		public object SelectedNode
		{
			set
			{
				CheckDisposed();

				TreeView.SelectedNode = (TreeNode) value;
			}
		}

		public void Clear()
		{
			CheckDisposed();

			TreeView.Nodes.Clear();
			m_listView.Items.Clear();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RecordBar));
			this.m_treeView = new System.Windows.Forms.TreeView();
			this.m_listView = new System.Windows.Forms.ListView();
			this.m_columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			//
			// m_treeView
			//
			resources.ApplyResources(this.m_treeView, "m_treeView");
			this.m_treeView.Name = "m_treeView";
			this.m_treeView.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes"))),
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes1"))),
			((System.Windows.Forms.TreeNode)(resources.GetObject("m_treeView.Nodes2")))});
			//
			// m_listView
			//
			this.m_listView.AutoArrange = false;
			this.m_listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_columnHeader1});
			resources.ApplyResources(this.m_listView, "m_listView");
			this.m_listView.HideSelection = false;
			this.m_listView.MultiSelect = false;
			this.m_listView.Name = "m_listView";
			this.m_listView.UseCompatibleStateImageBehavior = false;
			this.m_listView.View = System.Windows.Forms.View.Details;
			//
			// columnHeader1
			//
			resources.ApplyResources(this.m_columnHeader1, "m_columnHeader1");
			//
			// RecordBar
			//
			this.Controls.Add(this.m_listView);
			this.Controls.Add(this.m_treeView);
			this.Name = "RecordBar";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		public void ShowHeaderControl()
		{
			if (HasHeaderControl)
				m_optionalHeaderControl.Visible = true;
		}

		public void HideHeaderControl()
		{
			if (HasHeaderControl)
				m_optionalHeaderControl.Visible = false;
		}
	}
}
