// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	/// <summary />
	public partial class StringFieldOptions : UserControl
	{
		LcmCache m_cache;

		/// <summary>
		/// Initializes a new instance of the <see cref="StringFieldOptions"/> class.
		/// </summary>
		public StringFieldOptions()
		{
			InitializeComponent();
		}

		internal void Initialize(LcmCache cache, IHelpTopicProvider helpTopicProvider, IApp app,
			RnSfMarker rsfm)
		{
			m_cache = cache;
			m_btnAddWritingSystem.Initialize(cache, helpTopicProvider, app);
			NotebookImportWiz.InitializeWritingSystemCombo(rsfm.m_sto.m_wsId, cache,
				m_cbWritingSystem);
		}

		public string WritingSystem
		{
			get
			{
				var ws = m_cbWritingSystem.SelectedItem as CoreWritingSystemDefinition;
				if (ws == null)
					return null;
				else
					return ws.Id;
			}
		}

		private void m_btnAddWritingSystem_WritingSystemAdded(object sender, EventArgs e)
		{
			CoreWritingSystemDefinition ws = m_btnAddWritingSystem.NewWritingSystem;
			if (ws != null)
				NotebookImportWiz.InitializeWritingSystemCombo(ws.Id, m_cache,
					m_cbWritingSystem);
		}
	}
}