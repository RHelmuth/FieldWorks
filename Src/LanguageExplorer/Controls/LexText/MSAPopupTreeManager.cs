// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// Handles a TreeCombo control (Widgets assembly) for use with MorphoSyntaxAnalysis objects.
	/// </summary>
	public class MSAPopupTreeManager : PopupTreeManager
	{
		private const int kEmpty = 0;
		private const int kLine = -1;
		private const int kMore = -2;
		private const int kCreate = -3;
		private const int kModify = -4;

		#region Data members

		private IPersistenceProvider m_persistProvider;
		private ILexSense m_sense;
		private string m_fieldName = "";
		// The following strings are loaded from the string table if possible.
		private string m_sUnknown = null;
		private string m_sSpecifyGramFunc = null;
		private string m_sModifyGramFunc = null;
		private string m_sSpecifyDifferent = null;
		private string m_sCreateGramFunc = null;
		private string m_sEditGramFunc = null;
		#endregion Data members

		#region Events

		#endregion Events

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(TreeCombo treeCombo, LcmCache cache, ICmPossibilityList list,
			int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(treeCombo, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
			LoadStrings();
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		public MSAPopupTreeManager(PopupTree popupTree, LcmCache cache, ICmPossibilityList list,
			int ws, bool useAbbr, IPropertyTable propertyTable, IPublisher publisher, Form parent)
			: base(popupTree, cache, propertyTable, publisher, list, ws, useAbbr, parent)
		{
			LoadStrings();
		}

		public ILexSense Sense
		{
			get
			{
				CheckDisposed();
				return m_sense;
			}
			set
			{
				CheckDisposed();
				m_sense = value;
			}
		}

		public string FieldName
		{
			get
			{
				CheckDisposed();
				return m_fieldName;
			}
			set
			{
				CheckDisposed();
				m_fieldName = value;
			}
		}

		public IPersistenceProvider PersistenceProvider
		{
			get
			{
				CheckDisposed();
				return m_persistProvider;
			}
			set
			{
				CheckDisposed();
				m_persistProvider = value;
			}
		}

		private void LoadStrings()
		{
			// Load the special strings from the string table.
			m_sUnknown = StringTable.Table.GetString("NullItemLabel",
					"DetailControls/MSAReferenceComboBox");
			m_sSpecifyGramFunc = StringTable.Table.GetString("AddNewGramFunc",
					"DetailControls/MSAReferenceComboBox");
			m_sModifyGramFunc = StringTable.Table.GetString("ModifyGramFunc",
					"DetailControls/MSAReferenceComboBox");
			m_sSpecifyDifferent = StringTable.Table.GetString("SpecifyDifferentGramFunc",
					"DetailControls/MSAReferenceComboBox");
			m_sCreateGramFunc = StringTable.Table.GetString("CreateGramFunc",
					"DetailControls/MSAReferenceComboBox");
			m_sEditGramFunc = StringTable.Table.GetString("EditGramFunc",
					"DetailControls/MSAReferenceComboBox");
			if (string.IsNullOrEmpty(m_sUnknown) ||
				m_sUnknown == "*NullItemLabel*")
			{
				m_sUnknown = LexTextControls.ks_NotSure_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyGramFunc) ||
				m_sSpecifyGramFunc == "*AddNewGramFunc*")
			{
				m_sSpecifyGramFunc = LexTextControls.ksSpecifyGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sModifyGramFunc) ||
				m_sModifyGramFunc == "*ModifyGramFunc*")
			{
				m_sModifyGramFunc = LexTextControls.ksModifyThisGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sSpecifyDifferent) ||
				m_sSpecifyDifferent == "*SpecifyDifferentGramFunc*")
			{
				m_sSpecifyDifferent = LexTextControls.ksSpecifyDifferentGrammaticalInfo_;
			}
			if (string.IsNullOrEmpty(m_sCreateGramFunc) ||
				m_sCreateGramFunc == "*CreateGramFuncGramFunc*")
			{
				m_sCreateGramFunc = LexTextControls.ksCreateNewGrammaticalInfo;
			}
			if (string.IsNullOrEmpty(m_sEditGramFunc) ||
				m_sEditGramFunc == "*EditGramFuncGramFunc*")
			{
				m_sEditGramFunc = LexTextControls.ksEditGrammaticalInfo;
			}
		}

		/// <summary>
		/// Populate the tree with just ONE menu item, the one that we want to select.
		/// </summary>
		public TreeNode MakeTargetMenuItem()
		{
			CheckDisposed();

			PopupTree popupTree = GetPopupTree();
			popupTree.Nodes.Clear();
			var msa = m_sense.MorphoSyntaxAnalysisRA;
			TreeNode match = null;
			if (msa == null)
				match = AddNotSureItem(popupTree);
			else
				match = AddTreeNodeForMsa(popupTree, msa);
			return match;
		}

		/// <summary>
		/// NOTE that this implementation IGNORES hvoTarget and selects the MSA indicated by the sense.
		/// </summary>
		/// <param name="popupTree"></param>
		/// <param name="hvoTarget"></param>
		/// <returns></returns>
		protected override TreeNode MakeMenuItems(PopupTree popupTree, int hvoTarget)
		{
			Debug.Assert(m_sense != null);
			hvoTarget = m_sense.MorphoSyntaxAnalysisRA == null ? 0 : m_sense.MorphoSyntaxAnalysisRA.Hvo;
			TreeNode match = null;
			bool fStem = m_sense.GetDesiredMsaType() == MsaType.kStem;
			if (fStem /*m_sense.Entry.MorphoSyntaxAnalysesOC.Count != 0*/)
			{
				// We want the order to be:
				// 1. current msa items
				// 2. possible Parts of Speech
				// 3. "not sure" items
				// We also want the Parts of Speech to be sorted, but not the whole tree.
				// First add the part of speech items (which may be a tree...).
				int tagName = CmPossibilityTags.kflidName;
				// make sure they are sorted
				popupTree.Sorted = true;
				AddNodes(popupTree.Nodes, List.Hvo,
					CmPossibilityListTags.kflidPossibilities, 0, tagName);
				// reset the sorted flag - we only want the parts of speech to be sorted.
				popupTree.Sorted = false;
				// Remember the (sorted) nodes in an array (so we can use the AddRange() method).
				TreeNode[] posArray = new TreeNode[popupTree.Nodes.Count];
				popupTree.Nodes.CopyTo(posArray, 0);
				// now clear out the nodes so we can get the order we want
				popupTree.Nodes.Clear();

				// Add the existing MSA items for the sense's owning entry.
				foreach (var msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
				{
					HvoTreeNode node = AddTreeNodeForMsa(popupTree, msa);
					if (msa.Hvo == hvoTarget)
						match = node;
				}
				AddTimberLine(popupTree);

				// now add the sorted parts of speech
				popupTree.Nodes.AddRange(posArray);

				AddTimberLine(popupTree);

				//	1. "<Not Sure>" to produce a negligible Msa reference.
				//	2. "More..." command to launch category chooser dialog.
				TreeNode empty = AddNotSureItem(popupTree);
				if (match == null)
					match = empty;
				AddMoreItem(popupTree);
			}
			else
			{
				int cMsa = m_sense.Entry.MorphoSyntaxAnalysesOC.Count;
				if (cMsa == 0)
				{
					//	1. "<Not Sure>" to produce a negligible Msa reference.
					//	2. "Specify..." command.
					//Debug.Assert(hvoTarget == 0);
					match = AddNotSureItem(popupTree);
					popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sSpecifyGramFunc, Cache.WritingSystemFactory.UserWs), kCreate));
				}
				else
				{
					// 1. Show the current Msa at the top.
					// 2. "Modify ..." command.
					// 3. Show other existing Msas next (if any).
					// 4. <Not Sure> to produce a negligible Msa reference.
					// 5. "Specify different..." command.
					hvoTarget = 0;
					// We should always have an MSA assigned to every sense, but sometimes this
					// hasn't happened.  Don't crash if the data isn't quite correct.  See FWR-3090.
					if (m_sense.MorphoSyntaxAnalysisRA != null)
						hvoTarget = m_sense.MorphoSyntaxAnalysisRA.Hvo;
					if (hvoTarget != 0)
					{
						ITsString tssLabel = m_sense.MorphoSyntaxAnalysisRA.InterlinearNameTSS;
						HvoTreeNode node = new HvoTreeNode(tssLabel, hvoTarget);
						popupTree.Nodes.Add(node);
						match = node;
						popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sModifyGramFunc, Cache.WritingSystemFactory.UserWs), kModify));
						AddTimberLine(popupTree);
					}
					int cMsaExtra = 0;
					foreach (var msa in m_sense.Entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.Hvo == hvoTarget)
							continue;
						ITsString tssLabel = msa.InterlinearNameTSS;
						HvoTreeNode node = new HvoTreeNode(tssLabel, msa.Hvo);
						popupTree.Nodes.Add(node);
						++cMsaExtra;
					}
					if (cMsaExtra > 0)
						AddTimberLine(popupTree);
					// Per final decision on LT-5084, don't want <not sure> for affixes.
					//TreeNode empty = AddNotSureItem(popupTree, hvoTarget);
					//if (match == null)
					//    match = empty;
					popupTree.Nodes.Add(new HvoTreeNode(TsStringUtils.MakeString(m_sSpecifyDifferent, Cache.WritingSystemFactory.UserWs), kCreate));
				}
			}
			return match;
		}

		private HvoTreeNode AddTreeNodeForMsa(PopupTree popupTree, IMoMorphSynAnalysis msa)
		{
			// JohnT: as described in LT-4633, a stem can be given an allomorph that
			// is an affix. So we need some sort of way to handle this.
			//Debug.Assert(msa is MoStemMsa);
			ITsString tssLabel = msa.InterlinearNameTSS;
			var stemMsa = msa as IMoStemMsa;
			if (stemMsa != null && stemMsa.PartOfSpeechRA == null)
				tssLabel = TsStringUtils.MakeString(
					m_sUnknown,
					Cache.ServiceLocator.WritingSystemManager.UserWs);
			var node = new HvoTreeNode(tssLabel, msa.Hvo);
			popupTree.Nodes.Add(node);
			return node;
		}

		protected override void m_treeCombo_AfterSelect(object sender, TreeViewEventArgs e)
		{
			HvoTreeNode selectedNode = e.Node as HvoTreeNode;

			// Launch dialog only by a mouse click (or simulated mouse click).
			if (selectedNode != null && selectedNode.Hvo == kMore && e.Action == TreeViewAction.ByMouse)
			{
				ChooseFromMasterCategoryList();
			}
			else if (selectedNode != null && selectedNode.Hvo == kCreate && e.Action == TreeViewAction.ByMouse)
			{
				if (AddNewMsa())
					return;
			}
			else if (selectedNode != null && selectedNode.Hvo == kModify && e.Action == TreeViewAction.ByMouse)
			{
				if (EditExistingMsa())
					return;
			}
			else if (selectedNode != null && selectedNode.Hvo == kEmpty && e.Action == TreeViewAction.ByMouse)
			{
				SwitchToEmptyMsa();
				return;
			}
			base.m_treeCombo_AfterSelect(sender, e);
		}

		private void SwitchToEmptyMsa()
		{
			CreateEmptyMsa();
			LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
		}

		private void CreateEmptyMsa()
		{
			SandboxGenericMSA dummyMsa = new SandboxGenericMSA();
			dummyMsa.MsaType = m_sense.GetDesiredMsaType();
			// To make it fully 'not sure' we must discard knowledge of affix type.
			if (dummyMsa.MsaType == MsaType.kInfl || dummyMsa.MsaType == MsaType.kDeriv)
				dummyMsa.MsaType = MsaType.kUnclassified;
			UndoableUnitOfWorkHelper.Do(string.Format(LexTextControls.ksUndoSetX, FieldName),
				string.Format(LexTextControls.ksRedoSetX, FieldName), m_sense, () =>
			{
				m_sense.SandboxMSA = dummyMsa;
			});
		}

		private void ChooseFromMasterCategoryList()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "More..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObjWithoutTriggeringBeforeAfterSelects(m_sense.MorphoSyntaxAnalysisRA.Hvo);
			// FWR-3542 -- Need this in .Net too, or it eats the first mouse click intended
			// for the dialog we're about to show below.
			pt.HideForm();

			new MasterCategoryListChooserLauncher(ParentForm, m_propertyTable, m_publisher, List, FieldName, m_sense);
		}

		private bool AddNewMsa()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Specify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObj(m_sense.MorphoSyntaxAnalysisRA.Hvo);
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				SandboxGenericMSA dummyMsa = new SandboxGenericMSA();
				dummyMsa.MsaType = m_sense.GetDesiredMsaType();
				dlg.SetDlgInfo(Cache, m_persistProvider, m_propertyTable, m_publisher, m_sense.Entry, dummyMsa, 0, false, null);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					m_sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
					return true;
				}
			}
			return false;
		}

		private bool EditExistingMsa()
		{
			PopupTree pt = GetPopupTree();
			// Force the PopupTree to Hide() to trigger popupTree_PopupTreeClosed().
			// This will effectively revert the list selection to a previous confirmed state.
			// Whatever happens below, we don't want to actually leave the "Modify ..." node selected!
			// This is at least required if the user selects "Cancel" from the dialog below.
			if (m_sense.MorphoSyntaxAnalysisRA != null)
				pt.SelectObj(m_sense.MorphoSyntaxAnalysisRA.Hvo);
#if __MonoCS__
			// If Popup tree is shown whilest the dialog is shown, the first click on the dialog is consumed by the
			// Popup tree, (and closes it down). On .NET the PopupTree appears to be automatically closed.
			pt.HideForm();
#endif
			SandboxGenericMSA dummyMsa = SandboxGenericMSA.Create(m_sense.MorphoSyntaxAnalysisRA);
			using (MsaCreatorDlg dlg = new MsaCreatorDlg())
			{
				dlg.SetDlgInfo(Cache, m_persistProvider, m_propertyTable, m_publisher, m_sense.Entry, dummyMsa,
					m_sense.MorphoSyntaxAnalysisRA.Hvo, true, m_sEditGramFunc);
				if (dlg.ShowDialog(ParentForm) == DialogResult.OK)
				{
					Cache.DomainDataByFlid.BeginUndoTask(String.Format(LexTextControls.ksUndoSetX, FieldName),
						String.Format(LexTextControls.ksRedoSetX, FieldName));
					m_sense.SandboxMSA = dlg.SandboxMSA;
					Cache.DomainDataByFlid.EndUndoTask();
					LoadPopupTree(m_sense.MorphoSyntaxAnalysisRA.Hvo);
					return true;
				}
			}
			return false;
		}
	}
}
