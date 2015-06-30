﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal partial class ParatextLexiconPluginRegistryHelper
	{
		private const string ProductName = "FieldWorks";

		static ParatextLexiconPluginRegistryHelper()
		{
			RegistryHelper.CompanyName = DirectoryFinder.CompanyName;
			RegistryHelper.ProductName = ProductName;
		}

		public static bool IsFieldWorksInstalled
		{
			get
			{
				using (RegistryKey machineKey = FieldWorksRegistryKeyLocalMachine)
				{
					return machineKey != null;
				}
			}
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		public static RegistryKey FieldWorksRegistryKey
		{
			get { return RegistryHelper.SettingsKey(FWMajorVersion); }
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "Disposed in caller.")]
		public static RegistryKey FieldWorksRegistryKeyLocalMachine
		{
			get { return RegistryHelper.SettingsKeyLocalMachine(FWMajorVersion); }
		}
	}
}