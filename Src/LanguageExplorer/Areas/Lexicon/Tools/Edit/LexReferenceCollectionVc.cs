// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class LexReferenceCollectionVc : VectorReferenceVc
	{
		/// <summary />
		protected ICmObject m_displayParent;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceCollectionVc(LcmCache cache, int flid, string displayNameProperty, string displayWs)
			: base (cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			var da = vwenv.DataAccess;
			var count = da.get_VecSize(hvo, tag);
			// Show everything in the collection except the current element from the main display.
			for (var i = 0; i < count; ++i)
			{
				var hvoItem = da.get_VecItem(hvo, tag, i);
				if (m_displayParent != null && hvoItem == m_displayParent.Hvo)
				{
					continue;
				}
				vwenv.AddObj(hvoItem, this,	VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}