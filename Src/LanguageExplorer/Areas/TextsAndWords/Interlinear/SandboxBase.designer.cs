// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	partial class SandboxBase
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ******");

			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				PropertyTable.RemoveProperty("FirstControlToHandleMessages", SettingsGroup.LocalSettings);
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
				EditMonitor?.Dispose();
				m_vc?.Dispose();
				Caches?.Dispose();
				DisposeComboHandler();
				if (m_caHandler != null)
				{
					m_caHandler.AnalysisChosen -= new EventHandler(Handle_AnalysisChosen);
					m_caHandler.Dispose();
				}
			}
			m_stylesheet = null;
			Caches = null;
			// StringCaseStatus m_case; // Enum, which is a value type, and value types can't be set to null.
			m_ComboHandler = null; // handles most kinds of combo box.
			m_caHandler = null; // handles the one on the base line.
			EditMonitor = null;
			m_vc = null;
			m_rawWordform = null;
			m_tssWordform = null;
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			components = new System.ComponentModel.Container();
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		}

		#endregion
	}
}