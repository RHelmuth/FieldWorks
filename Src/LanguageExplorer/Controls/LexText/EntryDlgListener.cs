// Copyright (c) 2004-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: EntryDlgListener.cs
// Responsibility: Randy Regnier
using System;
using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.LexText
{
#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: MergeEntryDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Lexicon\Tools\Edit\
	// TODO: when the time comes to re-enable the Lexicon area "lexiconEdit" tool (Only one that uses it.).
	//
	// TODO: Likely disposition: Dump MergeEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the merge.
	//
	public class MergeEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "MergeEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public MergeEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Determines in which menus the Merge Entries command item can show up in.
		/// Should only be in the Lexicon area.
		/// </summary>
		/// <remarks>Obviously copied from another area that had more complex criteria for displaying its menu items.</remarks>
		/// <returns>true if Merge Entry ought to be displayed, false otherwise.</returns>
		protected bool InFriendlyArea
		{
			get
			{
				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				if (areaChoice == null) return false; // happens at start up
				if ("lexicon" == areaChoice)
				{
					return PropertyTable.GetValue<string>("toolChoice") == "lexiconEdit";
				}
				return false; //we are not in an area that wants to see the merge command
			}
		}
	#endregion XCORE Message Handlers
	}
#endif

#if RANDYTODO
	// TODO: Blocked out, while DlgListenerBase base class is moved, so this project takes no new dependency on LanguageExplorer.
	// TODO: GoLinkEntryDlgListener will get moved to fw\Src\LanguageExplorer\Areas\Lexicon\
	// TODO: when the time comes to re-enable the Lexicon area tools that use it.
	// TODO: Check "InFriendlyArea" property, as not all tools use this listener.
	//
	// TODO: Likely disposition: Dump GoLinkEntryDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers do the jump.
	//
	/// <summary>
	/// Listener class for the GoLinkEntryDlgListener class.
	/// </summary>
	public class GoLinkEntryDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		protected string PersistentLabel
		{
			get { return "GoLinkLexEntry"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region Construction and Initialization

		public GoLinkEntryDlgListener()
		{
		}

	#endregion Construction and Initialization

	#region XCORE Message Handlers

		/// <summary>
		/// Handles the xCore message to go to or link to a lexical entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoLexEntry(object argument)
		{
			CheckDisposed();

			using (var dlg = new EntryGoDlg())
			{
				var cache = PropertyTable.GetValue<LcmCache>("cache");
				dlg.SetDlgInfo(cache, null, PropertyTable, Publisher);
				dlg.SetHelpTopic("khtpFindLexicalEntry");
				if (dlg.ShowDialog() == DialogResult.OK)
				{
					Publisher.Publish("JumpToRecord", dlg.SelectedObject.Hvo);
				}
			}
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayGotoLexEntry(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		/// <summary>
		///
		/// </summary>
		/// <remarks> this is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible. It is difficult because some commands, like this one, may be appropriate
		/// from more than 1 area.</remarks>
		/// <returns></returns>
		protected  bool InFriendlyArea
		{
			get
			{
				if (PropertyTable.GetValue<string>("ToolForAreaNamed_lexicon") == "reversalEditComplete")
					return false;

				string areaChoice = PropertyTable.GetValue<string>("areaChoice");
				string[] areas = new string[]{"lexicon"};
				foreach(string area in areas)
				{
					if (area == areaChoice)
					{
						// We want to show goto dialog for dictionary views, but not lists, etc.
						// that may be in the Lexicon area.
						// Note, getting a clerk directly here causes a dependency loop in compilation.
						var obj = PropertyTable.GetValue<ICmObject>("ActiveClerkOwningObject");
						return (obj != null) && (obj.ClassID == LexDbTags.kClassId);
					}
				}
				return false; //we are not in an area that wants to see the parser commands
			}
		}

	#endregion XCORE Message Handlers
	}
#endif
}