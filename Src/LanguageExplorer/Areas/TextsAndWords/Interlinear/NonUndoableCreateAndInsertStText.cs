﻿// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class NonUndoableCreateAndInsertStText : CreateAndInsertStText
	{
		internal NonUndoableCreateAndInsertStText(LcmCache cache, InterlinearTextsRecordList list)
			: base(cache, list)
		{
		}

		#region ICreateAndInsert<IStText> Members

		public override IStText Create()
		{
			// Don't inline this, it launches a dialog and should be done BEFORE starting the UOW.
			var wsText = List.GetWsForNewText();

			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ActionHandlerAccessor, () => CreateNewTextWithEmptyParagraph(wsText));
			return NewStText;
		}

		#endregion
	}
}